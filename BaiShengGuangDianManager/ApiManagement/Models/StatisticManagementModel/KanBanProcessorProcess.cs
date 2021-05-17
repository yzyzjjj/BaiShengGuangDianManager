using System;

namespace ApiManagement.Models.StatisticManagementModel
{
    /// <summary>
    /// 操作工进度表
    /// </summary>
    public class ProcessorSchedule
    {
        public int ProcessorId { get; set; }
        public string Processor { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
    /// <summary>
    /// 操作工进度表
    /// </summary>
    public class KanBanProcessorProcess : ProductionSchedule
    {
        public DateTime Time { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
    }
}