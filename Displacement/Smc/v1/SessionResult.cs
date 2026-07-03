using System;
using System.Collections.Generic;
using System.Linq;

namespace Trading.Smc.v1;

    // ============================================================
    // Sessions
    // ============================================================

    public sealed class SessionResult
    {
        public int Index { get; set; }
        public bool Active { get; set; }
        public double? High { get; set; }
        public double? Low { get; set; }
    }

   

