namespace Trading.Smc.v1;

public sealed class RetracementEvent
    {
        public int Index { get; set; }
        public DateTime Time { get; set; }
        public PriceZone Zone { get; set; } = default!;
        public Direction Direction { get; set; }
        public double Price { get; set; }
        public string Message { get; set; } = string.Empty;
    }

