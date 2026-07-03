namespace Trading.Smc.v1;


public class SmcServices
{
    // ============================================================
    // Main public API
    // ============================================================

    public static MarketAnalysis Analyze(IReadOnlyList<Candle> candles, AnalyzeOptions? options = null)
    {
        options ??= new AnalyzeOptions();
        Helper.ValidateCandles(candles);

        var swings = DetectSwingPoints(candles, options.SwingLength);
        var trend = DetectTrend(swings);
        var fvg = DetectFairValueGaps(candles, joinConsecutive: true);
        var orderBlocks = DetectOrderBlocks(candles, swings, options.CloseMitigation);
        var sr = DetectSupportResistanceZones(candles, swings, options.SupportResistanceLookback, options.ZoneMergePercent);
        var supplyDemand = DetectSupplyDemandZones(candles, swings);
        var zones = Helper.MergeSimilarZones(fvg.Concat(orderBlocks).Concat(sr).Concat(supplyDemand).ToList(), options.ZoneMergePercent);
        var structure = DetectBosChoch(candles, swings, options.CloseBreak);
        var liquidity = DetectLiquidity(candles, swings, options.LiquidityRangePercent);
        var retracements = DetectRetracementsToZones(candles, zones, options.RecentBarsForRetracement);

        return new MarketAnalysis
        {
            Trend = trend,
            Swings = swings,
            Zones = zones,
            StructureBreaks = structure,
            Liquidity = liquidity,
            Retracements = retracements
        };
    }

    /// <summary>
    /// Higher timeframe finds zones. Lower timeframe confirms CHOCH after price retraces into one of those zones.
    /// Example: 1H or 4H zones, then 15m or 5m CHOCH confirmation.
    /// </summary>
    public static AlertSignal GenerateRetracementChochAlert(
        IReadOnlyList<Candle> higherTimeframeCandles,
        IReadOnlyList<Candle> lowerTimeframeCandles,
        AnalyzeOptions? higherOptions = null,
        AnalyzeOptions? lowerOptions = null)
    {
        higherOptions ??= new AnalyzeOptions { SwingLength = 5, RecentBarsForRetracement = 5 };
        lowerOptions ??= new AnalyzeOptions { SwingLength = 3, RecentBarsForRetracement = 3 };

        var ht = Analyze(higherTimeframeCandles, higherOptions);
        var lt = Analyze(lowerTimeframeCandles, lowerOptions);

        var current = lowerTimeframeCandles[^1];
        var activeZones = ht.Zones
            .Where(z => !z.IsMitigated)
            .Where(z => z.Type is ZoneType.FairValueGap or ZoneType.OrderBlock or ZoneType.Supply or ZoneType.Demand or ZoneType.Support or ZoneType.Resistance)
            .Where(z => z.Overlaps(current.High, current.Low))
            .OrderByDescending(z => z.Strength)
            .ThenByDescending(z => z.StartIndex)
            .ToList();

        if (activeZones.Count == 0)
        {
            return new AlertSignal { ShouldAlert = false, Message = "No higher-timeframe zone retracement." };
        }

        var lastChoch = lt.StructureBreaks
            .Where(x => x.Type == BreakType.CHOCH)
            .OrderByDescending(x => x.BrokenIndex)
            .FirstOrDefault();

        if (lastChoch == null)
        {
            return new AlertSignal { ShouldAlert = false, Message = "Price is in a higher-timeframe zone, but no lower-timeframe CHOCH yet." };
        }

        foreach (var zone in activeZones)
        {
            var expectedDirection = Helper.ExpectedReactionDirection(zone);
            if (expectedDirection == Direction.None || expectedDirection == lastChoch.Direction)
            {
                return new AlertSignal
                {
                    ShouldAlert = true,
                    Direction = lastChoch.Direction,
                    HigherTimeframeZoneIndex = zone.StartIndex,
                    LowerTimeframeChochIndex = lastChoch.BrokenIndex,
                    Time = lowerTimeframeCandles[lastChoch.BrokenIndex].Time,
                    Zone = zone,
                    Choch = lastChoch,
                    Message = $"ALERT: Price retraced into HTF {zone.Type} [{zone.Bottom:F2}-{zone.Top:F2}] and LTF created {lastChoch.Direction} CHOCH at {lastChoch.Level:F2}."
                };
            }
        }

        return new AlertSignal { ShouldAlert = false, Message = "Zone touched, but CHOCH direction does not match the expected reaction." };
    }

