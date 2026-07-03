namespace Trading.Smc.v1;

public sealed class SwingPoint
    {
        public int Index { get; set; }
        public DateTime Time { get; set; }
        public Direction Direction { get; set; } // Bullish = swing high, Bearish = swing low
        public double Level { get; set; }
    }

