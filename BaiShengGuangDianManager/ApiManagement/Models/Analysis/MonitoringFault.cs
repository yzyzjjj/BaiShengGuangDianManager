using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringFault
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// 车间
        /// </summary>
        public string Workshop { get; set; }
        /// <summary>
        /// 故障设备数量
        /// </summary>
        public int FaultDevice { get; set; }
        /// <summary>
        /// 上报故障类型数量
        /// </summary>
        public int ReportFaultType { get; set; }
        /// <summary>
        /// 上报故障总次数
        /// </summary>
        public int ReportCount { get; set; }

        /// <summary>
        /// 单个故障上报次数
        /// </summary>
        [JsonIgnore]
        private List<SingleFaultType> _reportSingleFaultType;

        public List<SingleFaultType> ReportSingleFaultType => _reportSingleFaultType ?? (_reportSingleFaultType = !ReportSingleFaultTypeStr.IsNullOrEmpty()
                                                                  ? JsonConvert.DeserializeObject<List<SingleFaultType>>(ReportSingleFaultTypeStr)
                                                                  : new List<SingleFaultType>());

        [JsonIgnore]
        public string ReportSingleFaultTypeStr { get; set; }

        /// <summary>
        /// 上报故障 故障率
        /// </summary>
        public decimal ReportFaultRate { get; set; }

        /// <summary>
        /// 已确认故障
        /// </summary>
        public int Confirmed { get; set; }

        /// <summary>
        /// 维修中故障
        /// </summary>
        public int Repairing { get; set; }

        /// <summary>
        /// 已维修故障(经上报)
        /// </summary>
        public int ReportRepaired { get; set; }

        /// <summary>
        /// 额外维修故障(直接添加维修记录)
        /// </summary>
        public int ExtraRepaired => RepairCount - ReportRepaired;

        /// <summary>
        /// 维修故障类型数量
        /// </summary>
        public int RepairFaultType { get; set; }
        /// <summary>
        /// 维修故障总次数
        /// </summary>
        public int RepairCount { get; set; }
        /// <summary>
        /// 单个故障维修次数
        /// </summary>
        [JsonIgnore]
        private List<SingleFaultType> _repairSingleFaultType;

        public List<SingleFaultType> RepairSingleFaultType => _repairSingleFaultType ?? (_repairSingleFaultType = !RepairSingleFaultTypeStr.IsNullOrEmpty()
                                                                  ? JsonConvert.DeserializeObject<List<SingleFaultType>>(RepairSingleFaultTypeStr)
                                                                  : new List<SingleFaultType>());

        [JsonIgnore]
        public string RepairSingleFaultTypeStr { get; set; }

    }

    /// <summary>
    /// 单个故障次数
    /// </summary>
    public class SingleFaultType
    {
        public int FaultId { get; set; }
        public string FaultName { get; set; }
        public int Count { get; set; }
        public List<DeviceFaultType> DeviceFaultTypes = new List<DeviceFaultType>();
        public List<Operator> Operators = new List<Operator>();
    }

    /// <summary>
    /// 设备故障详情
    /// </summary>
    public class DeviceFaultType
    {
        public string Code { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 员工上报/维修工详情
    /// </summary>
    public class Operator
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public int Time { get; set; }
    }
}
