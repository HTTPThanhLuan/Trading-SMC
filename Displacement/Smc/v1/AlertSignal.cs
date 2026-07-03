namespace Trading.Smc.v1;

public sealed class AlertSignal
    {
        public bool ShouldAlert { get; set; }
        public Direction Direction { get; set; }
        public int HigherTimeframeZoneIndex { get; set; }
        public int LowerTimeframeChochIndex { get; set; }
        public DateTime Time { get; set; }
        public PriceZone Zone { get; set; } = default!;
        public StructureBreak Choch { get; set; } = default!;
        public string Message { get; set; } = string.Empty;
    }

