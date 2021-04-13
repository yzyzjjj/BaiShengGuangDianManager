using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProduct : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 计划号
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 流程编号类型id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 产能id
        /// </summary>
        public int CapacityId { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 日最大产能 该日产能为末道工序最大产能
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 设备日产能
        /// </summary>
        public int DeviceCapacity { get; set; }
        /// <summary>
        /// 人员日产能
        /// </summary>
        public int OperatorCapacity { get; set; }
    }

    public class SmartProcessCraft
    {
        /// <summary>
        /// 工艺  加压时间（M） 加压时间（S） 工序时间（M） 工序时间（S） 设定压力（Kg） 下盘速度（rpm）
        /// </summary>
        public decimal[] Craft { get; set; } = new decimal[6];
        public decimal TotalSecond => Craft[0] * 60 + Craft[1] + Craft[2] * 60 + Craft[3];
    }

    public class SmartProductDetail : SmartProduct
    {
        /// <summary>
        /// 流程编号类型
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 产能
        /// </summary>
        public string Capacity { get; set; }
        ///// <summary>
        ///// 日最大产能 该日产能为末道工序最大产能
        ///// </summary>
        //public int Number { get; set; }
        /// <summary>
        /// 流程编号清单
        /// </summary>
        public string ProcessCodeIds { get; set; }
        [JsonIgnore]
        public List<int> ProcessCodeIdsList => ProcessCodeIds.IsNullOrEmpty() ? new List<int>() : ProcessCodeIds.Split(",").Select(int.Parse).ToList();
        [JsonIgnore]
        public List<string> ProcessCodesList { get; set; } = new List<string>();
        public string ProcessCodes => ProcessCodesList.Join();
        public List<SmartProductProcessDetail> Processes { get; set; } = new List<SmartProductProcessDetail>();
        public List<SmartProductCapacityDetail> Capacities { get; set; } = new List<SmartProductCapacityDetail>();

    }

}
