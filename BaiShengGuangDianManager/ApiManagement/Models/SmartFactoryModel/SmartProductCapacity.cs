using ApiManagement.Models.BaseModel;
using System;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductCapacity : CommonBase, ICloneable
    {
        /// <summary>
        /// 计划号id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate { get; set; }
        /// <summary>
        /// 工时
        /// </summary>
        public int Day { get; set; }
        /// <summary>
        /// 工时
        /// </summary>
        public int Hour { get; set; }
        /// <summary>
        /// 工时
        /// </summary>
        public int Min { get; set; }
        /// <summary>
        /// 工时
        /// </summary>
        public int Sec { get; set; }

        /// <summary>
        /// 日最大产能 该日产能为末道工序最大产能
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 设备日产能
        /// </summary>
        public int DeviceNumber { get; set; }
        /// <summary>
        /// 人员日产能
        /// </summary>
        public int OperatorNumber { get; set; }

        public SmartProductCapacityError Error { get; set; } = SmartProductCapacityError.正常;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class SmartProductCapacityDetail : SmartProductCapacity
    {
        /// <summary>
        /// 产能id
        /// </summary>
        public int CapacityId { get; set; }
        /// <summary>
        /// 标准流程id
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 产能清单设置id
        /// </summary>
        public int ListId { get; set; }
        /// <summary>
        /// 流程id
        /// </summary>
        public int PId { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; }
    }

    public class SmartProductCapacityLast : SmartCapacityList
    {
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate { get; set; }
    }
}
