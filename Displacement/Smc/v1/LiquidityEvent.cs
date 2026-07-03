namespace Trading.Smc.v1;

public sealed class LiquidityEvent
    {
        public Direction Direction { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int? SweptIndex { get; set; }
        public double Level { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

