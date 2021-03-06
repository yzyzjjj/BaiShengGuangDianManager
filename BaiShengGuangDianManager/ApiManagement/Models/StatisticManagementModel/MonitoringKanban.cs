using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ModelBase.Models.Device;

namespace ApiManagement.Models.StatisticManagementModel
{
    public enum MonitoringKanBanEnum
    {
        [Description("无")]
        无 = 0,
        [Description("设备详情看板")]
        设备详情看板 = 1,
        [Description("设备状态看板")]
        设备状态看板 = 2
    }

    public class MonitoringKanBan
    {
        public MonitoringKanBan()
        {
            ProductionList = new List<MonitoringProductionData>();
            MSetData = new List<MonitoringSetData>();
        }
        //[JsonIgnore]
        //public bool Init = false;
        //[JsonIgnore]
        //public int InitCount = 0;
        //[JsonIgnore]
        public DateTime Date => Time.Date;
        public DateTime Time { get; set; }
        /// <summary>
        /// 看板id
        /// </summary>
        public int Id { get; set; } = 0;
        /// <summary>
        /// 总设备数量
        /// </summary>
        public int AllDevice { get; set; } = 0;
        /// <summary>
        /// 正常运行设备数量
        /// </summary>
        public int NormalDevice { get; set; } = 0;
        /// <summary>
        /// 加工中设备数量
        /// </summary>
        public int ProcessDevice { get; set; } = 0;
        /// <summary>
        /// 闲置设备数量
        /// </summary>
        public int IdleDevice => NormalDevice - ProcessDevice;
        /// <summary>
        /// 故障设备数量
        /// </summary>
        public int FaultDevice { get; set; } = 0;
        /// <summary>
        /// 连接异常设备数量
        /// </summary>
        public int ConnectErrorDevice => AllDevice - NormalDevice - FaultDevice;
        /// <summary>
        /// 日最大使用台数
        /// </summary>
        public int MaxUse { get; set; } = 0;
        /// <summary>
        /// 今日当前使用台数
        /// </summary>
        [JsonIgnore]
        private List<int> _useList;
        public List<int> UseList
        {
            get
            {
                if (_useList == null)
                {
                    _useList = !UseListStr.IsNullOrEmpty()
                        ? JsonConvert.DeserializeObject<List<int>>(UseListStr) : new List<int>();
                }

                var str = _useList.OrderBy(x => x).ToJson();
                if (UseListStr != str)
                {
                    UseListStr = str;
                }

                _useList = _useList.OrderBy(x => x).ToList();
                return _useList;
            }
            set
            {
                _useList = value;
                UseListStr = _useList.OrderBy(x => x).ToJson();
            }
        }
        [JsonIgnore]
        public string UseListStr { get; set; }
        /// <summary>
        /// 今日最大使用台数
        /// </summary>
        [JsonIgnore]
        private List<int> _maxUseList;
        public List<int> MaxUseList
        {
            get
            {
                if (_maxUseList == null)
                {
                    _maxUseList = !MaxUseListStr.IsNullOrEmpty()
                        ? JsonConvert.DeserializeObject<List<int>>(MaxUseListStr) : new List<int>();
                }

                var str = _maxUseList.OrderBy(x => x).ToJson();
                if (MaxUseListStr != str)
                {
                    MaxUseListStr = str;
                }

                _maxUseList = _maxUseList.OrderBy(x => x).ToList();
                return _maxUseList;
            }
            set
            {
                _maxUseList = value;
                MaxUseListStr = _maxUseList.OrderBy(x => x).ToJson();
            }
        }
        [JsonIgnore]
        public string MaxUseListStr { get; set; }
        public List<string> UseCodeList { get; set; }
        /// <summary>
        /// 日最小使用台数
        /// </summary>
        public int MinUse { get; set; } = -1;
        /// <summary>
        /// 日最大使用率
        /// </summary>
        public decimal MaxUseRate => AllDevice != 0 ? (MaxUse * 1m / AllDevice).ToRound(4) : 0;
        /// <summary>
        /// 日最小使用率
        /// </summary>
        public decimal MinUseRate => AllDevice != 0 ? ((MinUse == -1 ? 0 : MinUse) * 1m / AllDevice).ToRound(4) : 0;
        /// <summary>
        /// 最大同时使用台数日
        /// </summary>
        public decimal MaxSimultaneousUseRate { get; set; } = 0;
        /// <summary>
        /// 最小同时使用台数日
        /// </summary>
        public decimal MinSimultaneousUseRate { get; set; } = -1;
        /// <summary>
        /// 单台加工利用率=加工时间/24h
        /// </summary>
        public List<ProcessUseRate> SingleProcessRate { get; set; } = new List<ProcessUseRate>();
        [JsonIgnore]
        public string SingleProcessRateStr => SingleProcessRate.ToJson();
        /// <summary>
        /// 所有利用率=总加工时间/（机台号*24h）
        /// </summary>
        public decimal AllProcessRate { get; set; } = 0;
        /// <summary>
        /// 运行时间
        /// </summary>
        public int RunTime { get; set; } = 0;
        /// <summary>
        /// 加工时间
        /// </summary>
        public int ProcessTime { get; set; } = 0;
        /// <summary>
        /// 闲置时间 = 运行时间-加工时间
        /// </summary>
        public int IdleTime => RunTime - ProcessTime;
        /// <summary>
        /// 生产总数
        /// </summary>
        public int FaChu { get; set; }
        /// <summary>
        /// 合格
        /// </summary>
        public int HeGe { get; set; }
        /// <summary>
        /// 裂片
        /// </summary>
        public int LiePian { get; set; }
        /// <summary>
        /// 合格率
        /// </summary>
        public decimal Rate => FaChu == 0 ? 0 : ((decimal)HeGe / FaChu).ToRound();
        /// <summary>
        /// 生产数据
        /// </summary>
        public List<MonitoringProductionData> ProductionList { get; set; }
        public string ProductionData => ProductionList.ToJSON();
        /// <summary>
        /// 监控数据
        /// </summary>
        public List<MonitoringSetData> MSetData { get; set; }
        public string VariableData => MSetData.Select(x => new
        {
            x.Id,
            Data = x.Data.Select(y => (MonitoringSetSingleData)y)
        }).ToJSON();
        public void Update(MonitoringKanBan monitoringKanBan)
        {
            Time = monitoringKanBan.Time;
            AllDevice = monitoringKanBan.AllDevice;
            NormalDevice = monitoringKanBan.NormalDevice;
            ProcessDevice = monitoringKanBan.ProcessDevice;
            FaultDevice = monitoringKanBan.FaultDevice;
            if (MaxUse < monitoringKanBan.MaxUse)
            {
                MaxUse = monitoringKanBan.MaxUse;
            }
            UseList = monitoringKanBan.UseList;
            UseCodeList = monitoringKanBan.UseCodeList;
            MaxUseList = monitoringKanBan.MaxUseList;
            if (MinUse == 0)
            {
                MinUse = MaxUse;
            }
            if (MinUse > monitoringKanBan.MinUse)
            {
                MinUse = monitoringKanBan.MinUse;
            }
            if (MaxSimultaneousUseRate < monitoringKanBan.MaxSimultaneousUseRate)
            {
                MaxSimultaneousUseRate = monitoringKanBan.MaxSimultaneousUseRate;
            }
            if (MinSimultaneousUseRate == 0)
            {
                MinSimultaneousUseRate = MaxSimultaneousUseRate;
            }
            if (MinSimultaneousUseRate > monitoringKanBan.MinSimultaneousUseRate)
            {
                MinSimultaneousUseRate = monitoringKanBan.MinSimultaneousUseRate;
            }

            SingleProcessRate = monitoringKanBan.SingleProcessRate;
            AllProcessRate = monitoringKanBan.AllProcessRate;
            RunTime = monitoringKanBan.RunTime;
            ProcessTime = monitoringKanBan.ProcessTime;
        }
    }

