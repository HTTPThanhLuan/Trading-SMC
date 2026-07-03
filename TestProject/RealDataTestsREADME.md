# SmcAnalyzer Real Data Tests - Setup Guide

## Overview

`SmcAnalyzerRealDataTests.cs` contains integration tests that validate `SmcAnalyzer` functionality using real market data fetched from the Financial Modeling Prep (FMP) API. These tests complement the synthetic data tests in `SmcAnalyzerTests.cs` by verifying that the analyzer works correctly with actual OHLCV candle data.

## Prerequisites

### FMP API Key

You must obtain a free or paid API key from [Financial Modeling Prep](https://financialmodelingprep.com/):

1. Visit https://financialmodelingprep.com/
2. Sign up for a free account
3. Navigate to your dashboard and copy your API key
4. Store this key securely

### Environment Variable Setup

The tests require the `FMP_API_KEY` environment variable to be set with your API key.

#### Windows (PowerShell)

```powershell
# Set for current session only
$env:FMP_API_KEY = "your_api_key_here"

# Verify it's set
$env:FMP_API_KEY
```

To set it permanently for all PowerShell sessions:

```powershell
[Environment]::SetEnvironmentVariable("FMP_API_KEY", "your_api_key_here", "User")
```

#### Windows (Command Prompt)

```cmd
# Set for current session only
set FMP_API_KEY=your_api_key_here

# Verify it's set
echo %FMP_API_KEY%
```

To set it permanently via System Properties:
1. Press `Win + R`, type `sysdm.cpl`, press Enter
2. Go to "Advanced" tab → "Environment Variables"
3. Under "User variables", click "New"
4. Variable name: `FMP_API_KEY`
5. Variable value: `your_api_key_here`
6. Click OK, then restart Visual Studio

#### Visual Studio Test Settings

Alternatively, you can set the environment variable directly in Visual Studio:

1. In Visual Studio, go to **Test** → **Test Settings** → **Default Processor Architecture** (or configure test run settings)
2. Create or edit a `.runsettings` file with:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
	<EnvironmentVariables>
	  <FMP_API_KEY>your_api_key_here</FMP_API_KEY>
	</EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

3. Load the `.runsettings` file via **Test** → **Configure Run Settings**

## Test Methods

### 1. `Analyze_WithRealAaplHourlyData_DetectsSwingsAndTrend`
- **Purpose**: Validates the main `SmcAnalyzer.Analyze()` method with real AAPL 1-hour candles
- **Data**: Fetches 100 candles for AAPL with 1-hour timeframe
- **Assertions**:
  - Candles are retrieved (≥50)
  - Candle OHLCV data is valid (High ≥ Low, etc.)
  - At least one swing is detected
  - Trend detection produces a valid result
  - Swings alternate between High and Low

### 2. `DetectSwings_WithRealAaplData_FindsMultipleSwings`
- **Purpose**: Tests swing detection on real market data
- **Data**: Fetches 80 AAPL 1-hour candles
- **Assertions**:
  - Multiple swings are detected
  - Swings alternate between High and Low
  - High swing levels match candle highs
  - Low swing levels match candle lows

### 3. `DetectTrend_WithRealAaplData_IdentifiesTrendDirection`
- **Purpose**: Validates trend identification (Bullish, Bearish, Sideways, or Unknown)
- **Data**: Fetches 100 AAPL 1-hour candles
- **Assertions**:
  - Trend detection completes successfully
  - Returned trend is a valid `TrendType` enum value

### 4. `CalculateAtr_WithRealAaplData_ProducesValidValues`
- **Purpose**: Validates ATR (Average True Range) calculation
- **Data**: Fetches 100 AAPL 1-hour candles
- **Assertions**:
  - ATR values are returned for all candles
  - All ATR values are non-negative
  - ATR doesn't exceed 50% of average candle close (sanity check)

### 5. `IsCandleTouchingZone_WithRealAaplData_IdentifiesZones`
- **Purpose**: Validates zone-touching logic with real price zones
- **Data**: Fetches 50 AAPL 1-hour candles
- **Assertions**:
  - Zone touching is correctly identified based on candle range overlap
  - High and low zones are properly validated

## Running the Tests

### In Visual Studio Test Explorer

1. Open **Test Explorer** (View → Test Explorer)
2. Search for "SmcAnalyzerRealDataTests"
3. Click "Run All" or right-click specific tests to run

### Via Command Line

```powershell
# Ensure FMP_API_KEY is set
$env:FMP_API_KEY = "your_api_key_here"

# Run all real data tests
dotnet test --filter "ClassName=TestProject.SmcAnalyzerRealDataTests"

# Run a specific test
dotnet test --filter "Name=Analyze_WithRealAaplHourlyData_DetectsSwingsAndTrend"

# Run with verbose output
dotnet test --filter "ClassName=TestProject.SmcAnalyzerRealDataTests" --verbosity detailed
```

## Expected Behavior

### When API Key is Set

- Tests execute successfully
- Real market data is fetched from FMP API
- Results are reported as Passed/Failed based on assertions
- Network latency: 1-30 seconds per test depending on API response time

### When API Key is NOT Set

- All tests are **Skipped** with the message:
  ```
  Assert.Inconclusive failed. FMP_API_KEY environment variable not set. Skipping real data tests.
  ```
- This is **expected behavior** and does NOT indicate a test failure

## Performance Considerations

- Each test fetches 50-100 candles from FMP API (network request)
- Tests have a 30-second timeout to account for API latency
- FMP caches results internally to avoid repeated fetches
- Free tier may have rate limits; tests respect this via caching

## Troubleshooting

### Tests Still Skip After Setting Environment Variable

1. **Verify the environment variable is set**:
   ```powershell
   $env:FMP_API_KEY  # Should print your API key
   ```

2. **Restart Visual Studio** after setting the environment variable system-wide

3. **Check variable name is exact**: `FMP_API_KEY` (case-sensitive in some cases)

### Network/API Errors

- **Connection timeout**: Increase timeout in test method or check internet connection
- **Invalid API key**: Verify the key is correct and active at https://financialmodelingprep.com/
- **Rate limit exceeded**: FMP free tier may limit requests. Wait or upgrade to premium

### Candle Data Issues

- **Expected at least 50 candles, got fewer**: FMP may not have 100 candles for the lookback period. Reduce `candleCount` parameter in the test.
- **High < Low or other data integrity errors**: Rare, but report to FMP support if consistent

## Integration with CI/CD

For automated test pipelines, set the environment variable before running tests:

```yaml
# GitHub Actions example
- name: Run SmcAnalyzer Real Data Tests
  env:
	FMP_API_KEY: ${{ secrets.FMP_API_KEY }}
  run: dotnet test --filter "ClassName=TestProject.SmcAnalyzerRealDataTests"
```

## Security Notes

- **Never commit API keys** to version control
- **Use environment variables** (as implemented) or secrets management
- **Rotate keys periodically** for security best practices
- **Monitor API usage** at your FMP dashboard to detect unauthorized access

## Related Files

- `SmcAnalyzerTests.cs` - Synthetic candle tests (no external dependencies)
- `Smc/SmcAnalyzer.cs` - Production analyzer code
- `Smc/FmpService.cs` - FMP API service implementation
- `Smc/Candle.cs` - Candle model definition

## References

- [Financial Modeling Prep API Docs](https://financialmodelingprep.com/api/v3/historical-chart/1hour?symbol=AAPL&apikey=YOUR_KEY)
- [SmcAnalyzer Documentation](#) (internal)
- [ATR Calculation](https://www.investopedia.com/terms/a/atr.asp)
