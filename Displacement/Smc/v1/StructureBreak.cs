namespace Trading.Smc.v1;

public sealed class StructureBreak
    {
        public BreakType Type { get; set; }
        public Direction Direction { get; set; }
        public int SignalIndex { get; set; }
        public int BrokenIndex { get; set; }
        public DateTime Time { get; set; }
        public double Level { get; set; }
        public string Description { get; set; } = string.Empty;
    }

