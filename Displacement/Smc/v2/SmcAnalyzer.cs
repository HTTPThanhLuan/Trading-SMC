using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Trading.Smc.v2;

// ================================================================
// SMC ANALYZER
// ================================================================

public sealed class SmcAnalyzer
{
    public SmcAnalysisResult Analyze(
        List<Candle> candles,
        int range = 100,
        int swingLength = 5,
        string sourceTimeFrame = "")
    {
        var ordered = candles
            .Where(IsValidCandle)
            .OrderBy(x => x.Time)
            .TakeLast(range)
            .ToList();

        var result = new SmcAnalysisResult();
        if (ordered.Count < Math.Max(20, swingLength * 2 + 5)) return result;

        result.Swings = DetectSwings(ordered, swingLength);
        result.Trend = DetectTrend(result.Swings);
        result.StructureBreaks = DetectBosChoch(ordered, result.Swings, closeBreak: true);
        result.SupportResistanceZones = BuildSupportResistanceZones(ordered, result.Swings, sourceTimeFrame: sourceTimeFrame);
        result.FvgZones = DetectFvg(ordered, joinConsecutive: true, sourceTimeFrame: sourceTimeFrame);
        result.OrderBlocks = DetectOrderBlocks(ordered, result.Swings, closeMitigation: false, sourceTimeFrame: sourceTimeFrame);
        result.SupplyDemandZones = DetectSupplyDemandZones(ordered, sourceTimeFrame: sourceTimeFrame);
        result.LiquidityZones = DetectLiquidity(ordered, result.Swings, rangePercent: 0.01m, sourceTimeFrame: sourceTimeFrame);
        result.LatestDisplacement = DetectBestDisplacement(ordered);

        MarkBrokenZones(ordered, result.AllZones);
        ScoreZones(ordered, result);

        return result;
    }

    private static bool IsValidCandle(Candle c) => c.High >= c.Low && c.High > 0 && c.Low > 0;

    // Similar to the Python swing_highs_lows idea: current high/low must be extreme among candles before/after.
    // Then consecutive highs/lows are cleaned so structure alternates better.
    public static List<SwingPoint> DetectSwings(List<Candle> candles, int swingLength = 5)
    {
        var raw = new List<SwingPoint>();
        int n = candles.Count;
        if (n < swingLength * 2 + 1) return raw;

        for (int i = swingLength; i < n - swingLength; i++)
        {
            //get highest high and lowest low in the range of swingLength before and after the current candle
            decimal maxHigh = candles.Skip(i - swingLength).Take(swingLength * 2 + 1).Max(x => x.High);
            decimal minLow = candles.Skip(i - swingLength).Take(swingLength * 2 + 1).Min(x => x.Low);

            if (candles[i].High == maxHigh)
            {
                raw.Add(new SwingPoint { Index = i, Time = candles[i].Time, Type = SwingType.High, Level = candles[i].High });
            }
            else if (candles[i].Low == minLow)
            {
                raw.Add(new SwingPoint { Index = i, Time = candles[i].Time, Type = SwingType.Low, Level = candles[i].Low });
            }
        }

        bool changed;
        do
        {
            changed = false;
            for (int i = 0; i < raw.Count - 1; i++)
            {
                var current = raw[i];
                var next = raw[i + 1];
                if (current.Type != next.Type) continue;

                if (current.Type == SwingType.High)
                {
                    int removeIndex = current.Level >= next.Level ? i + 1 : i;
                    raw.RemoveAt(removeIndex);
                }
                else
                {
                    int removeIndex = current.Level <= next.Level ? i + 1 : i;
                    raw.RemoveAt(removeIndex);
                }

                changed = true;
                break;
            }
        } while (changed);

        return raw.OrderBy(x => x.Index).ToList();
    }

    public static TrendType DetectTrend(List<SwingPoint> swings)
    {
        var highs = swings.Where(x => x.Type == SwingType.High).TakeLast(3).ToList();
        var lows = swings.Where(x => x.Type == SwingType.Low).TakeLast(3).ToList();
        if (highs.Count < 2 || lows.Count < 2) return TrendType.Unknown;

        bool hh = highs[^1].Level > highs[^2].Level;
        bool hl = lows[^1].Level > lows[^2].Level;
        bool lh = highs[^1].Level < highs[^2].Level;
        bool ll = lows[^1].Level < lows[^2].Level;

        if (hh && hl) return TrendType.Bullish;
        if (lh && ll) return TrendType.Bearish;
        return TrendType.Sideways;
    }

