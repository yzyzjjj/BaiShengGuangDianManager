using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcessLog
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int FlowCardId { get; set; }
        public int ProcessorId { get; set; }
    }
}
