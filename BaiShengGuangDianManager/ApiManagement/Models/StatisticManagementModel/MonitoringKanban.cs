using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.Warning;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public enum KanBanEnum
    {
        [Description("无")]
        无 = 0,
        [Description("设备详情看板")]
        设备详情看板 = 1,
        [Description("设备状态看板")]
        设备状态看板 = 2,
        [Description("生产相关看板")]
        生产相关看板 = 3
    }

    public enum KanBanItemEnum
    {
        [Description("无")]
        无 = 0,
        #region 生产相关看板
        [Description("合格率异常报警")]
        合格率异常报警 = 1,
        [Description("合格率异常统计")]
        合格率异常统计,
        [Description("设备状态反馈")]
        设备状态反馈,
        [Description("设备预警状态")]
        设备预警状态,
        [Description("计划号日进度表")]
        计划号日进度表,
        [Description("设备日进度表")]
        设备日进度表,
        [Description("操作工日进度表")]
        操作工日进度表,
        #endregion


        #region 设备状态看板 生产数据选项
        [Description("上次加工数")]
        上次加工数 = 100,
        [Description("上次合格数")]
        上次合格数,
        [Description("上次次品数")]
        上次次品数,
        [Description("上次合格率")]
        上次合格率,
        [Description("上次次品率")]
        上次次品率,

        [Description("今日加工数")]
        今日加工数,
        [Description("今日加工次数")]
        今日加工次数,
        [Description("今日合格数")]
        今日合格数,
        [Description("今日次品数")]
        今日次品数,
        [Description("今日合格率")]
        今日合格率,
        [Description("今日次品率")]
        今日次品率,
        [Description("今日合格率预警")]
        今日合格率预警,

        [Description("昨日加工数")]
        昨日加工数,
        [Description("昨日加工次数")]
        昨日加工次数,
        [Description("昨日合格数")]
        昨日合格数,
        [Description("昨日次品数")]
        昨日次品数,
        [Description("昨日合格率")]
        昨日合格率,
        [Description("昨日次品率")]
        昨日次品率,
        [Description("昨日合格率预警")]
        昨日合格率预警,
        #endregion

    }

    public class MonitoringKanBan : MonitoringProcess
    {
        public MonitoringKanBan()
        {
            ProductionList = new List<MonitoringProductionData>();
            MSetData = new List<MonitoringSetData>();
            Times = new Dictionary<KanBanItemEnum, DateTime>();
            WarningLogs = new List<WarningLog>();
            WarningStatistics = new List<WarningStatistic>();
            DeviceStateInfos = new List<DeviceStateInfo>();
            WarningDeviceInfos = new List<WarningDeviceInfo>();
            ProductionSchedules = new List<ProductionSchedule>();
            DeviceSchedules = new List<DeviceSchedule>();
            ProcessorSchedules = new List<ProcessorSchedule>();
        }
        //[JsonIgnore]
        //public bool Init = false;
        //[JsonIgnore]
        //public int InitCount = 0;
        //[JsonIgnore]
        public DateTime Date => Time.Date;
        /// <summary>
        /// 看板id
        /// </summary>
        public int Id { get; set; } = 0;
        public decimal MaxUseRate => AllDevice != 0 ? (MaxUse * 1m / AllDevice).ToRound(4) : 0;
        /// <summary>
        /// 日最小使用率
        /// </summary>
        public decimal MinUseRate => AllDevice != 0 ? ((MinUse == -1 ? 0 : MinUse) * 1m / AllDevice).ToRound(4) : 0;
        [JsonIgnore]
        public string SingleProcessRateStr => SingleProcessRate.ToJson();
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
        /// <summary>
        /// 报警数据
        /// </summary>
        [JsonIgnore]
        public Dictionary<KanBanItemEnum, DateTime> Times { get; set; }
        /// <summary>
        /// 报警数据
        /// </summary>
        public List<WarningLog> WarningLogs { get; set; }
        /// <summary>
        /// 报警统计数据
        /// </summary>
        public List<WarningStatistic> WarningStatistics { get; set; }
        /// <summary>
        /// 设备状态反馈
        /// </summary>
        public List<DeviceStateInfo> DeviceStateInfos { get; set; }
        /// <summary>
        /// 预警状态设备
        /// </summary>
        public List<WarningDeviceInfo> WarningDeviceInfos { get; set; }
        /// <summary>
        /// 计划号日进度表
        /// </summary>
        public List<ProductionSchedule> ProductionSchedules { get; set; }
        /// <summary>
        /// 设备日进度表
        /// </summary>
        public List<DeviceSchedule> DeviceSchedules { get; set; }
        /// <summary>
        /// 操作工日进度表
        /// </summary>
        public List<ProcessorSchedule> ProcessorSchedules { get; set; }
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
        public KanBanEnum Type { get; set; }
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
        /// 设备看板内容列数量
        /// </summary>
        public int ContentCol { get; set; }
        /// <summary>
        /// 设备看板内容列名
        /// </summary>
        public string ColName { get; set; }

        public List<string> ColNameList
        {
            get
            {
                try
                {
                    if (!ColName.IsNullOrEmpty())
                    {
                        return ColName.Split(",").ToList();
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return new List<string>();
            }
        }
        /// <summary>
        /// 设备看板内容配置
        /// </summary>
        public string ColSet { get; set; }
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int Length => Row * Col;
        /// <summary>
        /// data_name_dictionary，含生产数据设置
        /// </summary>
        public string Variables { get; set; }
        /// <summary>
        /// 子选项配置
        /// </summary>
        public string Items { get; set; }

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
        public List<KanBanItemEnum> ItemList
        {
            get
            {
                var vl = new List<KanBanItemEnum>();
                try
                {
                    if (!Items.IsNullOrEmpty())
                    {
                        vl.AddRange(JsonConvert.DeserializeObject<IEnumerable<KanBanItemEnum>>(Items));
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                Items = vl.ToJSON();
                return vl;
            }
        }
    }
    public class MonitoringProductionData
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public DateTime Time { get; set; }
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
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }

    }

    /// <summary>
    /// 设备状态反馈
    /// </summary>
    public class DeviceStateInfo
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        /// <summary>
        /// 待机时间
        /// </summary>
        public decimal IdleSecond { get; set; }
    }
    /// <summary>
    /// 预警状态设备
    /// </summary>
    public class WarningDeviceInfo
    {
        public DateTime Time { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        /// <summary>
        /// 预警名id
        /// </summary>
        public int SetId { get; set; }
        /// <summary>
        /// 预警名
        /// </summary>
        public string SetName { get; set; }
        /// <summary>
        /// 预警项id
        /// </summary>
        /// <returns></returns>
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
        /// 预警范围
        /// </summary>
        public string Range { get; set; }
        /// <summary>
        /// 预警值
        /// </summary>
        public decimal Value { get; set; }
    }
    /// <summary>
    /// 计划号进度表
    /// </summary>
    public class ProductionSchedule
    {
        public int ProductionId { get; set; }
        public string Production { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
    /// <summary>
    /// 设备进度表
    /// </summary>
    public class DeviceSchedule
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
    /// <summary>
    /// 操作工进度表
    /// </summary>
    public class ProcessorSchedule
    {
        public int ProcessorId { get; set; }
        public string Processor { get; set; }
        public decimal Plan { get; set; }
        public decimal Actual { get; set; }
    }
}
