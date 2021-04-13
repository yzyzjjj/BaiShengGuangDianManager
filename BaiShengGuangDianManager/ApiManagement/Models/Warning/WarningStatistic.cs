using System;

namespace ApiManagement.Models.Warning
{
    public class WarningStatistic
    {
        //private DateTime warningTime;
        //private int v;

        public WarningStatistic()
        {
        }

        public WarningStatistic(DateTime time, int setId, string setName, int itemId, string item, string range, int count)
        {
            Time = time;
            SetId = setId;
            SetName = setName;
            ItemId = itemId;
            Item = item;
            Range = range;
            Count = count;
        }
        public DateTime Time { get; set; }
        /// <summary>
        /// 预警设置id
        /// </summary>
        /// <returns></returns>
        public int SetId { get; set; }
        /// <summary>
        /// 预警设置名称
        /// </summary>
        public string SetName { get; set; } = string.Empty;
        /// <summary>
        /// 预警设置项id
        /// </summary>
        public int ItemId { get; set; }
        /// <summary>
        /// 预警项名称
        /// </summary>
        /// <returns></returns>
        public string Item { get; set; } = string.Empty;
        /// <summary>
        /// 预警范围
        /// </summary>
        /// <returns></returns>
        public string Range { get; set; } = string.Empty;
        /// <summary>
        /// 预警次数
        /// </summary>
        public int Count { get; set; }
    }

    public enum WarningStatisticTime
    {
        分,
        时,
        天
    }
}