    public static List<StructureBreak> DetectBosChoch(
        List<Candle> candles,
        List<SwingPoint> swings,
        bool closeBreak = true)
    {
        var result = new List<StructureBreak>();
        if (swings.Count < 4) return result;

        Direction currentTrend = Direction.None;
        var brokenSwingIndexes = new HashSet<int>();

        for (int s = 0; s < swings.Count; s++)
        {
            var swing = swings[s];
            if (brokenSwingIndexes.Contains(swing.Index)) continue;

            for (int i = swing.Index + 2; i < candles.Count; i++)
            {
                bool bullishBreak = swing.Type == SwingType.High &&
                    (closeBreak ? candles[i].Close > swing.Level : candles[i].High > swing.Level);

                bool bearishBreak = swing.Type == SwingType.Low &&
                    (closeBreak ? candles[i].Close < swing.Level : candles[i].Low < swing.Level);

                if (!bullishBreak && !bearishBreak) continue;

                var direction = bullishBreak ? Direction.Bullish : Direction.Bearish;
                bool isChoch = currentTrend != Direction.None && direction != currentTrend;
                bool isBos = !isChoch;

                result.Add(new StructureBreak
                {
                    Index = swing.Index,
                    Direction = direction,
                    IsBos = isBos,
                    IsChoch = isChoch,
                    Level = swing.Level,
                    BrokenIndex = i,
                    BrokenTime = candles[i].Time
                });

                currentTrend = direction;
                brokenSwingIndexes.Add(swing.Index);
                break;
            }
        }

        return result;
    }

    public static List<PriceZone> BuildSupportResistanceZones(
        List<Candle> candles,
        List<SwingPoint> swings,
        decimal tolerancePercent = 0.005m,
        string sourceTimeFrame = "")
    {
        var zones = new List<PriceZone>();

        foreach (var swing in swings)
        {
            var type = swing.Type == SwingType.Low ? ZoneType.Support : ZoneType.Resistance;
            decimal tolerance = swing.Level * tolerancePercent;
            decimal low = swing.Level - tolerance;
            decimal high = swing.Level + tolerance;

            var existing = zones.FirstOrDefault(z => z.Type == type && low <= z.High && high >= z.Low);
            if (existing == null)
            {
                zones.Add(new PriceZone
                {
                    Type = type,
                    Low = low,
                    High = high,
                    StartIndex = swing.Index,
                    EndIndex = swing.Index,
                    TouchCount = 1,
                    Score = 10,
                    SourceTimeFrame = sourceTimeFrame
                });
            }
            else
            {
                existing.Low = Math.Min(existing.Low, low);
                existing.High = Math.Max(existing.High, high);
                existing.EndIndex = Math.Max(existing.EndIndex, swing.Index);
                existing.TouchCount++;
                existing.Score += 10;
            }
        }

        return zones;
    }

    // FVG definition mirrors the Python file: bullish if previous high < next low and current candle is bullish;
    // bearish if previous low > next high and current candle is bearish.
    public static List<PriceZone> DetectFvg(
        List<Candle> candles,
        bool joinConsecutive = true,
        string sourceTimeFrame = "")
    {
        var zones = new List<PriceZone>();

        for (int i = 1; i < candles.Count - 1; i++)
        {
            var prev = candles[i - 1];
            var current = candles[i];
            var next = candles[i + 1];

            if (current.Close > current.Open && prev.High < next.Low)
            {
                zones.Add(new PriceZone
                {
                    Type = ZoneType.BullishFvg,
                    Low = prev.High,
                    High = next.Low,
                    StartIndex = i,
                    EndIndex = i,
                    Score = 25,
                    SourceTimeFrame = sourceTimeFrame
                });
            }
            else if (current.Close < current.Open && prev.Low > next.High)
            {
                zones.Add(new PriceZone
                {
                    Type = ZoneType.BearishFvg,
                    Low = next.High,
                    High = prev.Low,
                    StartIndex = i,
                    EndIndex = i,
                    Score = 25,
                    SourceTimeFrame = sourceTimeFrame
                });
            }
        }

        if (joinConsecutive) zones = MergeConsecutiveSameTypeZones(zones);

        for (int z = 0; z < zones.Count; z++)
        {
            var zone = zones[z];
            int start = Math.Min(candles.Count - 1, zone.EndIndex + 2);
            for (int i = start; i < candles.Count; i++)
            {
                if (zone.Type == ZoneType.BullishFvg && candles[i].Low <= zone.High)
                {
                    zone.MitigatedIndex = i;
                    break;
                }

                if (zone.Type == ZoneType.BearishFvg && candles[i].High >= zone.Low)
                {
                    zone.MitigatedIndex = i;
                    break;
                }
            }
        }

        return zones;
    }