    public class MonitoringKanBanDevice : MonitoringKanBan
    {
        public MonitoringKanBanDevice()
        {
            AllDevice = 1;
            AnalysisData = new DeviceData();
        }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public DeviceData AnalysisData { get; set; }
    }
    public class ProcessUseRate
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal Rate { get; set; }
    }
    public class MonitoringKanBanSet : CommonBase
    {
        /// <summary>
        /// 看板名
        /// </summary>
        public string Name { get; set; }
        public bool IsShow { get; set; }
        /// <summary>
        /// 0
        /// </summary>
        public MonitoringKanBanEnum Type { get; set; }
        public string DeviceIds { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 界面刷新时间(s)
        /// </summary>
        public int UI { get; set; }
        /// <summary>
        /// 数据刷新间隔(s)
        /// </summary>
        public int Second { get; set; }
        /// <summary>
        /// 单行显示数量
        /// </summary>
        public int Row { get; set; }
        /// <summary>
        /// 单列显示数量
        /// </summary>
        public int Col { get; set; }
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int Length => Row * Col;
        /// <summary>
        /// data_name_dictionary
        /// </summary>
        public string Variables { get; set; }

        public List<int> DeviceIdList
        {
            get
            {
                try
                {
                    if (!DeviceIds.IsNullOrEmpty())
                    {
                        return DeviceIds.Split(",").Select(int.Parse).ToList();
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return new List<int>();
            }
        }
        public List<DataNameDictionaryOrder> VariableList
        {
            get
            {
                var vl = new List<DataNameDictionaryOrder>();
                try
                {
                    if (!Variables.IsNullOrEmpty())
                    {
                        vl.AddRange(JsonConvert.DeserializeObject<IEnumerable<DataNameDictionaryOrder>>(Variables));
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                Variables = vl.Select(x => new { x.ScriptId, x.VariableTypeId, VariableName = x.VariableName ?? "", x.PointerAddress, x.Order }).ToJSON();
                return vl;
            }
        }
    }
    public class MonitoringProductionData
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public DateTime Time { get; set; }
        public int FaChu { get; set; }
        public int HeGe { get; set; }
        public int LiePian { get; set; }
        public decimal Rate { get; set; }
        public long ProcessTime { get; set; }
    }
    public class MonitoringSetData : DeviceLibraryDetail
    {
        public List<MonitoringSetSingleDataDetail> Data { get; set; } = new List<MonitoringSetSingleDataDetail>();
    }

    /// <summary>
    /// 
    /// </summary>
    public class MonitoringSetSingleData
    {
        /// <summary>
        /// 脚本Id
        /// </summary>
        public int Sid { get; set; }
        /// <summary>
        /// 变量类型
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 变量地址
        /// </summary>
        public int Add { get; set; }

        /// <summary>
        /// 变量值
        /// </summary>
        public string V { get; set; } = "";
    }
    public class MonitoringSetSingleDataDetail : MonitoringSetSingleData
    {
        /// <summary>
        /// 表data_name_dictionary ，id
        /// </summary>
        public string VName { get; set; }
    }
}
