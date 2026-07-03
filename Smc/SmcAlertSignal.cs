namespace Smc;

public sealed class SmcAlertSignal
{
    public bool ShouldAlert { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string HigherTimeFrame { get; set; } = string.Empty;
    public string LowerTimeFrame { get; set; } = string.Empty;
    public PriceZone? HigherTimeFrameZone { get; set; }
    public StructureBreak? LowerTimeFrameChoch { get; set; }
    public Direction Direction { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal Score { get; set; }

    public static SmcAlertSignal NoAlert(string ticker)
    {
        return new SmcAlertSignal
        {
            ShouldAlert = false,
            Ticker = ticker
        };
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
