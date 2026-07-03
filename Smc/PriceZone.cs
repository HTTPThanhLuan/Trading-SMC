namespace Smc;

public sealed class PriceZone
{
    public ZoneType Type { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int TouchCount { get; set; }
    public int? MitigatedIndex { get; set; }
    public int? SweptIndex { get; set; }
    public decimal Score { get; set; }
    public string SourceTimeFrame { get; set; } = string.Empty;
    public bool IsFresh => MitigatedIndex == null && SweptIndex == null;
    public bool IsBroken { get; set; }

    public Direction ExpectedReactionDirection => Type switch
    {
        ZoneType.Support => Direction.Bullish,
        ZoneType.Demand => Direction.Bullish,
        ZoneType.BullishFvg => Direction.Bullish,
        ZoneType.BullishOrderBlock => Direction.Bullish,
        ZoneType.BullishLiquidity => Direction.Bearish, // high liquidity often sweeps then rejects down

        ZoneType.Resistance => Direction.Bearish,
        ZoneType.Supply => Direction.Bearish,
        ZoneType.BearishFvg => Direction.Bearish,
        ZoneType.BearishOrderBlock => Direction.Bearish,
        ZoneType.BearishLiquidity => Direction.Bullish, // low liquidity often sweeps then rejects up
        _ => Direction.None
    };

    public override string ToString()
    {
        return Type switch
        {
            ZoneType.Demand => "Bullish",
            ZoneType.Support => "Bullish",
            ZoneType.BullishFvg => "Bullish",
            ZoneType.BullishOrderBlock => "Bullish",

            ZoneType.Supply => "Bearish",
            ZoneType.Resistance => "Bearish",
            ZoneType.BearishFvg => "Bearish",
            ZoneType.BearishOrderBlock => "Bearish",

            _ => ""
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
