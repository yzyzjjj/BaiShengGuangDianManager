using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 工序生产数量
    /// </summary>
    public class SmartTaskOrderScheduleIndex : CommonBase
    {
        public int Batch { get; set; }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int DealId { get; set; }
        /// <summary>
        /// 产能指数
        /// </summary>
        public decimal Index { get; set; }

    }
    /// <summary>
    /// 工序生产数量
    /// </summary>
    public class SmartTaskOrderScheduleIndexDetail : CommonBase
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }
}
