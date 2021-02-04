using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCode : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 流程编号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 流程编号类别
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 流程详情
        /// </summary>
        public string List { get; set; }
        [JsonIgnore]
        public List<int> ProcessIdList => List.IsNullOrEmpty() ? new List<int>() : List.Split(",").Select(int.Parse).ToList();
        [JsonIgnore]
        public List<string> ProcessList { get; set; } = new List<string>();
        public string Processes => ProcessList.Join();
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class SmartProcessCodeDetail : SmartProcessCode
    {
        /// <summary>
        /// 流程类别
        /// </summary>
        public string Category { get; set; }
    }
}