    public static List<PriceZone> DetectOrderBlocks(
        List<Candle> candles,
        List<SwingPoint> swings,
        bool closeMitigation = false,
        string sourceTimeFrame = "")
    {
        var zones = new List<PriceZone>();
        var swingHighs = swings.Where(x => x.Type == SwingType.High).ToList();
        var swingLows = swings.Where(x => x.Type == SwingType.Low).ToList();
        var crossed = new HashSet<int>();

        for (int i = 2; i < candles.Count; i++)
        {
            var lastHigh = swingHighs.LastOrDefault(x => x.Index < i);
            if (lastHigh != null && !crossed.Contains(lastHigh.Index) && candles[i].Close > lastHigh.Level)
            {
                crossed.Add(lastHigh.Index);
                var obCandleIndex = FindLowestLowIndex(candles, lastHigh.Index + 1, i - 1);
                if (obCandleIndex >= 0)
                {
                    zones.Add(new PriceZone
                    {
                        Type = ZoneType.BullishOrderBlock,
                        Low = candles[obCandleIndex].Low,
                        High = candles[obCandleIndex].High,
                        StartIndex = obCandleIndex,
                        EndIndex = obCandleIndex,
                        Score = 35 + CalculateThreeCandleVolumeScore(candles, i),
                        SourceTimeFrame = sourceTimeFrame
                    });
                }
            }

            var lastLow = swingLows.LastOrDefault(x => x.Index < i);
            if (lastLow != null && !crossed.Contains(lastLow.Index) && candles[i].Close < lastLow.Level)
            {
                crossed.Add(lastLow.Index);
                var obCandleIndex = FindHighestHighIndex(candles, lastLow.Index + 1, i - 1);
                if (obCandleIndex >= 0)
                {
                    zones.Add(new PriceZone
                    {
                        Type = ZoneType.BearishOrderBlock,
                        Low = candles[obCandleIndex].Low,
                        High = candles[obCandleIndex].High,
                        StartIndex = obCandleIndex,
                        EndIndex = obCandleIndex,
                        Score = 35 + CalculateThreeCandleVolumeScore(candles, i),
                        SourceTimeFrame = sourceTimeFrame
                    });
                }
            }
        }

        foreach (var zone in zones)
        {
            for (int i = zone.EndIndex + 1; i < candles.Count; i++)
            {
                bool mitigated = zone.Type == ZoneType.BullishOrderBlock
                    ? (!closeMitigation && candles[i].Low < zone.Low) || (closeMitigation && Math.Min(candles[i].Open, candles[i].Close) < zone.Low)
                    : (!closeMitigation && candles[i].High > zone.High) || (closeMitigation && Math.Max(candles[i].Open, candles[i].Close) > zone.High);

                if (mitigated)
                {
                    zone.MitigatedIndex = i;
                    break;
                }
            }
        }

        return zones;
    }

