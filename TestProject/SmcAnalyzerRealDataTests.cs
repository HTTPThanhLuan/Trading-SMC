namespace TestProject
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Smc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Integration tests for SmcAnalyzer using real candle data from FMPService.
    /// Requires FMP_API_KEY environment variable to be set.
    /// </summary>
    [TestClass]
    public class SmcAnalyzerRealDataTests
    {
        private FmpService? _fmpService;
        private string? _apiKey;
        private string _ticker = "CDNS"; 

        [TestInitialize]
        public void TestInitialize()
        {
            // Attempt to get API key from environment variable
            _apiKey = "bNwpAsAvIjEKxid7uc5F78XBPmUuW8l2";//  Environment.GetEnvironmentVariable("FMP_API_KEY");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Assert.Inconclusive("FMP_API_KEY environment variable not set. Skipping real data tests.");
            }

            _fmpService = new FmpService(_apiKey);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _fmpService?.Dispose();
        }

        [TestMethod]
        [Timeout(30000)] // 30 second timeout for API call
        public async Task DetectSwings()
        {
            // Arrange
            if (_fmpService == null)
            {
                Assert.Inconclusive("FmpService not initialized");
                return;
            }

            // Fetch real candle data for AAPL 1hour timeframe
            // Request 100 candles to have sufficient data for robust analysis
            var candles = await _fmpService.GetCandlesAsync(
                ticker: _ticker,
                timeframe: "4hour",
                start: new DateTime(2026, 03, 26), // last 10 days
                end: DateTime.Now
            );

            // Assert - validate data was retrieved
            Assert.IsNotNull(candles, "Candles list should not be null");
            Assert.IsTrue(candles.Count > 0, "Should retrieve at least one candle");
            Assert.IsTrue(candles.Count >= 50, $"Expected at least 50 candles, got {candles.Count}");

            // Verify candle data integrity
            foreach (var candle in candles)
            {
                Assert.IsTrue(candle.High >= candle.Low, "High should be >= Low for each candle");
                Assert.IsTrue(candle.High >= candle.Open && candle.High >= candle.Close, 
                    "High should be >= Open and Close");
                Assert.IsTrue(candle.Low <= candle.Open && candle.Low <= candle.Close, 
                    "Low should be <= Open and Close");
                Assert.IsTrue(candle.Volume >= 0, "Volume should be non-negative");
            }

            // Verify candles are ordered by time
            var sortedCandles = candles.OrderBy(c => c.Time).ToList();
            for (int i = 0; i < candles.Count; i++)
            {
                Assert.AreEqual(sortedCandles[i].Time, candles[i].Time, 
                    "Candles should be ordered by time");
            }

            // Act - Run SmcAnalyzer on real data
            var analyzer = new SmcAnalyzer();
            var result = analyzer.Analyze(
                candles,
                range: 130,
                swingLength: 5,
                sourceTimeFrame: "4hour"
            );

            // Assert - validate analysis results are populated
            Assert.IsNotNull(result, "Analysis result should not be null");
            Assert.IsNotNull(result.Swings, "Swings list should not be null");
            Assert.IsTrue(result.Swings.Count > 0, 
                "Should detect at least one swing in real market data");

            // Verify swings are ordered and alternating
            for (int i = 1; i < result.Swings.Count; i++)
            {
                Assert.IsTrue(result.Swings[i].Index > result.Swings[i - 1].Index,
                    "Swing indices should be in increasing order");

                // Swings should alternate between High and Low
                if (i > 0)
                {
                    Assert.AreNotEqual(result.Swings[i].Type, result.Swings[i - 1].Type,
                        "Consecutive swings should alternate between High and Low");
                }
            }

            // Trend may be Unknown, Bullish, Sideways, or Bearish - just verify it's set
            Assert.IsNotNull(result.Trend, "Trend should be detected");
            Assert.AreNotEqual(TrendType.Unknown, result.Trend, 
                "Should detect a valid trend in real market data");
        }
              

        [TestMethod]
        [Timeout(30000)]
        public async Task DetectTrend()
        {
            // Arrange
            if (_fmpService == null)
            {
                Assert.Inconclusive("FmpService not initialized");
                return;
            }

            // Fetch real candle data for AAPL 1hour timeframe
            // Request 100 candles to have sufficient data for robust analysis
            var candles = await _fmpService.GetCandlesAsync(
                ticker: _ticker,
                timeframe: "4hour",
                start: new DateTime(2026, 03, 26), // last 10 days
               // end: new DateTime(2026, 6, 26) Sideways
                end: DateTime.Now  // Bearish
               // end: new DateTime(2026, 6, 8) //Bullish
            );

            Assert.IsTrue(candles.Count >= 50, "Need sufficient candles for trend detection");

          
            // Act
            var analyzer = new SmcAnalyzer();
            var result = analyzer.Analyze(
                candles,
                range: 130,
                swingLength: 5,
                sourceTimeFrame: "4hour"
            );
            var trend = SmcAnalyzer.DetectTrend(result.Swings);

            // Assert
            Assert.IsNotNull(trend, "Trend should be detected");
            // Trend could be any type - just verify it's a valid value
            Assert.IsTrue(Enum.IsDefined(typeof(TrendType), trend),
                "Trend should be a valid TrendType value");
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task CalculateAtr()
        {
            // Arrange
            if (_fmpService == null)
            {
                Assert.Inconclusive("FmpService not initialized");
                return;
            }

            // Fetch real candle data for AAPL 1hour timeframe
            // Request 100 candles to have sufficient data for robust analysis
            var candles = await _fmpService.GetCandlesAsync(
                ticker: _ticker,
                timeframe: "4hour",
                start: new DateTime(2026, 03, 26), // last 10 days
                                                   // end: new DateTime(2026, 6, 26) Sideways
                                                    end: DateTime.Now  // Bearish
               // end: new DateTime(2026, 6, 8) //Bullish
            );

            Assert.IsTrue(candles.Count >= 50, "Need sufficient candles for trend detection");


            
            // Act
            var atrValues = SmcAnalyzer.CalculateAtr(candles, period: 14);

            // Assert
            Assert.IsNotNull(atrValues, "ATR values should not be null");
            Assert.AreEqual(candles.Count, atrValues.Count, 
                "ATR list should have same count as candles");

            // Verify all ATR values are non-negative
            foreach (var atr in atrValues)
            {
                Assert.IsTrue(atr >= 0, $"ATR value {atr} should be non-negative");
            }

            // Verify ATR doesn't grow unboundedly (sanity check)
            var maxAtr = atrValues.Max();
            var avgClose = candles.Average(c => c.Close);
            var maxAtrPercentage = (maxAtr / avgClose) * 100;
            Assert.IsTrue(maxAtrPercentage < 50, 
                $"ATR should be realistic (got {maxAtrPercentage:F1}% of avg close)");
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task IsCandleTouchingZone()
        {
            // Arrange
            if (_fmpService == null)
            {
                Assert.Inconclusive("FmpService not initialized");
                return;
            }

            var candles = await _fmpService.GetCandlesAsync(_ticker, "1hour", 50);
            Assert.IsTrue(candles.Count > 0, "Need at least one candle");

            // Create a zone based on recent price levels
            var recentCandles = candles.TakeLast(10).ToList();
            var zoneLow = recentCandles.Min(c => c.Low);
            var zoneHigh = recentCandles.Max(c => c.High);
            var midPoint = (zoneLow + zoneHigh) / 2;

            var testZone = new PriceZone
            {
                Low = zoneLow,
                High = zoneHigh,
                Type = ZoneType.Support
            };

            // Act - Test current candle
            var currentCandle = candles.Last();
            var touches = SmcAnalyzer.IsCandleTouchingZone(currentCandle, testZone);

            // Assert
            // Should touch if the candle's range overlaps with zone
            if (currentCandle.Low <= zoneHigh && currentCandle.High >= zoneLow)
            {
                Assert.IsTrue(touches, "Candle should touch zone when ranges overlap");
            }
            else
            {
                Assert.IsFalse(touches, "Candle should not touch zone when ranges don't overlap");
            }
        }
    }
}
