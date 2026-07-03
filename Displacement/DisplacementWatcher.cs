using System;
using System.Collections.Generic;
using System.Text;

namespace Displacement
{
    public class DisplacementWatcher
    {
        private AlertService _alert;
        private string _ticker;
        private string _timeFrame;
        private int _startIndex = 0;
        public DisplacementWatcher(AlertService _alert, string ticker, string timeFrame)
        {
            this._alert = _alert;
            this._ticker = ticker;
            this._timeFrame = timeFrame;
        }

        public void NotifyDisplacement(string message)
        {
            _alert.StartAlarm();
        }

       

        //Convert Local time to EST
       



        public DisplacementResult Detect(List<Candle> candles, int index, int atrPeriod = 14, decimal atrMultiplier = 2m)
        {
            var one = DetectSingle(candles, index, atrPeriod, atrMultiplier);

            if (one.IsDisplacement)
                return one;

            var two = DetectMultiCandleDisplacement(candles, index, 2, atrPeriod, atrMultiplier);

            if (two.IsDisplacement)
                return two;

            var three = DetectMultiCandleDisplacement(candles, index, 3, atrPeriod, atrMultiplier);

            if (three.IsDisplacement)
                return three;

            return new DisplacementResult();
        }

        public DisplacementResult DetectSingle(
            List<Candle> candles,
            int index,
            int atrPeriod = 14,
            decimal atrMultiplier = 2m
            )
        {
            var c = candles[index];

            var range = c.High - c.Low;
            if (range <= 0) return new();

            var body = Math.Abs(c.Close - c.Open);
            var bodyPercent = body / range;

            var atr = candles
                .Skip(index - atrPeriod + 1)
                .Take(atrPeriod)
                .Average(x => (double)(x.High - x.Low));

            var avgVolume = candles
                .Skip(index - atrPeriod + 1)
                .Take(atrPeriod)
                .Average(x => x.Volume);

            var atrRatio = range / (decimal)atr;
            var volumeRatio = avgVolume == 0 ? 0 : (double)c.Volume / avgVolume;

            bool bullish =
                c.Close > c.Open &&
                bodyPercent >= 0.80m &&
                atrRatio >= atrMultiplier && //1.2m &&
              //  volumeRatio >= 1.2d &&
                c.Close >= c.Low + range * 0.75m;

            bool bearish =
                c.Close < c.Open &&
                bodyPercent >=  0.80m &&
                atrRatio >= atrMultiplier && // 1.2m &&
               // volumeRatio >= 1.2d &&
                c.Close <= c.Low + range * 0.25m;

            return new DisplacementResult
            {
                IsDisplacement =  bullish || bearish,
                Direction = bullish ? $"Bullish - One Candle - Low: {c.Low} High: {c.High} Open: {c.Open} Close: {c.Close}" : bearish ? $"Bearish - One Candle - High: {c.High} Low: {c.Low} Open: {c.Open} Close: {c.Close}" : "",
                BodyPercent = bodyPercent,
                AtrRatio = atrRatio,
                VolumeRatio = volumeRatio,
                Ticker = _ticker,
                TimeFrame = _timeFrame,
                DateTime = c.Date.ConvertEstToLocalTime()
            };
        }


      
        public DisplacementResult DetectMultiCandleDisplacement(
            List<Candle> candles,
            int index,
            int candleCount = 2,
            int atrPeriod = 14,
            decimal atrMultiplier = 2m)
        {
            if (index < candleCount - 1 || index < atrPeriod)
                return new DisplacementResult();

            var sequence = candles
                .Skip(index - candleCount + 1)
                .Take(candleCount)
                .ToList();

            var lastCandle = sequence.Last();

            bool allBullish = sequence.All(c => c.Close > c.Open);
            bool allBearish = sequence.All(c => c.Close < c.Open);

            if (!allBullish && !allBearish)
                return new DisplacementResult();

            string direction = allBullish ? $"Bullish - Candle: {candleCount}  -  Low: {lastCandle.Low} High: {lastCandle.High} Open: {lastCandle.Open} Close: {lastCandle.Close}" : $"Bearish - Candle: {candleCount}  -  Low: {lastCandle.Low} High: {lastCandle.High} Open: {lastCandle.Open} Close: {lastCandle.Close}";

            decimal totalMove = allBullish
                ? sequence.Last().Close - sequence.First().Open
                : sequence.First().Open - sequence.Last().Close;

            decimal avgAtr = candles
                .Skip(index - atrPeriod + 1)
                .Take(atrPeriod)
                .Average(c => c.High - c.Low);

            //double avgVolume = candles
            //    .Skip(index - volumePeriod + 1)
            //    .Take(volumePeriod)
            //    .Average(c => (double)c.Volume);

            bool eachCandleStrong = sequence.All(c =>
            {
                decimal range = c.High - c.Low;
                if (range <= 0) return false;

                decimal body = Math.Abs(c.Close - c.Open);
                decimal bodyPercent = body / range;

                bool closeNearHigh = c.Close >= c.Low + range * 0.75m;
                bool closeNearLow = c.Close <= c.Low + range * 0.25m;

                return bodyPercent >= 0.55m &&
                       ((direction == "Bullish" && closeNearHigh) ||
                        (direction == "Bearish" && closeNearLow));
            });

            bool totalMoveStrong = totalMove >= avgAtr * 1.2m;

            //bool volumeStrong = sequence.Any(c => c.Volume >= avgVolume * 1.2m);

            bool noHeavyOverlap = true;

            for (int i = 1; i < sequence.Count; i++)
            {
                if (direction == "Bullish")
                {
                    if (sequence[i].Close <= sequence[i - 1].Open)
                        noHeavyOverlap = false;
                }
                else
                {
                    if (sequence[i].Close >= sequence[i - 1].Open)
                        noHeavyOverlap = false;
                }
            }

            bool isDisplacement =
                eachCandleStrong &&
                totalMoveStrong &&
               // volumeStrong &&
                noHeavyOverlap;

            return new DisplacementResult
            {
                //IsDisplacement = isDisplacement,
                //Direction = isDisplacement ? direction : "",
                //BodyPercent = sequence.Average(c =>
                //{
                //    var range = c.High - c.Low;
                //    return range <= 0 ? 0 : Math.Abs(c.Close - c.Open) / range;
                //}),
                //AtrRatio = avgAtr == 0 ? 0 : totalMove / avgAtr,
                // VolumeRatio = avgVolume == 0 ? 0 : sequence.Max(c => c.Volume) / avgVolume

                IsDisplacement = isDisplacement,
                Direction = isDisplacement ? direction : "",
                BodyPercent = sequence.Average(c =>
                {
                    var range = c.High - c.Low;
                    return range <= 0 ? 0 : Math.Abs(c.Close - c.Open) / range;
                }),
                AtrRatio = avgAtr == 0 ? 0 : totalMove / avgAtr,
                //VolumeRatio = volumeRatio,
                Ticker = _ticker,
                TimeFrame = _timeFrame,
                DateTime = sequence.Last().Date.ConvertEstToLocalTime()
            };
        }
    }

    public class DisplacementResult
    {
        public bool IsDisplacement { get; set; }
        public string Direction { get; set; } = "";
        public decimal BodyPercent { get; set; }
        public decimal AtrRatio { get; set; }
        public double VolumeRatio { get; set; }
        public string Ticker { get; set; } = "";
        public string TimeFrame { get; set; } = "";
        public DateTime DateTime { get; set; }
    }
}
