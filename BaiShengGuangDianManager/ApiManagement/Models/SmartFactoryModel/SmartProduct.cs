using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProduct : CommonBase
    {
        /// <summary>
        /// 计划号
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
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
        /// 流程编号清单
        /// </summary>
        public string ProcessCodeIds { get; set; }
        [JsonIgnore]
        public List<int> ProcessCodeIdsList => ProcessCodeIds.IsNullOrEmpty() ? new List<int>() : ProcessCodeIds.Split(",").Select(int.Parse).ToList();
        [JsonIgnore]
        public List<string> ProcessCodesList { get; set; } = new List<string>();
        public string ProcessCodes => ProcessCodesList.Join();
        public List<SmartProductProcessDetail> ProductProcesses { get; set; } = new List<SmartProductProcessDetail>();
    }

    public class SmartProductProcessDetail : SmartProductProcess
    {
        /// <summary>
        /// 流程
        /// </summary>
        public string Process { get; set; }
    }

}
