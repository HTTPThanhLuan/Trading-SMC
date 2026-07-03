namespace Trading.Smc.v1;

// ============================================================
// Previous high/low by timeframe
// ============================================================

public sealed class PreviousHighLowResult
    {
        public int Index { get; set; }
        public double? PreviousHigh { get; set; }
        public double? PreviousLow { get; set; }
        public bool BrokenHigh { get; set; }
        public bool BrokenLow { get; set; }
    }

   
