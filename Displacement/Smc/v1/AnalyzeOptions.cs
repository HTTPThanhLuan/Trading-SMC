using System;
using System.Collections.Generic;
using System.Text;

namespace Trading.Smc.v1
{
    public sealed class AnalyzeOptions
    {
        public int SwingLength { get; set; } = 5;
        public int SupportResistanceLookback { get; set; } = 100;
        public double ZoneMergePercent { get; set; } = 0.003; // 0.3%
        public double LiquidityRangePercent { get; set; } = 0.01;
        public bool CloseBreak { get; set; } = true;
        public bool CloseMitigation { get; set; } = false;
        public int RecentBarsForRetracement { get; set; } = 3;
    }
}