    // ============================================================
    // Trend
    // ============================================================

    public static Direction DetectTrend(IReadOnlyList<SwingPoint> swings)
    {
        var highs = swings.Where(x => x.Direction == Direction.Bullish).TakeLast(2).ToList();
        var lows = swings.Where(x => x.Direction == Direction.Bearish).TakeLast(2).ToList();

        if (highs.Count < 2 || lows.Count < 2) return Direction.None;

        bool higherHigh = highs[^1].Level > highs[^2].Level;
        bool higherLow = lows[^1].Level > lows[^2].Level;
        bool lowerHigh = highs[^1].Level < highs[^2].Level;
        bool lowerLow = lows[^1].Level < lows[^2].Level;

        if (higherHigh && higherLow) return Direction.Bullish;
        if (lowerHigh && lowerLow) return Direction.Bearish;
        return Direction.None;
    }

    // ============================================================
    // Swing highs/lows
    // ============================================================

    public static List<SwingPoint> DetectSwingPoints(IReadOnlyList<Candle> candles, int swingLength = 5)
    {
        Helper.ValidateCandles(candles);
        swingLength = Math.Max(1, swingLength);

        var raw = new List<SwingPoint>();

        for (int i = swingLength; i < candles.Count - swingLength; i++)
        {
            bool isHigh = true;
            bool isLow = true;

            for (int j = i - swingLength; j <= i + swingLength; j++)
            {
                if (candles[i].High < candles[j].High) isHigh = false;
                if (candles[i].Low > candles[j].Low) isLow = false;
                if (!isHigh && !isLow) break;
            }

            if (isHigh)
            {
                raw.Add(new SwingPoint
                {
                    Index = i,
                    Time = candles[i].Time,
                    Direction = Direction.Bullish,
                    Level = candles[i].High
                });
            }
            else if (isLow)
            {
                raw.Add(new SwingPoint
                {
                    Index = i,
                    Time = candles[i].Time,
                    Direction = Direction.Bearish,
                    Level = candles[i].Low
                });
            }
        }

        return RemoveDuplicateConsecutiveSwings(raw);
    }

    private static List<SwingPoint> RemoveDuplicateConsecutiveSwings(List<SwingPoint> raw)
    {
        if (raw.Count <= 1) return raw;

        var result = new List<SwingPoint>();

        foreach (var sp in raw)
        {
            if (result.Count == 0)
            {
                result.Add(sp);
                continue;
            }

            var last = result[^1];
            if (last.Direction != sp.Direction)
            {
                result.Add(sp);
                continue;
            }

            if (sp.Direction == Direction.Bullish)
            {
                if (sp.Level > last.Level) result[^1] = sp;
            }
            else if (sp.Direction == Direction.Bearish)
            {
                if (sp.Level < last.Level) result[^1] = sp;
            }
        }

        return result;
    }

    // ============================================================
    // Fair Value Gaps
    // ============================================================

    public static List<PriceZone> DetectFairValueGaps(IReadOnlyList<Candle> candles, bool joinConsecutive = true)
    {
        Helper.ValidateCandles(candles);
        var zones = new List<PriceZone>();

        for (int i = 1; i < candles.Count - 1; i++)
        {
            var prev = candles[i - 1];
            var cur = candles[i];
            var next = candles[i + 1];

            if (prev.High < next.Low && cur.IsBullish)
            {
                zones.Add(Helper.CreateZone(ZoneType.FairValueGap, Direction.Bullish, i, i, candles[i].Time, next.Low, prev.High, "FVG"));
            }
            else if (prev.Low > next.High && cur.IsBearish)
            {
                zones.Add(Helper.CreateZone(ZoneType.FairValueGap, Direction.Bearish, i, i, candles[i].Time, prev.Low, next.High, "FVG"));
            }
        }

        if (joinConsecutive)
        {
            zones = Helper.JoinConsecutiveZones(zones);
        }

        Helper.MarkMitigation(candles, zones, startOffset: 2);
        return zones;
    }

    // ============================================================
    // BOS / CHOCH
    // ============================================================