    // Supply/demand = base candles followed by impulse away.
    public static List<PriceZone> DetectSupplyDemandZones(
        List<Candle> candles,
        int maxBaseCandles = 4,
        decimal baseRangeAtrMultiplier = 0.8m,
        decimal impulseAtrMultiplier = 1.5m,
        string sourceTimeFrame = "")
    {
        var zones = new List<PriceZone>();
        if (candles.Count < 20) return zones;
        var atr = CalculateAtr(candles, 14);

        for (int start = 14; start < candles.Count - 2; start++)
        {
            for (int count = 1; count <= maxBaseCandles && start + count < candles.Count; count++)
            {
                int end = start + count - 1;
                int impulseIndex = end + 1;
                var baseCandles = candles.Skip(start).Take(count).ToList();
                decimal baseHigh = baseCandles.Max(x => x.High);
                decimal baseLow = baseCandles.Min(x => x.Low);
                decimal baseRange = baseHigh - baseLow;
                decimal currentAtr = atr[end];
                if (currentAtr <= 0 || baseRange > currentAtr * baseRangeAtrMultiplier) continue;

                var impulse = candles[impulseIndex];
                decimal impulseRange = impulse.High - impulse.Low;
                if (impulseRange <= 0) continue;
                decimal impulseBodyPercent = Math.Abs(impulse.Close - impulse.Open) / impulseRange;

                bool strongImpulse = impulseRange >= currentAtr * impulseAtrMultiplier && impulseBodyPercent >= 0.60m;
                if (!strongImpulse) continue;

                if (impulse.Close > impulse.Open)
                {
                    zones.Add(new PriceZone
                    {
                        Type = ZoneType.Demand,
                        Low = baseLow,
                        High = baseHigh,
                        StartIndex = start,
                        EndIndex = end,
                        Score = 30 + Math.Min(30, impulseRange / currentAtr * 10),
                        SourceTimeFrame = sourceTimeFrame
                    });
                }
                else if (impulse.Close < impulse.Open)
                {
                    zones.Add(new PriceZone
                    {
                        Type = ZoneType.Supply,
                        Low = baseLow,
                        High = baseHigh,
                        StartIndex = start,
                        EndIndex = end,
                        Score = 30 + Math.Min(30, impulseRange / currentAtr * 10),
                        SourceTimeFrame = sourceTimeFrame
                    });
                }
            }
        }

        return MergeOverlappingZones(zones);
    }

    public static List<PriceZone> DetectLiquidity(
        List<Candle> candles,
        List<SwingPoint> swings,
        decimal rangePercent = 0.01m,
        string sourceTimeFrame = "")
    {
        var zones = new List<PriceZone>();
        if (candles.Count == 0) return zones;

        decimal pipRange = (candles.Max(x => x.High) - candles.Min(x => x.Low)) * rangePercent;
        if (pipRange <= 0) return zones;

        BuildLiquidityForType(SwingType.High, ZoneType.BullishLiquidity);
        BuildLiquidityForType(SwingType.Low, ZoneType.BearishLiquidity);
        return zones;

        void BuildLiquidityForType(SwingType swingType, ZoneType zoneType)
        {
            var candidates = swings.Where(x => x.Type == swingType).OrderBy(x => x.Index).ToList();
            var used = new HashSet<int>();

            foreach (var s in candidates)
            {
                if (used.Contains(s.Index)) continue;
                decimal low = s.Level - pipRange;
                decimal high = s.Level + pipRange;
                var group = candidates.Where(x => x.Index >= s.Index && x.Level >= low && x.Level <= high && !used.Contains(x.Index)).ToList();
                if (group.Count < 2) continue;

                foreach (var g in group) used.Add(g.Index);

                var zone = new PriceZone
                {
                    Type = zoneType,
                    Low = group.Min(x => x.Level) - pipRange * 0.25m,
                    High = group.Max(x => x.Level) + pipRange * 0.25m,
                    StartIndex = group.First().Index,
                    EndIndex = group.Last().Index,
                    TouchCount = group.Count,
                    Score = 20 + group.Count * 10,
                    SourceTimeFrame = sourceTimeFrame
                };

                for (int i = zone.EndIndex + 1; i < candles.Count; i++)
                {
                    if (zoneType == ZoneType.BullishLiquidity && candles[i].High >= zone.High)
                    {
                        zone.SweptIndex = i;
                        break;
                    }

                    if (zoneType == ZoneType.BearishLiquidity && candles[i].Low <= zone.Low)
                    {
                        zone.SweptIndex = i;
                        break;
                    }
                }

                zones.Add(zone);
            }
        }
    }

    public static DisplacementResult DetectBestDisplacement(List<Candle> candles)
    {
        if (candles.Count < 25) return new DisplacementResult();

        // Priority: 1 candle first, then 2, then 3.
        foreach (int count in new[] { 1, 2, 3 })
        {
            var result = DetectDisplacement(candles, candles.Count - 1, count);
            if (result.IsDisplacement) return result;
        }

        return new DisplacementResult();
    }

