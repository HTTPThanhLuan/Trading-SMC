namespace Trading.Smc.v1;

public sealed class MarketAnalysis
    {
        public Direction Trend { get; set; }
        public List<SwingPoint> Swings { get; set; } = new();
        public List<PriceZone> Zones { get; set; } = new();
        public List<StructureBreak> StructureBreaks { get; set; } = new();
        public List<LiquidityEvent> Liquidity { get; set; } = new();
        public List<RetracementEvent> Retracements { get; set; } = new();
    }