    public static List<StructureBreak> DetectBosChoch(IReadOnlyList<Candle> candles, IReadOnlyList<SwingPoint> swings, bool closeBreak = true)
    {
        var results = new List<StructureBreak>();
        if (swings.Count < 4) return results;

        Direction currentBias = Direction.None;

        for (int i = 3; i < swings.Count; i++)
        {
            var a = swings[i - 3];
            var b = swings[i - 2];
            var c = swings[i - 1];
            var d = swings[i];

            bool patternBull = a.Direction == Direction.Bearish && b.Direction == Direction.Bullish && c.Direction == Direction.Bearish && d.Direction == Direction.Bullish;
            bool patternBear = a.Direction == Direction.Bullish && b.Direction == Direction.Bearish && c.Direction == Direction.Bullish && d.Direction == Direction.Bearish;

            Direction newBias = Direction.None;
            BreakType type;
            double level;
            int signalIndex = c.Index;

            if (patternBull && c.Level > a.Level && d.Level > b.Level)
            {
                newBias = Direction.Bullish;
                type = currentBias == Direction.Bearish ? BreakType.CHOCH : BreakType.BOS;
                level = b.Level;
            }
            else if (patternBear && c.Level < a.Level && d.Level < b.Level)
            {
                newBias = Direction.Bearish;
                type = currentBias == Direction.Bullish ? BreakType.CHOCH : BreakType.BOS;
                level = b.Level;
            }
            else
            {
                continue;
            }

            int? brokenIndex = FindBreakIndex(candles, signalIndex + 1, level, newBias, closeBreak);
            if (brokenIndex.HasValue)
            {
                results.Add(new StructureBreak
                {
                    Type = type,
                    Direction = newBias,
                    SignalIndex = signalIndex,
                    BrokenIndex = brokenIndex.Value,
                    Time = candles[brokenIndex.Value].Time,
                    Level = level,
                    Description = $"{newBias} {type} broke level {level:F2}"
                });
                currentBias = newBias;
            }
        }

        return results;
    }

    private static int? FindBreakIndex(IReadOnlyList<Candle> candles, int start, double level, Direction direction, bool closeBreak)
    {
        for (int i = Math.Max(0, start); i < candles.Count; i++)
        {
            double value = direction == Direction.Bullish
                ? (closeBreak ? candles[i].Close : candles[i].High)
                : (closeBreak ? candles[i].Close : candles[i].Low);

            if (direction == Direction.Bullish && value > level) return i;
            if (direction == Direction.Bearish && value < level) return i;
        }
        return null;
    }

    // ============================================================
    // Order Blocks
    // ============================================================

    public static List<PriceZone> DetectOrderBlocks(IReadOnlyList<Candle> candles, IReadOnlyList<SwingPoint> swings, bool closeMitigation = false)
    {
        Helper.ValidateCandles(candles);
        var zones = new List<PriceZone>();
        var swingHighs = swings.Where(x => x.Direction == Direction.Bullish).OrderBy(x => x.Index).ToList();
        var swingLows = swings.Where(x => x.Direction == Direction.Bearish).OrderBy(x => x.Index).ToList();
        var crossed = new HashSet<int>();

        for (int i = 1; i < candles.Count; i++)
        {
            var lastSwingHigh = swingHighs.LastOrDefault(x => x.Index < i);
            if (lastSwingHigh != null && !crossed.Contains(lastSwingHigh.Index) && candles[i].Close > lastSwingHigh.Level)
            {
                crossed.Add(lastSwingHigh.Index);
                int obIndex = FindLastOppositeCandle(candles, lastSwingHigh.Index + 1, i - 1, Direction.Bearish);
                if (obIndex < 0) obIndex = Math.Max(0, i - 1);

                var z = Helper.CreateZone(ZoneType.OrderBlock, Direction.Bullish, obIndex, i, candles[obIndex].Time, candles[obIndex].High, candles[obIndex].Low, "Bullish OB");
                z.Strength = VolumeStrength(candles, i);
                zones.Add(z);
            }

            var lastSwingLow = swingLows.LastOrDefault(x => x.Index < i);
            if (lastSwingLow != null && !crossed.Contains(lastSwingLow.Index) && candles[i].Close < lastSwingLow.Level)
            {
                crossed.Add(lastSwingLow.Index);
                int obIndex = FindLastOppositeCandle(candles, lastSwingLow.Index + 1, i - 1, Direction.Bullish);
                if (obIndex < 0) obIndex = Math.Max(0, i - 1);

                var z = Helper.CreateZone(ZoneType.OrderBlock, Direction.Bearish, obIndex, i, candles[obIndex].Time, candles[obIndex].High, candles[obIndex].Low, "Bearish OB");
                z.Strength = VolumeStrength(candles, i);
                zones.Add(z);
            }
        }

        Helper.MarkMitigation(candles, zones, startOffset: 1, closeMitigation);
        return zones;
    }

