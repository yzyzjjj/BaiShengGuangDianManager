using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Warning
{
    public class WarningClear : CommonBase
    {
        /// <summary>
        /// 解决日期
        /// </summary>
        public DateTime DealTime { get; set; }
        /// <summary>
        /// 预警设置id
        /// </summary>
        public int SetId { get; set; }
        /// <summary>
        /// 设备列表
        /// </summary>
        public string DeviceIds { get; set; } = string.Empty;
        public List<int> DeviceIdList => DeviceIds.IsNullOrEmpty() ? new List<int>() : DeviceIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();
        /// <summary>
        /// 系统处理日期
        /// </summary>
        public DateTime OpTime { get; set; }
        /// <summary>
        /// 是否处理
        /// </summary>
        /// <returns></returns>
        public bool IsDeal { get; set; }
    }
    public class WarningClearDetail : WarningClear
    {
        public WarningClearDetail()
        {
            DeviceList = new List<string>();
        }
        /// <summary>
        /// 解决人
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 预警名
        /// </summary>
        public string SetName { get; set; }
        public List<string> DeviceList { get; set; }
    }
}