    public static DisplacementResult DetectDisplacement(
        List<Candle> candles,
        int index,
        int candleCount,
        int atrPeriod = 14,
        int volumePeriod = 20)
    {
        if (index < candleCount - 1 || index < atrPeriod || index < volumePeriod) return new DisplacementResult();

        var seq = candles.Skip(index - candleCount + 1).Take(candleCount).ToList();
        bool allBullish = seq.All(x => x.Close > x.Open);
        bool allBearish = seq.All(x => x.Close < x.Open);
        if (!allBullish && !allBearish) return new DisplacementResult();

        var direction = allBullish ? Direction.Bullish : Direction.Bearish;
        var atr = CalculateAtr(candles, atrPeriod);
        decimal currentAtr = atr[index];
        if (currentAtr <= 0) return new DisplacementResult();

        decimal avgVolume =(decimal)candles.Skip(index - volumePeriod + 1).Take(volumePeriod).Average(x => x.Volume);
        decimal totalMove = direction == Direction.Bullish
            ? seq.Last().Close - seq.First().Open
            : seq.First().Open - seq.Last().Close;

        bool eachStrong = seq.All(c =>
        {
            decimal range = c.High - c.Low;
            if (range <= 0) return false;
            decimal body = Math.Abs(c.Close - c.Open);
            decimal bodyPct = body / range;
            bool closeNearHigh = c.Close >= c.Low + range * 0.75m;
            bool closeNearLow = c.Close <= c.Low + range * 0.25m;

            return bodyPct >= (candleCount == 1 ? 0.60m : 0.55m)
                && ((direction == Direction.Bullish && closeNearHigh) || (direction == Direction.Bearish && closeNearLow));
        });

        bool totalMoveStrong = totalMove >= currentAtr * (candleCount == 1 ? 1.2m : 1.1m);
        bool volumeStrong = avgVolume <= 0 || seq.Max(x => x.Volume) >= avgVolume * 1.2m;
        bool noHeavyOverlap = true;

        for (int i = 1; i < seq.Count; i++)
        {
            if (direction == Direction.Bullish && seq[i].Close <= seq[i - 1].Open) noHeavyOverlap = false;
            if (direction == Direction.Bearish && seq[i].Close >= seq[i - 1].Open) noHeavyOverlap = false;
        }

        bool isDisplacement = eachStrong && totalMoveStrong && volumeStrong && noHeavyOverlap;
        decimal atrRatio = totalMove / currentAtr;
        decimal volumeRatio = avgVolume <= 0 ? 1 : seq.Max(x => x.Volume) / avgVolume;
        decimal avgBody = seq.Average(c =>
        {
            decimal range = c.High - c.Low;
            return range <= 0 ? 0 : Math.Abs(c.Close - c.Open) / range;
        });

        return new DisplacementResult
        {
            IsDisplacement = isDisplacement,
            Direction = isDisplacement ? direction : Direction.None,
            CandleCount = candleCount,
            BodyPercent = avgBody,
            AtrRatio = atrRatio,
            VolumeRatio = volumeRatio,
            Score = isDisplacement ? Math.Min(100, 45 + atrRatio * 20 + volumeRatio * 10 + (4 - candleCount) * 5) : 0
        };
    }

    public static List<decimal> CalculateAtr(List<Candle> candles, int period = 14)
    {
        var trueRanges = new List<decimal>();
        for (int i = 0; i < candles.Count; i++)
        {
            if (i == 0)
            {
                trueRanges.Add(candles[i].High - candles[i].Low);
                continue;
            }

            decimal highLow = candles[i].High - candles[i].Low;
            decimal highClose = Math.Abs(candles[i].High - candles[i - 1].Close);
            decimal lowClose = Math.Abs(candles[i].Low - candles[i - 1].Close);
            trueRanges.Add(Math.Max(highLow, Math.Max(highClose, lowClose)));
        }

        var atr = new List<decimal>();
        for (int i = 0; i < trueRanges.Count; i++)
        {
            int start = Math.Max(0, i - period + 1);
            atr.Add(trueRanges.Skip(start).Take(i - start + 1).Average());
        }

        return atr;
    }

    public static bool IsCandleTouchingZone(Candle candle, PriceZone zone)
    {
        return candle.Low <= zone.High && candle.High >= zone.Low;
    }

    private static List<PriceZone> MergeConsecutiveSameTypeZones(List<PriceZone> zones)
    {
        if (zones.Count <= 1) return zones;
        var output = new List<PriceZone>();

        foreach (var zone in zones.OrderBy(x => x.StartIndex))
        {
            var last = output.LastOrDefault();
            if (last != null && last.Type == zone.Type && zone.StartIndex <= last.EndIndex + 1)
            {
                last.Low = Math.Min(last.Low, zone.Low);
                last.High = Math.Max(last.High, zone.High);
                last.EndIndex = Math.Max(last.EndIndex, zone.EndIndex);
                last.Score += zone.Score;
            }
            else
            {
                output.Add(zone);
            }
        }

        return output;
    }

