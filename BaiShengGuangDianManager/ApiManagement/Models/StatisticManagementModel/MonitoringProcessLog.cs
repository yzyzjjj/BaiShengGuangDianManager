using System;
using ApiManagement.Models.DeviceManagementModel;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringProcessLog
    {
        public int Id { get; set; }
        public ProcessType ProcessType { get; set; }
        public string OpName { get; set; }
        public int DeviceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalTime => StartTime != default(DateTime) && EndTime != default(DateTime) ? (int)(EndTime - StartTime).TotalSeconds : StartTime != default(DateTime) ? (int)(DateTime.Now - StartTime).TotalSeconds : 0;
        public int FlowCardId { get; set; }
        public string FlowCard { get; set; } = "";
        public int ProcessorId { get; set; }
        public string ProcessData { get; set; }
        public decimal RequirementMid { get; set; }
        public decimal ActualThickness { get; set; }
        /// <summary>
        /// 加工
        /// </summary>
        public int Error { get; set; }
    }

    public class MonitoringProcessLogDetail : MonitoringProcessLog
    {
        public string Code { get; set; }
        public string ProcessorName { get; set; }
        public string FlowCardName { get; set; }
        public int ProductionProcessId { get; set; }
        public string ProductionProcessName { get; set; }
    }
    public class MonitoringProcessLogFlag : MonitoringProcessLog
    {
        public bool Change { get; set; }
    }
}
