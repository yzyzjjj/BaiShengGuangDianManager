using System;

namespace ApiManagement.Models.StatisticManagementModel
{

    /// <summary>
    /// 设备进度表
    /// </summary>
    public class DeviceSchedule
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
    /// <summary>
    /// 设备进度表
    /// </summary>
    public class KanBanDeviceProcess : DeviceSchedule
    {
        public DateTime Time { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
    }
}