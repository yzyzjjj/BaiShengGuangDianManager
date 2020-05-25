using System;

namespace ApiManagement.Models.StatisticManagementModel
{

    public class RedisMonitoringProcess
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public int State { get; set; }
    }
    public class MonitoringProcess : ICloneable
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public int DeviceCategoryId { get; set; }
        public int State { get; set; }
        public int FlowCardId { get; set; }
        /// <summary>
        /// 今日加工次数
        /// </summary>
        public int ProcessCount { get; set; }
        /// <summary>
        /// 总加工次数
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 今日加工时间(秒）
        /// </summary>
        public int ProcessTime { get; set; }
        /// <summary>
        /// 总加工时间(秒）
        /// </summary>
        public int TotalProcessTime { get; set; }
        /// <summary>
        /// 今日运行时间(秒）
        /// </summary>
        public int RunTime { get; set; }
        /// <summary>
        /// 总运行时间(秒）
        /// </summary>
        public int TotalRunTime { get; set; }
        /// <summary>
        /// 使用台数
        /// </summary>
        public int Use { get; set; }
        /// <summary>
        /// 总台数
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 使用率%
        /// </summary>
        public decimal Rate { get; set; }
        /// <summary>
        /// 样本时间
        /// </summary>
        public DateTime SampleTime { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
