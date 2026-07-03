namespace Trading.Smc.v1;

public sealed class PriceZone
    {
        public ZoneType Type { get; set; }
        public Direction Direction { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
        public double Mid => (Top + Bottom) / 2.0;
        public bool IsMitigated { get; set; }
        public int? MitigatedIndex { get; set; }
        public double Strength { get; set; }
        public string Source { get; set; } = string.Empty;

        public bool Contains(double price, double tolerance = 0)
        {
            return price <= Top + tolerance && price >= Bottom - tolerance;
        }

        public bool Overlaps(double high, double low, double tolerance = 0)
        {
            return high >= Bottom - tolerance && low <= Top + tolerance;
        }
    }

