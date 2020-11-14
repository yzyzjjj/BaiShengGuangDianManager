using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductCapacity : CommonBase
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
    }

    public class SmartProductCapacityDetail : SmartProductCapacity
    {
        /// <summary>
        /// 标准流程id
        /// </summary>
        public string Process { get; set; }
        /// <summary>
        /// 产能清单设置id
        /// </summary>
        public int ListId { get; set; }
        /// <summary>
        /// 设备类型id
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Category { get; set; }
    }
}
