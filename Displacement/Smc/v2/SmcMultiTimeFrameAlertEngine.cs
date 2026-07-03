using Displacement;

namespace Trading.Smc.v2;

// ================================================================
// MULTI-TIMEFRAME ALERT ENGINE
// HTF zone retracement + LTF CHOCH confirmation
// ================================================================

public sealed class SmcMultiTimeFrameAlertEngine
{
    private readonly SmcAnalyzer _analyzer;

    public SmcMultiTimeFrameAlertEngine(SmcAnalyzer? analyzer = null)
    {
        _analyzer = analyzer ?? new SmcAnalyzer();
    }

    public SmcAlertSignal CheckHtfZoneRetestWithLtfChoch(
        string ticker,
        List<Candle> higherTimeFrameCandles,
        List<Candle> lowerTimeFrameCandles,
        string higherTimeFrame,
        string lowerTimeFrame,
        int htfRange = 100,
        int ltfRange = 100,
        int htfSwingLength = 5,
        int ltfSwingLength = 3,
        int chochLookbackCandles = 10)
    {
        var htf = _analyzer.Analyze(higherTimeFrameCandles, htfRange, htfSwingLength, higherTimeFrame);
        var ltf = _analyzer.Analyze(lowerTimeFrameCandles, ltfRange, ltfSwingLength, lowerTimeFrame);

        if (higherTimeFrameCandles.Count == 0 || lowerTimeFrameCandles.Count == 0)
        {
            return NoSignal(ticker, higherTimeFrame, lowerTimeFrame, "Not enough candle data.");
        }

        var latestHtfCandle = higherTimeFrameCandles.OrderBy(x => x.Time).Last();
        var zonesNearPrice = htf.AllZones
            .Where(z => !z.IsBroken)
            .Where(z => z.ExpectedReactionDirection != Direction.None)
            .Where(z => SmcAnalyzer.IsCandleTouchingZone(latestHtfCandle, z))
            .OrderByDescending(z => z.Score)
            .ToList();

        foreach (var zone in zonesNearPrice)
        {
            Direction expected = zone.ExpectedReactionDirection;
            var choch = ltf.StructureBreaks
                .Where(x => x.IsChoch && x.Direction == expected)
                .Where(x => x.BrokenIndex >= Math.Max(0, lowerTimeFrameCandles.Count - chochLookbackCandles))
                .OrderByDescending(x => x.BrokenIndex)
                .FirstOrDefault();

            if (choch == null) continue;

            decimal score = Math.Min(100, zone.Score + 30 + (ltf.LatestDisplacement.IsDisplacement ? 10 : 0));

            return new SmcAlertSignal
            {
                ShouldAlert = true,
                Ticker = ticker,
                HigherTimeFrame = higherTimeFrame,
                LowerTimeFrame = lowerTimeFrame,
                HigherTimeFrameZone = zone,
                LowerTimeFrameChoch = choch,
                Direction = expected,
                Score = score,
                Message = $"{ticker} ALERT: {higherTimeFrame} price retraced into {zone.Type} " +
                          $"({zone.Low:N2} - {zone.High:N2}); {lowerTimeFrame} created {expected} CHOCH. Score: {score:N0}."
            };
        }

        return NoSignal(ticker, higherTimeFrame, lowerTimeFrame, "No HTF zone retest with LTF CHOCH confirmation.");
    }

    public async Task<SmcAlertSignal> CheckFromFmpAsync(
        FmpService fmp,
        string ticker,
        string higherTimeFrame,
        string lowerTimeFrame,
        int htfRange = 100,
        DateTime? lookBackFrom = null,
        CancellationToken cancellationToken = default)
    {
        var htfCandles = await fmp.GetCandlesAsync(ticker, higherTimeFrame, htfRange, lookBackFrom, cancellationToken);
        var ltfCandles = await fmp.GetCandlesAsync(ticker, lowerTimeFrame, htfRange, lookBackFrom, cancellationToken);

        return CheckHtfZoneRetestWithLtfChoch(
            ticker,
            htfCandles,
            ltfCandles,
            higherTimeFrame,
            lowerTimeFrame,
            htfRange: 100,
            ltfRange: 100,
            htfSwingLength: higherTimeFrame.Contains("4") ? 5 : 4,
            ltfSwingLength: lowerTimeFrame.Contains("5") ? 3 : 4,
            chochLookbackCandles: 10);
    }

    private static SmcAlertSignal NoSignal(string ticker, string htf, string ltf, string message) => new()
    {
        ShouldAlert = false,
        Ticker = ticker,
        HigherTimeFrame = htf,
        LowerTimeFrame = ltf,
        Direction = Direction.None,
        Message = message
    };

  

   
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
