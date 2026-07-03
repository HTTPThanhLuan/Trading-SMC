namespace Smc;

public sealed class SmcAnalysisResult
{
    public TrendType Trend { get; set; }
    public List<SwingPoint> Swings { get; set; } = new();
    public List<PriceZone> SupportResistanceZones { get; set; } = new();
    public List<PriceZone> FvgZones { get; set; } = new();
    public List<PriceZone> OrderBlocks { get; set; } = new();
    public List<PriceZone> SupplyDemandZones { get; set; } = new();
    public List<PriceZone> LiquidityZones { get; set; } = new();
    public List<StructureBreak> StructureBreaks { get; set; } = new();
    public DisplacementResult LatestDisplacement { get; set; } = new();
    public List<decimal> Atr { get; set; } = new();

    public IEnumerable<PriceZone> AllZones => SupportResistanceZones
        .Concat(FvgZones)
        .Concat(OrderBlocks)
        .Concat(SupplyDemandZones)
        .Concat(LiquidityZones);
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
