using System;
using System.Collections.Generic;
using System.Text;

namespace Trading.Smc.v1
{
    public static class Helper
    {
        public static List<SessionResult> Sessions(IReadOnlyList<Candle> candles, TimeSpan start, TimeSpan end)
        {
            var results = new List<SessionResult>();
            double sessionHigh = double.MinValue;
            double sessionLow = double.MaxValue;
            bool wasActive = false;

            for (int i = 0; i < candles.Count; i++)
            {
                var time = candles[i].Time.TimeOfDay;
                bool active = start < end
                    ? time >= start && time <= end
                    : time >= start || time <= end;

                if (active && !wasActive)
                {
                    sessionHigh = candles[i].High;
                    sessionLow = candles[i].Low;
                }
                else if (active)
                {
                    sessionHigh = Math.Max(sessionHigh, candles[i].High);
                    sessionLow = Math.Min(sessionLow, candles[i].Low);
                }

                results.Add(new SessionResult
                {
                    Index = i,
                    Active = active,
                    High = active ? sessionHigh : null,
                    Low = active ? sessionLow : null
                });

                wasActive = active;
            }

            return results;
        }

        // ============================================================
        // Helpers
        // ============================================================

        public static PriceZone CreateZone(ZoneType type, Direction direction, int startIndex, int endIndex, DateTime startTime, double top, double bottom, string source)
        {
            if (bottom > top)
            {
                (top, bottom) = (bottom, top);
            }

            return new PriceZone
            {
                Type = type,
                Direction = direction,
                StartIndex = startIndex,
                EndIndex = endIndex,
                StartTime = startTime,
                Top = top,
                Bottom = bottom,
                Source = source,
                Strength = 1
            };
        }

        public static List<PriceZone> JoinConsecutiveZones(List<PriceZone> zones)
        {
            if (zones.Count <= 1) return zones;
            var result = new List<PriceZone>();
            var current = zones[0];

            for (int i = 1; i < zones.Count; i++)
            {
                var next = zones[i];
                if (next.Type == current.Type && next.Direction == current.Direction && next.StartIndex == current.EndIndex + 1)
                {
                    current.Top = Math.Max(current.Top, next.Top);
                    current.Bottom = Math.Min(current.Bottom, next.Bottom);
                    current.EndIndex = next.EndIndex;
                    current.EndTime = next.EndTime;
                    current.Strength += next.Strength;
                }
                else
                {
                    result.Add(current);
                    current = next;
                }
            }

            result.Add(current);
            return result;
        }

        public static List<PriceZone> MergeSimilarZones(List<PriceZone> zones, double mergePercent)
        {
            if (zones.Count <= 1) return zones;
            var sorted = zones.OrderBy(z => z.Bottom).ThenBy(z => z.Top).ToList();
            var result = new List<PriceZone>();

            foreach (var zone in sorted)
            {
                var match = result.FirstOrDefault(x =>
                    x.Type == zone.Type &&
                    x.Direction == zone.Direction &&
                    Math.Abs(x.Mid - zone.Mid) / Math.Max(Math.Abs(zone.Mid), 0.0000001) <= mergePercent);

                if (match == null)
                {
                    result.Add(zone);
                }
                else
                {
                    match.Top = Math.Max(match.Top, zone.Top);
                    match.Bottom = Math.Min(match.Bottom, zone.Bottom);
                    match.EndIndex = Math.Max(match.EndIndex, zone.EndIndex);
                    match.Strength += zone.Strength;
                }
            }

            return result.OrderBy(x => x.StartIndex).ToList();
        }

        public static void MarkMitigation(IReadOnlyList<Candle> candles, List<PriceZone> zones, int startOffset = 1, bool closeMitigation = false)
        {
            foreach (var z in zones)
            {
                for (int i = z.EndIndex + startOffset; i < candles.Count; i++)
                {
                    bool mitigated;
                    if (closeMitigation)
                    {
                        mitigated = z.Contains(candles[i].Close);
                    }
                    else
                    {
                        mitigated = z.Overlaps(candles[i].High, candles[i].Low);
                    }

                    if (mitigated)
                    {
                        z.IsMitigated = true;
                        z.MitigatedIndex = i;
                        break;
                    }
                }
            }
        }

        public static Direction ExpectedReactionDirection(PriceZone zone)
        {
            return zone.Type switch
            {
                ZoneType.Demand => Direction.Bullish,
                ZoneType.Support => Direction.Bullish,
                ZoneType.Supply => Direction.Bearish,
                ZoneType.Resistance => Direction.Bearish,
                ZoneType.OrderBlock => zone.Direction,
                ZoneType.FairValueGap => zone.Direction,
                _ => Direction.None
            };
        }

        public static DateTime FloorTime(DateTime time, TimeSpan span)
        {
            long ticks = time.Ticks / span.Ticks * span.Ticks;
            return new DateTime(ticks, time.Kind);
        }

        public static void ValidateCandles(IReadOnlyList<Candle> candles)
        {
            if (candles == null) throw new ArgumentNullException(nameof(candles));
            if (candles.Count < 10) throw new ArgumentException("At least 10 candles are required.", nameof(candles));
        }
    }
}
