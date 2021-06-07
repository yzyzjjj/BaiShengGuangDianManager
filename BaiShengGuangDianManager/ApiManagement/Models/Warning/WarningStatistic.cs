using System;

namespace ApiManagement.Models.Warning
{
    public enum WarningStatisticTime
    {
        分 = 0,
        时 = 1,
        天 = 2,
    }
    public class WarningStatistic
    {
        //private DateTime warningTime;
        //private int v;

        public WarningStatistic()
        {
        }

        public WarningStatistic(int workshopId, WarningStatisticTime type, DateTime time, int setId, string setName, int itemId, string item, string range, int count)
        {
            WorkshopId = workshopId;
            Type = type;
            Time = time;
            SetId = setId;
            SetName = setName;
            ItemId = itemId;
            Item = item;
            Range = range;
            Count = count;
        }
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 时间类型
        /// </summary>
        public WarningStatisticTime Type { get; set; }
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

        public bool HaveChange(WarningStatistic statistic)
        {
            return Count != statistic.Count;
        }
    }
}