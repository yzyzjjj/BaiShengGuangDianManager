using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcessLog
    {
        public int Id { get; set; }
        public string OpName { get; set; }
        public int DeviceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int FlowCardId { get; set; }
        public int ProcessorId { get; set; }
        public string ProcessData { get; set; }
        public decimal RequirementMid { get; set; }
        public decimal ActualThickness { get; set; }
    }

    public class MonitoringProcessLogDetail : MonitoringProcessLog
    {
        public int TotalTime => StartTime != default(DateTime) && EndTime != default(DateTime) ? (int)(EndTime - StartTime).TotalSeconds : StartTime != default(DateTime) ? (int)(DateTime.Now - StartTime).TotalSeconds : 0;
        public string Code { get; set; }
        public string ProcessorName { get; set; }
        public string FlowCardName { get; set; }
        public string ProductionProcessName { get; set; }
    }
}
