using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacity : CommonBase
    {
        /// <summary>
        /// 产能
        /// </summary>
        public string Capacity { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 日最大产能 该日产能为末道工序最大产能
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 末道工序顺序
        /// </summary>
        public int Last { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartCapacityDetail : SmartCapacity
    {
        /// <summary>
        /// 流程编号类型
        /// </summary>
        public string Category { get; set; }
    }

    public class OpSmartCapacity : SmartCapacity
    {
        /// <summary>
        /// 流程编号类型
        /// </summary>
        public List<SmartCapacityList> List { get; set; } = new List<SmartCapacityList>();
    }

}
