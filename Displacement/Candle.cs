using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Trading;

namespace Displacement
{

    public class FmpService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FmpService(string apiKey)
        {
            _apiKey = apiKey;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://financialmodelingprep.com/stable/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Example URL: https://financialmodelingprep.com/stable/historical-chart/1min?symbol=EURUSD&apikey=bNwpAsAvIjEKxid7uc5F78XBPmUuW8l2

        public async Task<List<Candle>> GetIntradayCandlesAsync(
            string ticker,
            string timeframe,
            string from,
            string to
            )
        {
            try
            {
                // timeframe examples: 1min, 5min, 15min, 30min, 1hour
                string url =
                    $"historical-chart/{timeframe}?symbol={ticker.ToUpper()}&from={from}&to={to}&apikey={_apiKey}";

                System.Diagnostics.Debug.WriteLine($"Fetching data from: {url}");

                var candles = await _httpClient
                    .GetFromJsonAsync<List<FmpCandleDto>>(url, JsonHandler.JsonOptions);                             

                System.Diagnostics.Debug.WriteLine($"Successfully fetched {candles?.Count ?? 0} candles for {ticker}");

                return candles?
                    .Select(x => new Candle
                    {
                        Date = x.Date,
                        Open = x.Open,
                        High = x.High,
                        Low = x.Low,
                        Close = x.Close,
                        Volume = x.Volume
                    })
                    .OrderBy(x => x.Date)
                    .ToList()
                    ?? new List<Candle>();
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Error for {ticker}: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Request timeout for {ticker}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching candles for {ticker}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Candle>> GetDailyCandlesAsync(string ticker)
        {
            string url =
                $"historical-price-full/{ticker.ToUpper()}?apikey={_apiKey}";

            var result = await _httpClient
                .GetFromJsonAsync<FmpDailyResponse>(url);

            return result?.Historical?
                .Select(x => new Candle
                {
                    Date = x.Date,
                    Open = x.Open,
                    High = x.High,
                    Low = x.Low,
                    Close = x.Close,
                    Volume = x.Volume
                })
                .OrderBy(x => x.Date)
                .ToList()
                ?? new List<Candle>();
        }

        
    }

    public class Candle
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }

        public decimal Body => Math.Abs(Close - Open);
        public decimal Range => High - Low;

        public bool IsBullish => Close > Open;

        public bool IsBearish => Close < Open;

        public decimal BodyPercent => Range == 0 ? 0 : Body / Range;

        public decimal UpperShadow =>
             High - Math.Max(Open, Close);

        public decimal LowerShadow =>
            Math.Min(Open, Close) - Low;

        public decimal UpperShadowPercent =>
            Range == 0 ? 0 : UpperShadow / Range;

        public decimal LowerShadowPercent =>
            Range == 0 ? 0 : LowerShadow / Range;


        public bool IsStrongBullish =>
                    IsBullish &&
                    BodyPercent >= 0.70m &&
                    UpperShadowPercent <= 0.10m;

        public bool IsStrongBearish =>
                IsBearish &&
                BodyPercent >= 0.70m &&
                LowerShadowPercent <= 0.10m;

        public bool IsSpinningTop =>
                BodyPercent >= 0.10m &&
                BodyPercent <= 0.30m &&
                UpperShadowPercent > 0.25m &&
                LowerShadowPercent > 0.25m;

        public bool IsDoji =>
                BodyPercent <= 0.05m;

    }
    public class FmpCandleDto
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("open")]
        public decimal Open { get; set; }

        [JsonPropertyName("high")]
        public decimal High { get; set; }

        [JsonPropertyName("low")]
        public decimal Low { get; set; }

        [JsonPropertyName("close")]
        public decimal Close { get; set; }

        [JsonPropertyName("volume")]
        public long Volume { get; set; }
    }

    public class FmpDailyResponse
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = "";

        [JsonPropertyName("historical")]
        public List<FmpCandleDto> Historical { get; set; } = new();
    }
}