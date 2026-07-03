using System;
using System.Collections.Generic;
using System.Text;

namespace Displacement
{
    public class ActiveAlert
    {
        public string Ticker { get; set; } = "";
        public string Direction { get; set; } = "";
        public string TimeFrame { get; set; } = "";
        public DateTime AlertTime { get; set; }
    }
}
