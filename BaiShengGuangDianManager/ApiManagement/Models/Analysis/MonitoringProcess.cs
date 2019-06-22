using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcess
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public int ProcessCount { get; set; }
        public int LastState { get; set; }
        public int TodayProcessCount { get; set; }
        public int TotalProcessCount { get; set; }
    }
}
