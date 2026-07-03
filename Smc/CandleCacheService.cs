using System;
using System.Collections.Generic;
using System.Text;

namespace Smc
{
    public class CandleCacheService
    {
        private readonly Dictionary<string, List<Candle>> _cache = new();

        private static string GetKey(string ticker, string timeframe)
            => $"{ticker.ToUpper()}_{timeframe.ToLower()}";

        public bool HasCache(string ticker, string timeframe)
        {
            if( _cache.ContainsKey(GetKey(ticker, timeframe)))
            {
                //if (_cache[GetKey(ticker, timeframe)].Any(t=>t.Time.Year == year && t.Time.Month == month && t.Time.Day == day))
                //    return true;
                //else
                //    return false;
                return true ;
            }
            else return false;
        }

        public List<Candle> Get(string ticker, string timeframe)
        {
            var key = GetKey(ticker, timeframe);

            return _cache.TryGetValue(key, out var candles)
                ? candles
                : new List<Candle>();
        }

        public void Set(
            string ticker,
            string timeframe,
            List<Candle> candles,
            int maxCandles)
        {
            var key = GetKey(ticker, timeframe);

            _cache[key] = candles
                .OrderBy(x => x.Time)
                .TakeLast(maxCandles)
                .ToList();
        }

        public void Merge(
            string ticker,
            string timeframe,
            List<Candle> newCandles,
            int maxCandles)
        {
            var key = GetKey(ticker, timeframe);

            if (!_cache.ContainsKey(key))
            {
                Set(ticker, timeframe, newCandles, maxCandles);
                return;
            }

            var existing = _cache[key];

            foreach (var candle in newCandles)
            {
                var index = existing.FindIndex(x => x.Time == candle.Time);

                if (index >= 0)
                    existing[index] = candle;
                else
                    existing.Add(candle);
            }

            _cache[key] = existing
                .OrderBy(x => x.Time)
                .TakeLast(maxCandles)
                .ToList();
        }

        public DateTime? GetLastCandleTime(string ticker, string timeframe)
        {
            var candles = Get(ticker, timeframe);

            if (candles.Count == 0)
                return null;

            return candles.Max(x => x.Time);
        }
    }
}
