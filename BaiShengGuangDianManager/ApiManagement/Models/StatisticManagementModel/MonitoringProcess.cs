using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.Warning;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Device;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringProcessAnalysis
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }

        #region 数据分析
        /// <summary>
        /// 设备状态
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 加工类型
        /// </summary>
        public ProcessType ProcessType { get; set; }
        /// <summary>
        /// 加工日志Id  npc_monitoring_process_log
        /// </summary>
        public int LogId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public string FlowCard { get; set; } = "";
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public int LastFlowCardId { get; set; }
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public string LastFlowCard { get; set; } = "";

        /// <summary>
        /// 日总加工次数
        /// </summary>
        public int ProcessCount { get; set; }
        /// <summary>
        /// 设备自首次启用以来总加工次数
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 日总加工时间(秒）
        /// </summary>
        public int ProcessTime { get; set; }
        /// <summary>
        /// 设备自首次启用以来总加工时间(秒）
        /// </summary>
        public int TotalProcessTime { get; set; }
        /// <summary>
        /// 日总运行时间(秒）
        /// </summary>
        public int RunTime { get; set; }
        /// <summary>
        /// 设备自首次启用以来总运行时间(秒）
        /// </summary>
        public int TotalRunTime { get; set; }

        /// <summary>
        /// 日总闲置时间 = 日总运行时间-日总加工时间
        /// </summary>
        public int IdleTime => RunTime - ProcessTime;
        /// <summary>
        /// 设备自首次启用以来总闲置时间 = 设备自首次启用以来总运行时间 - 设备自首次启用以来加工时间
        /// </summary>
        public int TotalIdleTime => TotalRunTime - TotalProcessTime;

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
        public int MaxUse { get; set; }
        public List<string> UseCodeList { get; set; } = new List<string>();
        /// <summary>
        /// 日最小使用台数
        /// </summary>
        public int MinUse { get; set; }
        /// <summary>
        /// 日最大使用率
        /// </summary>
        public decimal MaxSimultaneousUseRate { get; set; }
        /// <summary>
        /// 日最小使用率
        /// </summary>
        public decimal MinSimultaneousUseRate { get; set; }
        /// <summary>
        /// 所有利用率=总加工时间/（机台号*24h）
        /// </summary>
        public decimal AllProcessRate { get; set; }

        /// <summary>
        /// 生产数据
        /// </summary>
        public List<MonitoringProductionData> ProductionList { get; set; } = new List<MonitoringProductionData>();

        /// <summary>
        /// 使用台数
        /// </summary>
        public int Use { get; set; }

        /// <summary>
        /// 总设备数量
        /// </summary>
        public int Total => AllDevice;
        /// <summary>
        /// 使用率%
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// 样本时间
        /// </summary>
        public DateTime SampleTime { get; set; }
        /// <summary>
        /// 今日当前使用台数
        /// </summary>
        public List<int> UseList { get; set; } = new List<int>();
        /// <summary>
        /// 今日最大使用台数
        /// </summary>
        public List<int> MaxUseList { get; set; } = new List<int>();
        /// <summary>
        /// 单台加工利用率=加工时间/24h
        /// </summary>
        public List<ProcessUseRate> SingleProcessRate { get; set; } = new List<ProcessUseRate>();

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 其他数据
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// 单台日加工数
        /// </summary>
        public int DayTotal { get; set; }
        /// <summary>
        /// 单台日合格数
        /// </summary>
        public int DayQualified { get; set; }
        /// <summary>
        /// 单台日次品数
        /// </summary>
        public int DayUnqualified { get; set; }
        /// <summary>
        /// 加工日志Id时间  npc_monitoring_process_log
        /// </summary>
        public DateTime LogTime { get; set; }
        /// <summary>
        /// 是否有插入加工日志  npc_monitoring_process_log
        /// </summary>
        public bool NewLog => LogId == 0;
        /// <summary>
        /// 设备数据
        /// </summary>
        public DeviceData AnalysisData { get; set; }
        #endregion
    }

    public class ExtraData
    {
        /// <summary>
        /// 待机开始时间
        /// </summary>
        public DateTime IdleTime { get; set; }
        /// <summary>
        /// 待机结束时间
        /// </summary>
        public DateTime IdleEndTime { get; set; }
        /// <summary>
        /// 加工开始时间
        /// </summary>
        public DateTime ProcessTime { get; set; }
        /// <summary>
        /// 加工结束时间
        /// </summary>
        public DateTime ProcessEndTime { get; set; }
        /// <summary>
        /// 洗盘开始时间
        /// </summary>
        public DateTime WashTime { get; set; }
        /// <summary>
        /// 洗盘结束时间
        /// </summary>
        public DateTime WashEndTime { get; set; }
        /// <summary>
        /// 修盘开始时间
        /// </summary>
        public DateTime RepairTime { get; set; }
        /// <summary>
        /// 修盘结束时间
        /// </summary>
        public DateTime RepairEndTime { get; set; }

        public List<MonitoringProcessPart> Parts { get; set; } = new List<MonitoringProcessPart>();

        public void Reset()
        {
            //IdleTime = default(DateTime);
            //IdleEndTime = default(DateTime);
            //ProcessTime = default(DateTime);
            //ProcessEndTime = default(DateTime);
            //WashTime = default(DateTime);
            //WashEndTime = default(DateTime);
            //RepairTime = default(DateTime);
            //RepairEndTime = default(DateTime);
            Parts = new List<MonitoringProcessPart>();
        }
    }

    public class MonitoringProcessWarning
    {
        public DateTime Time { get; set; }
        public bool Warning { get; set; }
        /// <summary>
        /// 预警时间
        /// </summary>
        public DateTime WarningTime { get; set; }
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备数据预警列表ItemId
        /// </summary>
        public string DeviceWarnings { get; set; }
        /// <summary>
        /// 生产数据预警列表ItemId
        /// </summary>
        public string ProductWarnings { get; set; }
    }

    public class MonitoringProcessWarningLog
    {
        public int LogId { get; set; }
        public DateTime WarningTime { get; set; }
        public int ItemId { get; set; }
        /// <summary>
        /// 预警项名称
        /// </summary>
        /// <returns></returns>
        public string Item { get; set; } = string.Empty;
        /// <summary>
        /// 预警项类型
        /// </summary>
        /// <returns></returns>
        public WarningItemType ItemType { get; set; }
        /// <summary>
        /// 预警名id
        /// </summary>
        public int SetId { get; set; }
        /// <summary>
        /// 预警名
        /// </summary>
        public string SetName { get; set; }
        /// <summary>
        /// 预警范围
        /// </summary>
        public string Range { get; set; }
        public decimal Value { get; set; }
    }

    /// <summary>
    /// 分班次统计数据
    /// </summary>
    public class MonitoringProcessPart : MonitoringProcess
    {
        /// <summary>
        /// 班次 0为全部 1 为第一个班
        /// </summary>
        public int Shift { get; set; }
    }


    public class MonitoringProcess : ICloneable
    {
        public MonitoringProcess()
        {
            AllDevice = 1;
            AnalysisData = new DeviceData();
        }
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        public DateTime Time { get; set; }
        /// <summary>
        /// 加工类型
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 机台号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 脚本id
        /// </summary>
        public int ScriptId { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>
        /// 设备类型名
        /// </summary>
        public string CategoryName { get; set; }

        #region 数据分析
        /// <summary>
        /// 设备状态
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 加工类型
        /// </summary>
        public ProcessType ProcessType { get; set; }
        /// <summary>
        /// 加工日志Id  npc_monitoring_process_log
        /// </summary>
        public int LogId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public int FlowCardId { get; set; }
        /// <summary>
        /// 当前流程卡
        /// </summary>
        public string FlowCard { get; set; } = "";
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public int LastFlowCardId { get; set; }
        /// <summary>
        /// 上次加工流程卡
        /// </summary>
        public string LastFlowCard { get; set; } = "";

        /// <summary>
        /// 日总加工次数
        /// </summary>
        public int ProcessCount { get; set; }
        /// <summary>
        /// 设备自首次启用以来总加工次数
        /// </summary>
        public int TotalProcessCount { get; set; }
        /// <summary>
        /// 日总加工时间(秒）
        /// </summary>
        public int ProcessTime { get; set; }
        /// <summary>
        /// 设备自首次启用以来总加工时间(秒）
        /// </summary>
        public int TotalProcessTime { get; set; }
        /// <summary>
        /// 日总运行时间(秒）
        /// </summary>
        public int RunTime { get; set; }
        /// <summary>
        /// 设备自首次启用以来总运行时间(秒）
        /// </summary>
        public int TotalRunTime { get; set; }

        /// <summary>
        /// 日总闲置时间 = 日总运行时间-日总加工时间
        /// </summary>
        public int IdleTime => RunTime - ProcessTime;
        /// <summary>
        /// 设备自首次启用以来总闲置时间 = 设备自首次启用以来总运行时间 - 设备自首次启用以来加工时间
        /// </summary>
        public int TotalIdleTime => TotalRunTime - TotalProcessTime;

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
        public List<string> UseCodeList { get; set; } = new List<string>();
        /// <summary>
        /// 日最小使用台数
        /// </summary>
        public int MinUse { get; set; } = -1;
        /// <summary>
        /// 日最大使用率
        /// </summary>
        public decimal MaxSimultaneousUseRate { get; set; } = 0;
        /// <summary>
        /// 日最小使用率
        /// </summary>
        public decimal MinSimultaneousUseRate { get; set; } = -1;
        /// <summary>
        /// 所有利用率=总加工时间/（机台号*24h）
        /// </summary>
        public decimal AllProcessRate { get; set; } = 0;

        /// <summary>
        /// 生产数据
        /// </summary>
        public List<MonitoringProductionData> ProductionList { get; set; } = new List<MonitoringProductionData>();

        /// <summary>
        /// 使用台数
        /// </summary>
        public int Use { get; set; }

        /// <summary>
        /// 总设备数量
        /// </summary>
        public int Total => AllDevice;
        /// <summary>
        /// 使用率%
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// 样本时间
        /// </summary>
        public DateTime SampleTime { get; set; }

        /// <summary>
        /// 今日当前使用台数
        /// </summary>
        [JsonIgnore]
        public string UseListStr { get; set; } = "[]";
        [JsonIgnore]
        private List<int> _useList;
        /// <summary>
        /// 今日当前使用台数
        /// </summary>
        public List<int> UseList
        {
            get
            {
                _useList = _useList ?? (!UseListStr.IsNullOrEmpty()
                    ? JsonConvert.DeserializeObject<List<int>>(UseListStr) : new List<int>());

                _useList = _useList.OrderBy(x => x).ToList();
                return _useList;
            }
            set => _useList = value;
        }

        /// <summary>
        /// 今日最大使用台数
        /// </summary>
        [JsonIgnore]
        public string MaxUseListStr { get; set; } = "[]";
        /// <summary>
        /// 今日最大使用台数
        /// </summary>
        [JsonIgnore]
        private List<int> _maxUseList;
        public List<int> MaxUseList
        {
            get
            {
                _maxUseList = _maxUseList ?? (!MaxUseListStr.IsNullOrEmpty()
                    ? JsonConvert.DeserializeObject<List<int>>(MaxUseListStr) : new List<int>());

                _maxUseList = _maxUseList.OrderBy(x => x).ToList();
                return _maxUseList;
            }
            set => _maxUseList = value;
        }

        /// <summary>
        /// 单台加工利用率=加工时间/24h
        /// </summary>
        public List<ProcessUseRate> SingleProcessRate { get; set; } = new List<ProcessUseRate>();
        public void UpdateAnalysis()
        {
            UseListStr = _useList.ToJson();
            MaxUseListStr = _maxUseList.ToJson();
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 本次过程已经历时间
        /// </summary>
        public int TotalTime => StartTime != default(DateTime) && EndTime != default(DateTime) ? (int)(EndTime - StartTime).TotalSeconds : StartTime != default(DateTime) ? (int)(DateTime.Now - StartTime).TotalSeconds : 0;
        /// <summary>
        /// 其他数据
        /// </summary>
        public string Data { get; set; }
        private ExtraData _extraData { get; set; }
        [JsonIgnore]
        public ExtraData ExtraData
        {
            get
            {
                try
                {
                    _extraData = _extraData ?? (Data.IsNullOrEmpty() ? new ExtraData() : Data.ToClass<ExtraData>());
                    _extraData = _extraData ?? new ExtraData();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return _extraData;
            }
            set
            {
                _extraData = value;
            }
        }

        public void SerializeExtraData()
        {
            Data = _extraData.ToJSON();
        }

        public void UpdateExtraData()
        {
            if (ProcessType == ProcessType.Idle)
            {
                ExtraData.IdleTime = StartTime;
                ExtraData.IdleEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Wash)
            {
                ExtraData.WashTime = StartTime;
                ExtraData.WashEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Repair)
            {
                ExtraData.RepairTime = StartTime;
                ExtraData.RepairEndTime = EndTime;
            }
            else if (ProcessType == ProcessType.Process)
            {
                ExtraData.ProcessTime = StartTime;
                ExtraData.ProcessEndTime = EndTime;
            }
            SerializeExtraData();
        }

        /// <summary>
        /// 单台日加工数
        /// </summary>
        public int DayTotal { get; set; }
        /// <summary>
        /// 单台日合格数
        /// </summary>
        public int DayQualified { get; set; }
        /// <summary>
        /// 单台日次品数
        /// </summary>
        public int DayUnqualified { get; set; }
        /// <summary>
        /// 单台日合格率(%)
        /// </summary>
        public decimal DayQualifiedRate => DayTotal == 0 ? 0 : ((decimal)DayQualified * 100 / DayTotal).ToRound();
        /// <summary>
        /// 单台日次品率(%)
        /// </summary>
        public decimal DayUnqualifiedRate => DayTotal == 0 ? 0 : ((decimal)DayUnqualified * 100 / DayTotal).ToRound();

        /// <summary>
        /// 加工日志Id时间  npc_monitoring_process_log
        /// </summary>
        public DateTime LogTime { get; set; }
        /// <summary>
        /// 是否有插入加工日志  npc_monitoring_process_log
        /// </summary>
        public bool NewLog => LogId == 0;

        public DeviceData AnalysisData { get; set; }


        #endregion

        #region 预警数据
        public bool Warning => DeviceWarning || ProductWarning;
        public bool DeviceWarning => DeviceWarningList.Any();
        public bool ProductWarning => ProductWarningList.Any();
        /// <summary>
        /// 预警时间
        /// </summary>
        public DateTime WarningTime
        {
            get
            {
                var t1 = DeviceWarning
                    ? DeviceWarningList.Values.Max(x => x.WarningTime)
                    : default(DateTime);
                var t2 = ProductWarning
                    ? ProductWarningList.Values.Max(x => x.WarningTime)
                    : default(DateTime);
                return Warning
                    ? (t1.Max(t2))
                    : default(DateTime);
            }
        }

        /// <summary>
        /// 设备数据预警列表ItemId
        /// </summary>
        public string DeviceWarnings { get; set; }
        /// <summary>
        /// 生产数据预警列表ItemId
        /// </summary>
        public string ProductWarnings { get; set; }

        /// <summary>
        ///  ItemId, LogId, Value (预警值)
        /// </summary>
        private Dictionary<int, MonitoringProcessWarningLog> _deviceWarningList { get; set; }
        /// <summary>
        ///  ItemId, LogId, Value (预警值)
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, MonitoringProcessWarningLog> DeviceWarningList
        {
            get
            {
                try
                {
                    _deviceWarningList = _deviceWarningList ?? (DeviceWarnings.IsNullOrEmpty()
                                             ? new Dictionary<int, MonitoringProcessWarningLog>()
                                             : DeviceWarnings.ToClass<Dictionary<int, MonitoringProcessWarningLog>>());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return _deviceWarningList;
            }
            set => _deviceWarningList = value;
        }
        /// <summary>
        ///  ItemId, LogId, Value (预警值)
        /// </summary>
        private Dictionary<int, MonitoringProcessWarningLog> _productWarningList { get; set; }
        /// <summary>
        ///  ItemId, LogId, Value (预警值)
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, MonitoringProcessWarningLog> ProductWarningList
        {
            get
            {
                try
                {
                    _productWarningList = _productWarningList ?? (ProductWarnings.IsNullOrEmpty()
                                             ? new Dictionary<int, MonitoringProcessWarningLog>()
                                             : ProductWarnings.ToClass<Dictionary<int, MonitoringProcessWarningLog>>());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return _productWarningList;
            }
            set => _productWarningList = value;
        }

        public void UpdateWarningData(WarningDataType dataType = WarningDataType.默认)
        {
            if (dataType == WarningDataType.设备数据)
            {
                DeviceWarnings = _deviceWarningList.ToJSON();
            }
            else if (dataType == WarningDataType.生产数据)
            {
                ProductWarnings = _productWarningList.ToJSON();
            }
            else if (dataType == WarningDataType.默认)
            {
                DeviceWarnings = _deviceWarningList.ToJSON();
                ProductWarnings = _productWarningList.ToJSON();
            }
        }
        public void UpdateWarningData(WarningDataType dataType, WarningLog log, bool add = true)
        {
            WarningChange = true;
            var key = log.ItemId;
            if (add)
            {
                var wl = ClassExtension.CopyTo<WarningLog, MonitoringProcessWarningLog>(log);
                //var wl = new MonitoringProcessWarningLog
                //{
                //    LogId = log.Id,
                //    WarningTime = log.WarningTime,
                //    ItemId = log.ItemId,
                //    Item = log.Item,
                //    ItemType = log.ItemType,
                //    SetId = log.SetId,
                //    SetName = log.SetName,
                //    Range = log.Range,
                //    Value = log.Value,
                //};
                wl.Range = log.Range;
                if (dataType == WarningDataType.设备数据)
                {
                    if (DeviceWarningList.ContainsKey(key))
                    {
                        DeviceWarningList[key] = wl;
                    }
                    else
                    {
                        DeviceWarningList.Add(key, wl);
                    }
                }
                else if (dataType == WarningDataType.生产数据)
                {
                    if (ProductWarningList.ContainsKey(key))
                    {
                        ProductWarningList[key] = wl;
                    }
                    else
                    {
                        ProductWarningList.Add(key, wl);
                    }
                }
                UpdateWarningData(dataType);
            }
            else
            {
                if (dataType == WarningDataType.设备数据)
                {
                    if (DeviceWarningList.ContainsKey(key))
                    {
                        DeviceWarningList.Remove(key);
                        UpdateWarningData(dataType);
                    }
                }
                else if (dataType == WarningDataType.生产数据)
                {
                    if (ProductWarningList.ContainsKey(key))
                    {
                        ProductWarningList.Remove(key);
                        UpdateWarningData(dataType);
                    }
                }
            }
        }

        public void RemoveWarningData(WarningDataType dataType, int sId)
        {
            WarningChange = true;
            var keys = DeviceWarningList.Where(x => x.Value.SetId == sId).Select(x => x.Key);
            DeviceWarningList = DeviceWarningList.Where(x => !keys.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            UpdateWarningData(dataType);
        }
        #endregion

        public object Clone()
        {
            return MemberwiseClone();
        }
        public bool WarningChange { get; set; }
        /// <summary>
        /// 每日重置
        /// </summary>
        public void DayRest(bool init = true)
        {
            NormalDevice = 0;
            ProcessDevice = 0;
            FaultDevice = 0;
            if (init)
            {
                DayTotal = 0;
                DayQualified = 0;
                DayUnqualified = 0;
                ProcessCount = 0;
                ProcessTime = 0;
                RunTime = 0;
                MaxUse = 0;
                MinUse = -1;
                MaxSimultaneousUseRate = 0;
                MinSimultaneousUseRate = -1;
                AllProcessRate = 0;
                UseList = new List<int>();
                MaxUseList = new List<int>();
                SingleProcessRate = new List<ProcessUseRate>();
                UseCodeList = new List<string>();
                ProductionList = new List<MonitoringProductionData>();
                UpdateAnalysis();
                ExtraData.Reset();
            }
        }

        public void Check()
        {
            if (TotalProcessCount < 0)
            {
                TotalProcessCount = 0;
            }
            if (TotalProcessTime < 0)
            {
                TotalProcessTime = 0;
            }
            if (TotalRunTime < 0)
            {
                TotalRunTime = 0;
            }
            if (UseList == null)
            {
                UseList = new List<int>();
            }

            if (MaxUseList == null)
            {
                MaxUseList = new List<int>();
            }

            if (SingleProcessRate == null)
            {
                SingleProcessRate = new List<ProcessUseRate>();
            }

            if (UseCodeList == null)
            {
                UseCodeList = new List<string>();
            }

            if (ProductionList == null)
            {
                ProductionList = new List<MonitoringProductionData>();
            }
        }
    }

}