    private static List<PriceZone> MergeOverlappingZones(List<PriceZone> zones)
    {
        var result = new List<PriceZone>();
        foreach (var zone in zones.OrderBy(x => x.Type).ThenBy(x => x.Low))
        {
            var existing = result.FirstOrDefault(x => x.Type == zone.Type && zone.Low <= x.High && zone.High >= x.Low);
            if (existing == null)
            {
                result.Add(zone);
            }
            else
            {
                existing.Low = Math.Min(existing.Low, zone.Low);
                existing.High = Math.Max(existing.High, zone.High);
                existing.StartIndex = Math.Min(existing.StartIndex, zone.StartIndex);
                existing.EndIndex = Math.Max(existing.EndIndex, zone.EndIndex);
                existing.Score = Math.Max(existing.Score, zone.Score) + 5;
            }
        }
        return result;
    }

    private static int FindLowestLowIndex(List<Candle> candles, int start, int end)
    {
        start = Math.Max(0, start);
        end = Math.Min(candles.Count - 1, end);
        if (start > end) return -1;

        int index = start;
        decimal min = candles[start].Low;
        for (int i = start + 1; i <= end; i++)
        {
            if (candles[i].Low <= min)
            {
                min = candles[i].Low;
                index = i;
            }
        }
        return index;
    }

    private static int FindHighestHighIndex(List<Candle> candles, int start, int end)
    {
        start = Math.Max(0, start);
        end = Math.Min(candles.Count - 1, end);
        if (start > end) return -1;

        int index = start;
        decimal max = candles[start].High;
        for (int i = start + 1; i <= end; i++)
        {
            if (candles[i].High >= max)
            {
                max = candles[i].High;
                index = i;
            }
        }
        return index;
    }

    private static decimal CalculateThreeCandleVolumeScore(List<Candle> candles, int index)
    {
        if (index < 2) return 0;
        decimal current = candles[index].Volume + candles[index - 1].Volume;
        decimal previous = candles[index - 2].Volume;
        if (previous <= 0) return 0;
        return Math.Min(20, current / previous * 5);
    }

    private static void MarkBrokenZones(List<Candle> candles, IEnumerable<PriceZone> zones)
    {
        foreach (var zone in zones)
        {
            for (int i = zone.EndIndex + 1; i < candles.Count; i++)
            {
                if (zone.ExpectedReactionDirection == Direction.Bullish && candles[i].Close < zone.Low)
                {
                    zone.IsBroken = true;
                    break;
                }

                if (zone.ExpectedReactionDirection == Direction.Bearish && candles[i].Close > zone.High)
                {
                    zone.IsBroken = true;
                    break;
                }
            }
        }
    }

    private static void ScoreZones(List<Candle> candles, SmcAnalysisResult result)
    {
        decimal currentPrice = candles[^1].Close;

        foreach (var z in result.AllZones)
        {
            if (z.IsBroken) z.Score -= 40;
            if (z.IsFresh) z.Score += 15;
            if (z.TouchCount >= 2) z.Score += z.TouchCount * 5;

            decimal mid = (z.Low + z.High) / 2m;
            if (mid > 0)
            {
                decimal distancePercent = Math.Abs(currentPrice - mid) / mid;
                if (distancePercent <= 0.01m) z.Score += 25;
                else if (distancePercent <= 0.02m) z.Score += 15;
                else if (distancePercent <= 0.05m) z.Score += 5;
            }
        }
    }
}

// ================================================================
// WINFORMS USAGE EXAMPLE
// ================================================================
/*
private readonly FmpService _fmp = new("YOUR_FMP_API_KEY");
private readonly SmcMultiTimeFrameAlertEngine _engine = new();

private async Task ScanTickerAsync(string ticker)
{
    // Stronger setup:
    var signal4h15m = await _engine.CheckFromFmpAsync(_fmp, ticker, "4hour", "15min");
    if (signal4h15m.ShouldAlert)
    {
        AddAlertToGrid(signal4h15m);
        _alertService.StartAlarm();
        return;
    }

    // Faster setup:
    var signal1h5m = await _engine.CheckFromFmpAsync(_fmp, ticker, "1hour", "5min");
    if (signal1h5m.ShouldAlert)
    {
        AddAlertToGrid(signal1h5m);
        _alertService.StartAlarm();
    }
}
*/
