using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.Warning
{

    /// <summary>
    /// 生产预警可选项
    /// </summary>
    public enum WarningItemType
    {
        [Description("无")]
        Default,

        [Description("单次加工数")]
        SingleTotal,
        [Description("单次合格数")]
        SingleQualified,
        [Description("单次次品数")]
        SingleUnqualified,
        [Description("单次合格率(%)")]
        SingleQualifiedRate,
        [Description("单次次品率(%)")]
        SingleUnqualifiedRate,

        [Description("单台日加工数")]
        DeviceTotal,
        [Description("单台日合格数")]
        DeviceQualified,
        [Description("单台日次品数")]
        DeviceUnqualified,
        [Description("单台日合格率(%)")]
        DeviceQualifiedRate,
        [Description("单台日次品率(%)")]
        DeviceUnqualifiedRate,

        [Description("已选设备日总加工数")]
        DayTotal,
        [Description("已选设备日总合格数")]
        DayQualified,
        [Description("已选设备日总次品数")]
        DayUnqualified,
        [Description("已选设备日总合格率(%)")]
        DayQualifiedRate,
        [Description("已选设备日总次品率(%)")]
        DayUnqualifiedRate,
    }

    public class WarningSetItem : CommonBase
    {
        /// <summary>
        /// 预警设置id
        /// </summary>
        /// <returns></returns>
        public int SetId { get; set; }
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
        /// 报警时间频率
        /// </summary>
        /// <returns></returns>
        public int Frequency { get; set; }
        /// <summary>
        /// 报警时间单位
        /// </summary>
        /// <returns></returns>
        public WarningInterval Interval { get; set; }
        /// <summary>
        /// 异常次数上限，达到该值报警
        /// </summary>
        /// <returns></returns>
        public int Count { get; set; }
        /// <summary>
        /// 频率是否正确
        /// </summary>
        /// <returns></returns>
        public bool ValidFrequency()
        {
            if (Interval == WarningInterval.每次)
            {
                Frequency = 0;
                Count = 1;
            }
            if (Interval == WarningInterval.连续)
            {
                Frequency = 0;
            }
            return (Interval > 0 && Count > 0);
        }
        public WarningCondition Condition1 { get; set; }
        public decimal Value1 { get; set; }
        public WarningLogic Logic { get; set; }
        public WarningCondition Condition2 { get; set; }
        public decimal Value2 { get; set; }

        /// <summary>
        /// 预警范围
        /// </summary>
        public string Range
        {
            get
            {
                var range = "";
                if ((Condition1 == WarningCondition.不设置 || Condition2 == WarningCondition.不设置 || Logic != WarningLogic.不设置) && Condition1 != WarningCondition.不设置)
                {
                    var conditionInfos = new List<WarningConditionInfo>
                    {
                        new WarningConditionInfo(Condition1, Value1),
                        new WarningConditionInfo(Condition2, Value2),
                    };
                    var res = new List<string>();
                    foreach (var conditionInfo in conditionInfos)
                    {
                        var r = "";
                        switch (conditionInfo.Condition)
                        {
                            case WarningCondition.大于: r = $"大于 {conditionInfo.Value:g0}"; break;
                            case WarningCondition.大于等于: r = $"大于等于 {conditionInfo.Value:g0}"; break;
                            case WarningCondition.小于: r = $"小于 {conditionInfo.Value:g0}"; break;
                            case WarningCondition.小于等于: r = $"小于等于 {conditionInfo.Value:g0}"; break;
                            default: continue;
                        }
                        res.Add(r);
                    }

                    switch (Logic)
                    {
                        case WarningLogic.并且:
                        case WarningLogic.或者: range = res.Join($"{ Logic.ToString() }"); break;
                        case WarningLogic.不设置: range = res.Any() ? res.First() : ""; break;
                    }
                }
                return range;
            }
        }
        public int DictionaryId { get; set; }
        /// <summary>
        /// 设备id列表
        /// </summary>
        public string DeviceIds { get; set; } = string.Empty;
        public List<int> DeviceList => DeviceIds.IsNullOrEmpty() ? new List<int>() : DeviceIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();
        /// <summary>
        /// 是否处理
        /// </summary>
        /// <returns></returns>
        public bool IsDeal { get; set; }
        /// <summary>
        /// 条件1是否正确
        /// </summary>
        /// <returns></returns>
        public bool ValidCondition()
        {
            if (Condition1 == WarningCondition.不设置 && Condition2 != WarningCondition.不设置)
            {
                Condition1 = Condition2;
                Condition2 = WarningCondition.不设置;
                Value1 = Value2;
                Value2 = 0;
                Logic = WarningLogic.不设置;
            }

            return (Condition1 == WarningCondition.不设置 || Condition2 == WarningCondition.不设置 || Logic != WarningLogic.不设置) && Condition1 != WarningCondition.不设置;
        }

        /// <summary>
        /// 配置时间，总计多少秒内
        /// </summary>
        /// <returns></returns>
        public int TotalConfigSeconds
        {
            get
            {
                var total = 0;
                switch (Interval)
                {
                    case WarningInterval.秒:
                        total += Frequency;
                        break;
                    case WarningInterval.分:
                        total += 60 * Frequency;
                        break;
                    case WarningInterval.小时:
                        total += 60 * 60 * Frequency;
                        break;
                    case WarningInterval.天:
                        total += 24 * 60 * 60 * Frequency;
                        break;
                    case WarningInterval.周:
                        total += 7 * 24 * 60 * 60 * Frequency;
                        break;
                    case WarningInterval.月:
                        total += 30 * 24 * 60 * 60 * Frequency;
                        break;
                    case WarningInterval.年:
                        total += 365 * 24 * 60 * 60 * Frequency;
                        break;
                }
                return total;
            }
        }
    }

    public class WarningConditionInfo
    {
        public WarningConditionInfo(WarningCondition condition, decimal value)
        {
            Condition = condition;
            Value = value;
        }
        public WarningCondition Condition { get; set; }
        public decimal Value { get; set; }
    }
    public class WarningSetItemDetail : WarningSetItem
    {
        public WarningType WarningType { get; set; }
        /// <summary>
        /// 数据类型
        /// </summary>
        /// <returns></returns>
        public WarningDataType DataType { get; set; }
        /// <summary>
        /// 数据类型是否正确
        /// </summary>
        /// <returns></returns>
        public bool ValidDataType()
        {
            return DataType != WarningDataType.默认;
        }
        public int StepId { get; set; }
        public int ClassId { get; set; }
        public int ScriptId { get; set; }
        public int CategoryId { get; set; }
        //public string VariableName { get; set; } 改成Item
        public int PointerAddress { get; set; }
        public int VariableTypeId { get; set; }
        public int Precision { get; set; }
        /// <summary>
        /// 预警设置名称
        /// </summary>
        public string SetName { get; set; }
    }

    public class WarningCurrent : WarningSetItemDetail
    {
        public WarningCurrent()
        {
        }

        public void Update(WarningSetItemDetail config)
        {
            Item = config.Item;
            ItemType = config.ItemType;
            Interval = config.Interval;
            Frequency = config.Frequency;
            Count = config.Count;
            Condition1 = config.Condition1;
            Value1 = config.Value1;
            Condition2 = config.Condition2;
            Value2 = config.Value2;
            DictionaryId = config.DictionaryId;
            Precision = config.Precision;
        }

        public void Reset()
        {
            Trend = false;
            WarningData = new List<WarningData>();
            UpdateValues();
            Param = string.Empty;
        }
        /// <summary>
        /// 当前时间
        /// </summary>
        public DateTime CurrentTime { get; set; }
        /// <summary>
        /// 预警设置项id
        /// </summary>
        public int ItemId { get; set; }
        /// <summary>
        /// 设备id
        /// </summary>
        public int DeviceId { get; set; }
        /// <summary>
        /// 第一次出现的时间， 和EndTime相差一个频率的时间
        /// </summary>
        public DateTime StartTime => WarningData.Any() ? WarningData.First().T : default(DateTime);
        /// <summary>
        /// 最近一次出现的时间
        /// </summary>
        public DateTime EndTime => WarningData.Any() ? WarningData.Last().T : default(DateTime);
        /// <summary>
        /// 正在计数中
        /// </summary>
        public bool Counting => Current > 0;
        /// <summary>
        /// 当前满足预警条件的次数
        /// </summary>
        public int Current => WarningData.Count;
        /// <summary>
        /// 当前值
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// 是否为上升趋势，上升 / 下降
        /// </summary>
        public bool Trend { get; set; }
        /// <summary>
        /// 中间值
        /// </summary>
        public string Param { get; set; } = string.Empty;
        public List<decimal> ParamList => Param.IsNullOrEmpty() ? new List<decimal>() : Param.Split(",").Select(x => decimal.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();

        public decimal GetParam(int index)
        {
            return ParamList.Count() <= index ? 0 : ParamList[index];
        }
        public void AddParam(decimal value, int index = -1)
        {
            if (index >= 0 && ParamList.Count > index)
            {
                ParamList.Insert(index, value);
            }
            else
            {
                ParamList.Add(value);
            }
            //if (ParamList.Count() <= index)
            //{
            //    for (var i = 0; i <= (index - ParamList.Count); i++)
            //    {
            //        ParamList.Add(0);
            //    }
            //}
            //ParamList[index] += value;
            Param = ParamList.Join();
        }
        /// <summary>
        /// 满足预警条件的数据
        /// </summary>
        public string Values { get; set; }
        [JsonIgnore]
        private List<WarningData> _warningData;
        public List<WarningData> WarningData
        {
            get
            {
                _warningData = _warningData ?? (!Values.IsNullOrEmpty()
                    ? JsonConvert.DeserializeObject<List<WarningData>>(Values) : new List<WarningData>());
                return _warningData;
            }
            set => _warningData = value;
        }

        public void UpdateValues()
        {
            Values = _warningData.ToJson();
        }
    }

    public class WarningCurrentDetail : WarningCurrent
    {
        public string Code { get; set; }
        public string CategoryName { get; set; }
    }

    public class WarningData
    {
        public WarningData(DateTime t, decimal v)
        {
            T = t;
            V = v;
        }
        [JsonIgnore]
        private DateTime _t { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        [JsonIgnore]
        public DateTime T
        {
            get
            {
                _t = DateTime.Now;
                if (DateTime.TryParse(DT, out var t))
                {
                    _t = t;
                }
                DT = _t.ToStr();
                return _t;
            }
            set
            {
                _t = value;
                DT = _t.ToStr();
            }
        }
        public string DT { get; set; }
        /// <summary>
        /// 数值
        /// </summary>
        public decimal V { get; set; }
        //[JsonIgnore]
        //public WarningLog Log { get; set; }
        /// <summary>
        /// 其他参数
        /// </summary>
        public string Param { get; set; } = "[]";
        [JsonIgnore]
        public List<string> ParamList { get; set; } = new List<string>();
        public void AddParam(string param)
        {
            ParamList.Add(param);
            Param = ParamList.ToJSON();
        }
        //[JsonIgnore]
        //public WarningLog Log { get; set; }
        /// <summary>
        /// 其他参数
        /// </summary>
        public string OtherParam { get; set; } = "[]";
        [JsonIgnore]
        public List<object> OtherParamList { get; set; } = new List<object>();
        public void AddOtherParam(string param)
        {
            OtherParamList.Add(param);
            OtherParam = OtherParamList.ToJSON();
        }
        public void AddOtherParam(IEnumerable<object> param)
        {
            OtherParamList.AddRange(param);
            OtherParam = OtherParamList.ToJSON();
        }
    }
    //public class WarningSetItemConfig : WarningSetItemDetail
    //{
    //    public string DeviceIds { get; set; }
    //    public IEnumerable<int> DeviceList => DeviceIds.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
    //}
}