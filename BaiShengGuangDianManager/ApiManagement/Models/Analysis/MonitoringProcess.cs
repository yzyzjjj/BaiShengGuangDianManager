using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcess
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public int State { get; set; }
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

    }
}
