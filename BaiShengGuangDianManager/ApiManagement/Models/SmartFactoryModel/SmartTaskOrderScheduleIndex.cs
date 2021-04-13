using ModelBase.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    /// <summary>
    /// 工序生产数量
    /// </summary>
    public class SmartTaskOrderScheduleIndex : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        public int Batch { get; set; }
        /// <summary>
        /// 0 设备 1 人员
        /// </summary>
        public int ProductType { get; set; } = -1;
        /// <summary>
        /// 加工时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
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
    public class SmartTaskOrderScheduleIndexDetail : SmartTaskOrderScheduleIndex
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 设备
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 人员
        /// </summary>
        public string Name { get; set; }
    }
}