    private static int FindLastOppositeCandle(IReadOnlyList<Candle> candles, int start, int end, Direction targetCandleDirection)
    {
        for (int i = end; i >= start; i--)
        {
            if (targetCandleDirection == Direction.Bullish && candles[i].IsBullish) return i;
            if (targetCandleDirection == Direction.Bearish && candles[i].IsBearish) return i;
        }
        return -1;
    }

    private static double VolumeStrength(IReadOnlyList<Candle> candles, int i)
    {
        double cur = candles[i].Volume;
        double p1 = i >= 1 ? candles[i - 1].Volume : 0;
        double p2 = i >= 2 ? candles[i - 2].Volume : 0;
        double highVolume = cur + p1;
        double lowVolume = p2;
        double max = Math.Max(highVolume, lowVolume);
        if (max <= 0) return 100;
        return Math.Min(highVolume, lowVolume) / max * 100.0;
    }

    // ============================================================
    // Supply / Demand and Support / Resistance
    // ============================================================

    public static List<PriceZone> DetectSupplyDemandZones(IReadOnlyList<Candle> candles, IReadOnlyList<SwingPoint> swings)
    {
        var zones = new List<PriceZone>();

        foreach (var sp in swings)
        {
            int i = sp.Index;
            var c = candles[i];
            if (sp.Direction == Direction.Bullish)
            {
                zones.Add(Helper.CreateZone(ZoneType.Supply, Direction.Bearish, i, i, c.Time, c.High, Math.Max(c.Open, c.Close), "Supply from swing high"));
            }
            else if (sp.Direction == Direction.Bearish)
            {
                zones.Add(Helper.CreateZone(ZoneType.Demand, Direction.Bullish, i, i, c.Time, Math.Min(c.Open, c.Close), c.Low, "Demand from swing low"));
            }
        }

        return zones;
    }

    public static List<PriceZone> DetectSupportResistanceZones(
        IReadOnlyList<Candle> candles,
        IReadOnlyList<SwingPoint> swings,
        int lookback = 100,
        double mergePercent = 0.003)
    {
        var recentSwings = swings.Where(x => x.Index >= Math.Max(0, candles.Count - lookback)).ToList();
        var zones = new List<PriceZone>();
        double priceRange = candles.Max(x => x.High) - candles.Min(x => x.Low);
        double tolerance = Math.Max(priceRange * mergePercent, 0.0000001);

        foreach (var group in recentSwings.GroupBy(s => s.Direction))
        {
            var sorted = group.OrderBy(x => x.Level).ToList();
            var bucket = new List<SwingPoint>();

            foreach (var sp in sorted)
            {
                if (bucket.Count == 0 || Math.Abs(sp.Level - bucket.Average(x => x.Level)) <= tolerance)
                {
                    bucket.Add(sp);
                }
                else
                {
                    AddSupportResistanceBucket(candles, zones, bucket, tolerance);
                    bucket = new List<SwingPoint> { sp };
                }
            }

            AddSupportResistanceBucket(candles, zones, bucket, tolerance);
        }

        return zones;
    }

    private static void AddSupportResistanceBucket(IReadOnlyList<Candle> candles, List<PriceZone> zones, List<SwingPoint> bucket, double tolerance)
    {
        if (bucket.Count < 2) return;
        double level = bucket.Average(x => x.Level);
        var dir = bucket[0].Direction;
        var type = dir == Direction.Bullish ? ZoneType.Resistance : ZoneType.Support;
        var reaction = dir == Direction.Bullish ? Direction.Bearish : Direction.Bullish;
        int start = bucket.Min(x => x.Index);
        int end = bucket.Max(x => x.Index);
        var zone = Helper.CreateZone(type, reaction, start, end, candles[start].Time, level + tolerance, level - tolerance, type.ToString());
        zone.Strength = bucket.Count;
        zones.Add(zone);
    }

    // ============================================================
    // Liquidity
    // ============================================================

