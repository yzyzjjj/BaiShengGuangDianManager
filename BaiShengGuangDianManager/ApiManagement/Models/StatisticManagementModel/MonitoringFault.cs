using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;
using Newtonsoft.Json;using ModelBase.Models.BaseModel;
using ServiceStack;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringFault : ICloneable
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
        /// 设备总数
        /// </summary>
        public int AllDevice { get; set; }
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

        [JsonIgnore]
        public List<SingleFaultType> _reportSingleFaultType;

        /// <summary>
        /// 单个故障上报次数
        /// </summary>
        public List<SingleFaultType> ReportSingleFaultType
        {
            get => _reportSingleFaultType ?? (_reportSingleFaultType = !ReportSingleFaultTypeStr.IsNullOrEmpty()
                       ? JsonConvert.DeserializeObject<List<SingleFaultType>>(ReportSingleFaultTypeStr)
                       : new List<SingleFaultType>());
            set
            {
                _reportSingleFaultType = value;
                ReportSingleFaultTypeStr = _reportSingleFaultType.ToJSON();
            }
        }

        [JsonIgnore]
        public string ReportSingleFaultTypeStr { get; set; }

        /// <summary>
        /// 上报故障 故障率
        /// </summary>
        public decimal ReportFaultRate => AllDevice != 0 ? (FaultDevice * 1.0m / AllDevice).ToRound(4) : 0;

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
        public int ExtraRepaired { get; set; }

        /// <summary>
        /// 维修故障类型数量
        /// </summary>
        public int RepairFaultType { get; set; }
        /// <summary>
        /// 维修故障总次数
        /// </summary>
        public int RepairCount { get; set; }
        [JsonIgnore]
        public List<SingleFaultType> _repairSingleFaultType;

        /// <summary>
        /// 单个故障维修次数
        /// </summary>
        public List<SingleFaultType> RepairSingleFaultType
        {
            get => _repairSingleFaultType ?? (_repairSingleFaultType = !RepairSingleFaultTypeStr.IsNullOrEmpty()
                       ? JsonConvert.DeserializeObject<List<SingleFaultType>>(RepairSingleFaultTypeStr)
                       : new List<SingleFaultType>());
            set
            {
                _repairSingleFaultType = value;
                RepairSingleFaultTypeStr = _repairSingleFaultType.ToJSON();
            }
        }

        [JsonIgnore]
        public string RepairSingleFaultTypeStr { get; set; }

        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 机台号上报故障类型数量
        /// </summary>
        public int CodeReportFaultType { get; set; }

        /// <summary>
        /// 取消上报故障
        /// </summary>
        public int ReportCancel { get; set; }
        /// <summary>
        /// 取消维修记录
        /// </summary>
        public int RepairCancel { get; set; }

        public void Add(MonitoringFault monitoringFault)
        {
            AllDevice += monitoringFault.AllDevice;
            FaultDevice += monitoringFault.FaultDevice;
            ReportCount += monitoringFault.ReportCount;
            ReportCancel += monitoringFault.ReportCancel;
            foreach (var singleFaultType in monitoringFault.ReportSingleFaultType)
            {
                if (ReportSingleFaultType.Any(x => x.FaultId == singleFaultType.FaultId))
                {
                    var faultType = ReportSingleFaultType.First(x => x.FaultId == singleFaultType.FaultId);
                    faultType.Count += singleFaultType.Count;
                    foreach (var deviceFaultType in singleFaultType.DeviceFaultTypes)
                    {
                        if (faultType.DeviceFaultTypes.Any(x => x.Code == deviceFaultType.Code))
                        {
                            var first = faultType.DeviceFaultTypes.First(x => x.Code == deviceFaultType.Code);
                            first.Count += deviceFaultType.Count;
                        }
                        else
                        {
                            faultType.DeviceFaultTypes.Add(deviceFaultType);
                        }
                    }

                    foreach (var @operator in singleFaultType.Operators)
                    {
                        if (faultType.Operators.Any(x => x.Name == @operator.Name))
                        {
                            var operator1 = faultType.Operators.First(x => x.Name == @operator.Name);
                            operator1.Count += @operator.Count;
                            operator1.Time += @operator.Time;
                        }
                        else
                        {
                            faultType.Operators.Add(@operator);
                        }
                    }
                }
                else
                {
                    ReportSingleFaultType.Add(singleFaultType);
                }
            }
            ReportFaultType = ReportSingleFaultType.GroupBy(x => x.FaultId).Count();
            ReportSingleFaultTypeStr = ReportSingleFaultType.OrderBy(x => x.FaultId).ToJSON();

            Confirmed += monitoringFault.Confirmed;
            Repairing += monitoringFault.Repairing;
            RepairCount += monitoringFault.RepairCount;
            RepairCancel += monitoringFault.RepairCancel;

            foreach (var repairSingleFaultType in monitoringFault.RepairSingleFaultType)
            {
                if (RepairSingleFaultType.Any(x => x.FaultId == repairSingleFaultType.FaultId))
                {
                    var faultType = RepairSingleFaultType.First(x => x.FaultId == repairSingleFaultType.FaultId);
                    faultType.Count += repairSingleFaultType.Count;
                    foreach (var deviceFaultType in repairSingleFaultType.DeviceFaultTypes)
                    {
                        if (faultType.DeviceFaultTypes.Any(x => x.Code == deviceFaultType.Code))
                        {
                            var first = faultType.DeviceFaultTypes.First(x => x.Code == deviceFaultType.Code);
                            first.Count += deviceFaultType.Count;
                        }
                        else
                        {
                            faultType.DeviceFaultTypes.Add(deviceFaultType);
                        }
                    }
                    foreach (var @operator in repairSingleFaultType.Operators)
                    {
                        if (faultType.Operators.Any(x => x.Name == @operator.Name))
                        {
                            var operator1 = faultType.Operators.First(x => x.Name == @operator.Name);
                            operator1.Count += @operator.Count;
                            operator1.Time += @operator.Time;
                        }
                        else
                        {
                            faultType.Operators.Add(@operator);
                        }
                    }
                }
                else
                {
                    RepairSingleFaultType.Add(repairSingleFaultType);
                }
            }
            RepairFaultType = RepairSingleFaultType.GroupBy(x => x.FaultId).Count();
            RepairSingleFaultTypeStr = RepairSingleFaultType.OrderBy(x => x.FaultId).ToJSON();
        }

        public void DayAdd(MonitoringFault monitoringFault)
        {
            AllDevice = monitoringFault.AllDevice;
            ReportCount += monitoringFault.ReportCount;
            ReportCancel += monitoringFault.ReportCancel;
            foreach (var singleFaultType in monitoringFault.ReportSingleFaultType)
            {
                if (ReportSingleFaultType.Any(x => x.FaultId == singleFaultType.FaultId))
                {
                    var faultType = ReportSingleFaultType.First(x => x.FaultId == singleFaultType.FaultId);
                    faultType.Count += singleFaultType.Count;
                    foreach (var deviceFaultType in singleFaultType.DeviceFaultTypes)
                    {
                        if (faultType.DeviceFaultTypes.Any(x => x.Code == deviceFaultType.Code))
                        {
                            var first = faultType.DeviceFaultTypes.First(x => x.Code == deviceFaultType.Code);
                            first.Count += deviceFaultType.Count;
                        }
                        else
                        {
                            faultType.DeviceFaultTypes.Add(deviceFaultType);
                        }
                    }
                    foreach (var @operator in singleFaultType.Operators)
                    {
                        if (faultType.Operators.Any(x => x.Name == @operator.Name))
                        {
                            var operator1 = faultType.Operators.First(x => x.Name == @operator.Name);
                            operator1.Count += @operator.Count;
                            operator1.Time += @operator.Time;
                        }
                        else
                        {
                            faultType.Operators.Add(@operator);
                        }
                    }
                }
                else
                {
                    ReportSingleFaultType.Add(singleFaultType);
                }
            }
            ReportFaultType = ReportSingleFaultType.GroupBy(x => x.FaultId).Count();
            FaultDevice = ReportSingleFaultType.SelectMany(x => x.DeviceFaultTypes).GroupBy(y => y.Code).Count();
            ReportSingleFaultTypeStr = ReportSingleFaultType.OrderBy(x => x.FaultId).ToJSON();

            Confirmed += monitoringFault.Confirmed;
            Repairing += monitoringFault.Repairing;
            RepairCount += monitoringFault.RepairCount;
            RepairCancel += monitoringFault.RepairCancel;

            foreach (var repairSingleFaultType in monitoringFault.RepairSingleFaultType)
            {
                if (RepairSingleFaultType.Any(x => x.FaultId == repairSingleFaultType.FaultId))
                {
                    var faultType = RepairSingleFaultType.First(x => x.FaultId == repairSingleFaultType.FaultId);
                    faultType.Count += repairSingleFaultType.Count;
                    foreach (var deviceFaultType in repairSingleFaultType.DeviceFaultTypes)
                    {
                        if (faultType.DeviceFaultTypes.Any(x => x.Code == deviceFaultType.Code))
                        {
                            var first = faultType.DeviceFaultTypes.First(x => x.Code == deviceFaultType.Code);
                            first.Count += deviceFaultType.Count;
                        }
                        else
                        {
                            faultType.DeviceFaultTypes.Add(deviceFaultType);
                        }
                    }
                    foreach (var @operator in repairSingleFaultType.Operators)
                    {
                        if (faultType.Operators.Any(x => x.Name == @operator.Name))
                        {
                            var operator1 = faultType.Operators.First(x => x.Name == @operator.Name);
                            operator1.Count += @operator.Count;
                            operator1.Time += @operator.Time;
                        }
                        else
                        {
                            faultType.Operators.Add(@operator);
                        }
                    }
                }
                else
                {
                    RepairSingleFaultType.Add(repairSingleFaultType);
                }
            }
            RepairFaultType = RepairSingleFaultType.GroupBy(x => x.FaultId).Count();
            RepairSingleFaultTypeStr = RepairSingleFaultType.OrderBy(x => x.FaultId).ToJSON();

        }
        public object Clone()
        {
            return MemberwiseClone();
        }
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
