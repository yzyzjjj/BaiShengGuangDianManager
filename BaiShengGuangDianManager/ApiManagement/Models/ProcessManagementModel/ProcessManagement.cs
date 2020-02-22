using ApiManagement.Models.BaseModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.ProcessManagementModel
{
    public class ProcessManagement : CommonBase
    {
        /// <summary>
        /// 工艺编号
        /// </summary>
        public string ProcessNumber { get; set; } = string.Empty;
        /// <summary>
        /// 适用设备型号（自增Id, 英文逗号隔开）
        /// </summary>
        public string DeviceModels { get; set; }
        [JsonIgnore]
        public IEnumerable<int> DeviceModelList => DeviceModels.Split(",").Select(int.Parse);
        /// <summary>
        /// 适用产品型号（计划号）（自增Id, 英文逗号隔开）
        /// </summary>
        public string ProductModels { get; set; }

        [JsonIgnore]
        public IEnumerable<int> ProductModelList => ProductModels.Split(",").Select(int.Parse);

        /// <summary>
        /// 适用机台号（自增Id, 英文逗号隔开）
        /// </summary>
        public string DeviceIds { get; set; }
        [JsonIgnore]
        public IEnumerable<int> DeviceIdList => DeviceIds.Split(",").Select(int.Parse);
        public List<ProcessData> ProcessDatas = new List<ProcessData>();
    }

    public class ProcessManagementDetail : ProcessManagement
    {
        /// <summary>
        /// 适用设备型号 英文逗号隔开
        /// </summary>
        public string ModelName { get; set; } = string.Empty;
        /// <summary>
        /// 适用产品型号 英文逗号隔开
        /// </summary>
        public string ProductionProcessName { get; set; } = string.Empty;
        /// <summary>
        /// 适用机台号 英文逗号隔开
        /// </summary>
        public string Code { get; set; } = string.Empty;
        public int DeviceModelId { get; set; }
        public string CategoryName { get; set; }
    }
}