    public static List<LiquidityEvent> DetectLiquidity(IReadOnlyList<Candle> candles, IReadOnlyList<SwingPoint> swings, double rangePercent = 0.01)
    {
        var events = new List<LiquidityEvent>();
        if (candles.Count == 0 || swings.Count == 0) return events;

        double range = candles.Max(x => x.High) - candles.Min(x => x.Low);
        double tolerance = range * rangePercent;
        var used = new HashSet<int>();

        foreach (var side in new[] { Direction.Bullish, Direction.Bearish })
        {
            var candidates = swings.Where(x => x.Direction == side).OrderBy(x => x.Index).ToList();
            for (int i = 0; i < candidates.Count; i++)
            {
                var first = candidates[i];
                if (used.Contains(first.Index)) continue;

                var grouped = new List<SwingPoint> { first };
                double low = first.Level - tolerance;
                double high = first.Level + tolerance;
                int? swept = FindLiquiditySweepIndex(candles, first.Index + 1, side, high, low);

                for (int j = i + 1; j < candidates.Count; j++)
                {
                    var next = candidates[j];
                    if (swept.HasValue && next.Index >= swept.Value) break;
                    if (next.Level >= low && next.Level <= high)
                    {
                        grouped.Add(next);
                        used.Add(next.Index);
                    }
                }

                if (grouped.Count > 1)
                {
                    events.Add(new LiquidityEvent
                    {
                        Direction = side,
                        StartIndex = grouped[0].Index,
                        EndIndex = grouped[^1].Index,
                        SweptIndex = swept,
                        Level = grouped.Average(x => x.Level),
                        Top = grouped.Max(x => x.Level) + tolerance,
                        Bottom = grouped.Min(x => x.Level) - tolerance
                    });
                }
            }
        }

        return events;
    }

    private static int? FindLiquiditySweepIndex(IReadOnlyList<Candle> candles, int start, Direction side, double rangeHigh, double rangeLow)
    {
        for (int i = start; i < candles.Count; i++)
        {
            if (side == Direction.Bullish && candles[i].High >= rangeHigh) return i;
            if (side == Direction.Bearish && candles[i].Low <= rangeLow) return i;
        }
        return null;
    }

    // ============================================================
    // Retracement to zones
    // ============================================================

    public static List<RetracementEvent> DetectRetracementsToZones(IReadOnlyList<Candle> candles, IReadOnlyList<PriceZone> zones, int recentBars = 3)
    {
        var results = new List<RetracementEvent>();
        int start = Math.Max(0, candles.Count - recentBars);

        for (int i = start; i < candles.Count; i++)
        {
            foreach (var z in zones.Where(x => !x.IsMitigated || x.MitigatedIndex == i))
            {
                if (z.Overlaps(candles[i].High, candles[i].Low))
                {
                    results.Add(new RetracementEvent
                    {
                        Index = i,
                        Time = candles[i].Time,
                        Zone = z,
                        Direction = Helper.ExpectedReactionDirection(z),
                        Price = candles[i].Close,
                        Message = $"Price retraced into {z.Type} zone [{z.Bottom:F2}-{z.Top:F2}]"
                    });
                }
            }
        }

        return results;
    }



    public static List<PreviousHighLowResult> PreviousHighLow(IReadOnlyList<Candle> candles, TimeSpan timeframe)
    {
        Helper.ValidateCandles(candles);
        var results = candles.Select((_, i) => new PreviousHighLowResult { Index = i }).ToList();

        var groups = candles
            .Select((c, i) => new { Candle = c, Index = i, Period = Helper.FloorTime(c.Time, timeframe) })
            .GroupBy(x => x.Period)
            .OrderBy(g => g.Key)
            .ToList();

        for (int g = 1; g < groups.Count; g++)
        {
            var prev = groups[g - 1].ToList();
            var cur = groups[g].ToList();
            double prevHigh = prev.Max(x => x.Candle.High);
            double prevLow = prev.Min(x => x.Candle.Low);
            bool brokenHigh = false;
            bool brokenLow = false;

            foreach (var item in cur)
            {
                brokenHigh = brokenHigh || item.Candle.High > prevHigh;
                brokenLow = brokenLow || item.Candle.Low < prevLow;
                results[item.Index].PreviousHigh = prevHigh;
                results[item.Index].PreviousLow = prevLow;
                results[item.Index].BrokenHigh = brokenHigh;
                results[item.Index].BrokenLow = brokenLow;
            }
        }

        return results;
    }
}
