using System;
using System.Collections.Generic;
using System.Text;

namespace Displacement
{
    public abstract class CandleAnalyzerBase
    {
        protected readonly IReadOnlyList<Candle> _candles;
        protected readonly int _lookback;

        protected CandleAnalyzerBase(IReadOnlyList<Candle> candles, int lookback)
        {
            _candles = candles;
            _lookback = lookback;
        }

        public abstract bool IsBearishBreakout(int index);
        public abstract bool IsBearishDisplacement(int index, decimal bodyMultiplier);
        public abstract bool IsBullishBreakout(int index);
        public abstract bool IsBullishDisplacement(int index, decimal bodyMultiplier);
        public abstract bool IsBullishEngulfing(int index);
        public abstract bool IsFairValueGap(int index);
        public abstract bool IsInsideBar(int index);
        public abstract bool IsLiquiditySweep(int index);
        public abstract bool IsOrderBlock(int index);
    }

    public class CandleAnalyzer : CandleAnalyzerBase
    {
        public CandleAnalyzer(
            IReadOnlyList<Candle> candles,
            int lookback
            ) :base(candles, lookback)
        {
            
        }

        public override bool IsBearishBreakout(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsBearishDisplacement(int index, decimal bodyMultiplier)
        {
            return DisplacementDetector.IsBearishDisplacement(_candles, index, _lookback, bodyMultiplier);
        }

        public override bool IsBullishBreakout(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsBullishDisplacement(int index, decimal bodyMultiplier)
        {
            return DisplacementDetector.IsBullishDisplacement(_candles, index, _lookback, bodyMultiplier);
        }

        public override bool IsBullishEngulfing(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsFairValueGap(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsInsideBar(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsLiquiditySweep(int index)
        {
            throw new NotImplementedException();
        }

        public override bool IsOrderBlock(int index)
        {
            throw new NotImplementedException();
        }

        public DisplacementResult Detect(int index, decimal bodyMultiplier, string ticker, string timeFrame)
        {
           var isBullish = IsBullishDisplacement(index, bodyMultiplier);
           var isBearish = IsBearishDisplacement(index, bodyMultiplier);
           var currentCandle = _candles[index];

            return new DisplacementResult
            {
                IsDisplacement = isBullish || isBearish,
                Direction = isBullish ? "Bullish" : isBearish ? "Bearish" : "None",
                Ticker = ticker,
                TimeFrame = timeFrame,
                DateTime = ConvertEstToLocalTime(currentCandle?.Date ?? DateTime.MinValue)
            };
        }

        private DateTime ConvertEstToLocalTime(DateTime estDateTime)
        {
            try
            {
                // Get EST timezone (Eastern Standard Time)
                var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                // Convert from EST to local time
                return TimeZoneInfo.ConvertTime(estDateTime, estZone, TimeZoneInfo.Local);
            }
            catch
            {
                // If timezone conversion fails, return the original time
                return estDateTime;
            }
        }
    }

    public class DisplacementDetector
    {
        public static bool IsBullishDisplacement(
             IReadOnlyList<Candle> candles,
             int index,
             int lookback = 5,
             decimal bodyMultiplier = 1.5m)
        {
            if (index < lookback)
                return false;

            Candle current = candles[index];

            // Current candle must already be a strong bullish candle
            if (!current.IsStrongBullish)
                return false;

            var previous = candles
                .Skip(index - lookback)
                .Take(lookback)
                .ToList();

            decimal averageBody = previous.Average(c => c.Body);
            decimal highestHigh = previous.Max(c => c.High);

            return
                current.Body >= averageBody * bodyMultiplier &&
                current.Close > highestHigh;
        }

        public static bool IsBearishDisplacement(
             IReadOnlyList<Candle> candles,
             int index,
             int lookback = 5,
             decimal bodyMultiplier = 1.5m)
        {
            if (index < lookback)
                return false;

            Candle current = candles[index];
            // Current candle must already be a strong bearish candle
            if (!current.IsStrongBearish)
                return false;

            var previous = candles
                .Skip(index - lookback)
                .Take(lookback)
                .ToList();

            decimal averageBody = previous.Average(c => c.Body);
            decimal lowestLow = previous.Min(c => c.Low);
            return
                current.Body >= averageBody * bodyMultiplier &&
                current.Close < lowestLow;
        }
    }
}
