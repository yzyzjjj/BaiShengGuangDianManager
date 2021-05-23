using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.Warning;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    /// <summary>
    /// 看板类型
    /// </summary>
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
    /// <summary>
    /// 看板子选项类型
    /// </summary>
    public enum KanBanItemEnum
    {
        [Description("无")]
        无 = 0,
        #region 生产相关看板
        [Description("异常报警")]
        异常报警 = 1,
        [Description("异常统计")]
        异常统计,
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
        [Description("故障状态反馈")]
        故障状态反馈,
        [Description("计划号工序推移图")]
        计划号工序推移图,
        [Description("设备工序推移图")]
        设备工序推移图,
        [Description("操作工工序推移图")]
        操作工工序推移图,
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

    /// <summary>
    /// 看板子选项显示类型
    /// </summary>
    public enum KanBanItemDisplayEnum
    {
        [Description("表格")]
        Table = 0,
        [Description("图表")]
        Chart = 1,
    }

    /// <summary>
    /// 看板子选项显示类型
    /// </summary>
    public enum KanBanItemDisplayTypeEnum
    {
        [Description("折线/面积图")]
        Line = 0,
        [Description("柱状/条形图")]
        Bar = 1,
        [Description("饼图")]
        Pie = 2,
    }
    /// <summary>
    /// 看板班次配置
    /// </summary>
    public enum KanBanShiftsEnum
    {
        [Description("当前班次")]
        当前班次 = 0,
        [Description("上个班")]
        上个班 = 1,
        [Description("今日")]
        今日 = 2,
        [Description("昨日")]
        昨日 = 3
    }

    public class MonitoringKanBan : MonitoringProcess
    {
        public MonitoringKanBan()
        {
            ProductionList = new List<MonitoringProductionData>();
            MSetData = new List<MonitoringSetData>();
            Times = new Dictionary<string, DateTime>();

            ItemData = new Dictionary<string, List<object>>();
            //WarningLogs = new Dictionary<string, List<WarningLog>>();
            //WarningStatistics = new Dictionary<string, List<WarningStatistic>>();
            //DeviceStateInfos = new Dictionary<string, List<DeviceStateInfo>>();
            //WarningDeviceInfos = new Dictionary<string, List<WarningDeviceInfo>>();
            //ProductionSchedules = new Dictionary<string, List<ProductionSchedule>>();
            //DeviceSchedules = new Dictionary<string, List<DeviceSchedule>>();
            //ProcessorSchedules = new Dictionary<string, List<ProcessorSchedule>>();
        }
        public void Check(List<KanBanItemSet> itemList)
        {
            var wl = itemList.Select(x => $"{(int)x.Item}_{x.Col}_{x.Order}");
            ItemData = ItemData.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);

            //WarningLogs = WarningLogs.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //WarningStatistics = WarningStatistics.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //DeviceStateInfos = DeviceStateInfos.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //WarningDeviceInfos = WarningDeviceInfos.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //ProductionSchedules = ProductionSchedules.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //DeviceSchedules = DeviceSchedules.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //ProcessorSchedules = ProcessorSchedules.Where(x => wl.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
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
        public Dictionary<string, DateTime> Times { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public Dictionary<string, List<dynamic>> ItemData { get; set; }

        /// <summary>
        /// 报警数据
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<WarningLog>> WarningLogs { get; set; }
        /// <summary>
        /// 报警统计数据
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<WarningStatistic>> WarningStatistics { get; set; }
        /// <summary>
        /// 设备状态反馈
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<DeviceStateInfo>> DeviceStateInfos { get; set; }
        /// <summary>
        /// 预警状态设备
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<WarningDeviceInfo>> WarningDeviceInfos { get; set; }
        /// <summary>
        /// 计划号日进度表
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<ProductionSchedule>> ProductionSchedules { get; set; }
        /// <summary>
        /// 设备日进度表
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<DeviceSchedule>> DeviceSchedules { get; set; }
        /// <summary>
        /// 操作工日进度表
        /// </summary>
        [Obsolete]
        public Dictionary<string, List<ProcessorSchedule>> ProcessorSchedules { get; set; }
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
    /// <summary>
    /// 看板设置
    /// </summary>
    public class MonitoringKanBanSet : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
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

                //Variables = vl.Select(x => new { x.ScriptId, x.VariableTypeId, VariableName = x.VariableName ?? "", x.PointerAddress, x.Order }).ToJSON();
                return vl;
            }
        }
        public List<KanBanItemSet> ItemList
        {
            get
            {
                var vl = new List<KanBanItemSet>();
                try
                {
                    if (!Items.IsNullOrEmpty())
                    {
                        vl.AddRange(JsonConvert.DeserializeObject<IEnumerable<KanBanItemSet>>(Items));
                        //vl.AddRange(JsonConvert.DeserializeObject<IEnumerable<KanBanItemEnum>>(Items));
                    }
                }
                catch (Exception)
                {
                    // ignored
                    var ov = new List<KanBanItemEnum>();
                    try
                    {
                        if (!Items.IsNullOrEmpty())
                        {
                            ov.AddRange(JsonConvert.DeserializeObject<IEnumerable<KanBanItemEnum>>(Items));
                        }

                        var i = 0;
                        vl.AddRange(ov.Select(x => new KanBanItemSet
                        {
                            Item = x,
                            Order = i++,
                            Shifts = KanBanShiftsEnum.当前班次
                        }));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                Items = vl.ToJSON();
                return vl;
            }
        }
    }

    /// <summary>
    /// 看板设置 子选项设置
    /// </summary>
    public class KanBanItemSet
    {
        public KanBanItemEnum Item { get; set; }
        /// <summary>
        /// 列序
        /// </summary>
        public int Col { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        public KanBanShiftsEnum Shifts { get; set; }

        #region 显示时长
        public int Hour { get; set; }
        public int Min { get; set; }
        /// <summary>
        /// 其他参数
        /// 工序推移图  [0][0] 班制 [0][1]数据类型 [0][2]时间范围;[1][...] 工序;[2][...] 计划号;[3][...] 操作工
        /// </summary>
        //public string Configs { get; set; }
        public int[][] ConfigList { get; set; } = new int[0][];
        //{
        //    get
        //    {
        //        try
        //        {
        //            if (!Configs.IsNullOrEmpty())
        //            {
        //                return JsonConvert.DeserializeObject<int[][]>(Configs);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            // ignored
        //        }

        //        return new int[0][];
        //    }
        //}
        /// <summary>
        /// 字段配置
        /// </summary>
        //public string Fields { get; set; }
        public List<KanBanTableFieldSet> FieldList { get; set; } = new List<KanBanTableFieldSet>();
        //{
        //    get
        //    {
        //        var vl = new List<KanBanTableFieldSet>();
        //        try
        //        {
        //            if (!Fields.IsNullOrEmpty())
        //            {
        //                vl.AddRange(JsonConvert.DeserializeObject<IEnumerable<KanBanTableFieldSet>>(Fields));
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            // ignored
        //        }

        //        Fields = vl.ToJSON();
        //        return vl;
        //    }
        //}
        #endregion
    }

    public class MonitoringKanBanSetWeb : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        /// <summary>
        /// 看板名
        /// </summary>
        public string Name { get; set; }
        public bool IsShow { get; set; }
        /// <summary>
        /// 0
        /// </summary>
        public KanBanEnum Type { get; set; }
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
        public List<string> ColNameList { get; set; }
        /// <summary>
        /// 设备看板内容配置
        /// </summary>
        public string ColSet { get; set; }
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int Length => Row * Col;

        /// <summary>
        /// 设备
        /// </summary>
        public List<int> DeviceIdList { get; set; }
        /// <summary>
        /// data_name_dictionary，含生产数据设置
        /// </summary>
        public List<DataNameDictionaryOrder> VariableList { get; set; }
        /// <summary>
        /// 子选项配置
        /// </summary>
        public List<KanBanItemSetWeb> ItemList { get; set; }
    }

    public class KanBanItemSetWeb
    {
        public KanBanItemEnum Item { get; set; }
        /// <summary>
        /// 列序
        /// </summary>
        public int Col { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        public KanBanShiftsEnum Shifts { get; set; }

        #region 显示时长
        public int Hour { get; set; }
        public int Min { get; set; }
        /// <summary>
        /// 其他参数
        /// </summary>
        public int[][] ConfigList { get; set; } = new int[0][];
        /// <summary>
        /// 字段配置
        /// </summary>
        public List<KanBanTableFieldSet> FieldList { get; set; } = new List<KanBanTableFieldSet>();
        #endregion
    }

    public class KanBanItemConfig
    {
        public KanBanItemConfig(KanBanItemEnum item, bool bShifts, bool bDuration)
        {
            Item = item;
            BShifts = bShifts;
            BDuration = bDuration;
        }

        public KanBanItemConfig(KanBanItemEnum item, bool bShifts, bool bDuration, List<KanBanTableFieldConfig> fieldList, 
            KanBanItemDisplayEnum display = KanBanItemDisplayEnum.Table, KanBanItemDisplayTypeEnum displayType = KanBanItemDisplayTypeEnum.Line)
        {
            Display = display;
            DisplayType = displayType;
            Item = item;
            BShifts = bShifts;
            BDuration = bDuration;
            FieldList = fieldList;
        }

        /// <summary>
        /// 显示方式
        /// </summary>
        public KanBanItemDisplayEnum Display { get; set; }
        /// <summary>
        /// 图表方式
        /// </summary>
        public KanBanItemDisplayTypeEnum DisplayType { get; set; }
        /// <summary>
        /// 看板子选项类型
        /// </summary>
        public KanBanItemEnum Item { get; set; }
        /// <summary>
        /// 是否可配置班制
        /// </summary>
        public bool BShifts { get; set; }
        /// <summary>
        /// 是否可配置班制
        /// </summary>
        public string Name => Item.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        /// <summary>
        /// 是否可配置时长
        /// </summary>
        public bool BDuration { get; set; }
        public List<KanBanTableFieldConfig> FieldList { get; set; } = new List<KanBanTableFieldConfig>();
    }

    public class KanBanTableFieldCommon
    {
        public KanBanTableFieldCommon()
        {
        }
        public KanBanTableFieldCommon(string field, string column)
        {
            Field = field;
            Column = column;
        }
        /// <summary>
        /// 字段
        /// </summary>
        //[JsonProperty("Fie")]
        public string Field { get; set; } = "";
        /// <summary>
        /// 名称
        /// </summary>
        //[JsonProperty("Cmt")]
        public string Column { get; set; } = "";
    }

    public class KanBanTableFieldSet : KanBanTableFieldCommon
    {
        public KanBanTableFieldSet()
        {
        }
        public KanBanTableFieldSet(KanBanTableFieldConfig config)
            : base(config.Field, config.Column)
        {
        }

        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 宽度
        /// </summary>
        public string Width { get; set; } = "";
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { get; set; } = "";
        /// <summary>
        /// 处理方式
        /// </summary>
        public string Func { get; set; } = "";
        /// <summary>
        /// 前缀
        /// </summary>
        public string Pre { get; set; } = "";
        /// <summary>
        /// 后缀
        /// </summary>
        public string Suffix { get; set; } = "";
        /// <summary>
        /// 子字段
        /// </summary>
        //[JsonProperty("FieL")]
        public List<KanBanTableFieldSet> FieldList { get; set; } = new List<KanBanTableFieldSet>();
    }

    public class KanBanTableFieldConfig : KanBanTableFieldCommon
    {
        public KanBanTableFieldConfig(KanBanTableFieldConfig config)
            : base(config.Field, config.Column)
        {
            DataType = config.DataType;
            Special = config.Special;
        }

        public KanBanTableFieldConfig(string type, string field, string column)
            : base(field, column)
        {
            DataType = type;
        }

        public KanBanTableFieldConfig(string type, string special, string field, string column)
            : base(field, column)
        {
            DataType = type;
            Special = special;
        }
        public KanBanTableFieldConfig(string type, string field, string column, List<KanBanTableFieldConfig> fieldList)
            : base(field, column)
        {
            DataType = type;
            FieldList = fieldList;
        }
        public KanBanTableFieldConfig(string type, string special, string field, string column, List<KanBanTableFieldConfig> fieldList)
            : base(field, column)
        {
            DataType = type;
            Special = special;
            FieldList = fieldList;
        }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        //[JsonProperty("DType")]
        public string DataType { get; set; } = "";

        /// <summary>
        /// 字段处理类型
        /// </summary>
        //[JsonProperty("Spec")]
        public string Special { get; set; } = "";
        /// <summary>
        /// 子字段
        /// </summary>
        //[JsonProperty("FieL")]
        public List<KanBanTableFieldConfig> FieldList { get; set; } = new List<KanBanTableFieldConfig>();
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
        /// <summary>
        /// 分隔符
        /// </summary>
        public string Delimiter { get; set; }
        /// <summary>
        /// 子顺序
        /// </summary>
        public int SubOrder { get; set; }

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
}
