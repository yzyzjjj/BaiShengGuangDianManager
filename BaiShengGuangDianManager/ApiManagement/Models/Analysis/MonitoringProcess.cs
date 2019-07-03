using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcess
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public int ProcessCount { get; set; }
        public int ProcessTime { get; set; }
        public int State { get; set; }
        /// <summary>
        /// 今日加工次数
        /// </summary>
        public int TodayProcessCount { get; set; }
        /// <summary>
        /// 总加工次数
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 今日加工时间(秒）
        /// </summary>
        public int TodayProcessTime { get; set; }
        /// <summary>
        /// 总加工时间(秒）
        /// </summary>
        public int TotalProcessTime { get; set; }

    }
}
