using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logic;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.Warning
{
    public enum WarningType
    {
        默认,
        设备,
        其他,
    }
    public enum WarningDataType
    {
        默认,
        设备数据,
        生产数据,
        故障数据,
        //其他数据,
    }

    public enum WarningInterval
    {
        不设置,
        秒,
        分,
        小时,
        天,
        周,
        月,
        年,
    }
    public enum WarningCondition
    {
        不设置,
        大于,
        大于等于,
        小于,
        小于等于,
    }

    public enum WarningLogic
    {
        不设置,
        并且,
        或者
    }


    public class WarningSetNoItems : CommonBase
    {
        public WarningType Type { get; set; }
        /// <summary>
        /// 数据类型
        /// </summary>
        /// <returns></returns>
        public WarningDataType DataType { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
        public int ClassId { get; set; }
        [IgnoreChange]
        public string Class { get; set; }
        public int ScriptId { get; set; }
        [IgnoreChange]
        public string Script { get; set; }
        public int CategoryId { get; set; }
        [IgnoreChange]
        public string CategoryName { get; set; }
        [IgnoreChange]
        public string Code { get; set; }
        public string DeviceIds { get; set; }
        public IEnumerable<int> DeviceList => DeviceIds.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
        public List<WarningSetItem> Items { get; set; }

        /// <summary>
        /// 是否汇总
        /// </summary>
        /// <returns></returns>
        public bool IsSum { get; set; }
        public bool HaveChange(WarningSetNoItems warningSet)
        {
            var thisProperties = GetType().GetProperties();
            var properties = warningSet.GetType().GetProperties();
            foreach (var propInfo in typeof(WarningSetNoItems).GetProperties())
            {
                var attr = (IgnoreChangeAttribute)propInfo.GetCustomAttributes(typeof(IgnoreChangeAttribute), false).FirstOrDefault();
                if (attr != null)
                {
                    continue;
                }
                //var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                //if (attr == null)
                //{
                //    continue;
                //}
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(warningSet);
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    if ((DateTime)thisValue != (DateTime)value)
                    {
                        return true;
                    }
                }
                else if (propInfo.PropertyType == typeof(decimal))
                {
                    if ((decimal)thisValue != (decimal)value)
                    {
                        return true;
                    }
                }
                else
                {
                    var oldValue = thisValue?.ToString() ?? "";
                    var newValue = value?.ToString() ?? "";
                    if (oldValue != newValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public class WarningSet : WarningSetNoItems
    {
        public WarningSet()
        {
            Items = new List<WarningSetItem>();
        }
        //public WarningType Type { get; set; }
        ///// <summary>
        ///// 数据类型
        ///// </summary>
        ///// <returns></returns>
        //public WarningDataType DataType { get; set; }
        //public string Name { get; set; }
        //public bool Enable { get; set; }
        //public int ClassId { get; set; }
        //[IgnoreChange]
        //public string Class { get; set; }
        //public int ScriptId { get; set; }
        //[IgnoreChange]
        //public string Script { get; set; }
        //public int CategoryId { get; set; }
        //[IgnoreChange]
        //public string CategoryName { get; set; }
        //public string DeviceIds { get; set; }
        //[IgnoreChange]
        //public string Code { get; set; }

        ///// <summary>
        ///// 是否汇总
        ///// </summary>
        ///// <returns></returns>
        //public bool IsSum { get; set; }
        //public bool HaveChange(WarningSet warningSet)
        //{
        //    var thisProperties = GetType().GetProperties();
        //    var properties = warningSet.GetType().GetProperties();
        //    foreach (var propInfo in typeof(WarningSet).GetProperties())
        //    {
        //        var attr = (IgnoreChangeAttribute)propInfo.GetCustomAttributes(typeof(IgnoreChangeAttribute), false).FirstOrDefault();
        //        if (attr != null)
        //        {
        //            continue;
        //        }
        //        //var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
        //        //if (attr == null)
        //        //{
        //        //    continue;
        //        //}
        //        var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
        //        var value = properties.First(x => x.Name == propInfo.Name).GetValue(warningSet);
        //        if (propInfo.PropertyType == typeof(DateTime))
        //        {
        //            if ((DateTime)thisValue != (DateTime)value)
        //            {
        //                return true;
        //            }
        //        }
        //        else if (propInfo.PropertyType == typeof(decimal))
        //        {
        //            if ((decimal)thisValue != (decimal)value)
        //            {
        //                return true;
        //            }
        //        }
        //        else
        //        {
        //            var oldValue = thisValue?.ToString() ?? "";
        //            var newValue = value?.ToString() ?? "";
        //            if (oldValue != newValue)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}
        //public IEnumerable<int> DeviceList => DeviceIds.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
        //[IgnoreChange]
        //public List<WarningSetItem> Items = new List<WarningSetItem>();
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
        public string Item { get; set; }
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
        /// 报警次数上限
        /// </summary>
        /// <returns></returns>
        public int Count { get; set; }
        /// <summary>
        /// 频率是否正确
        /// </summary>
        /// <returns></returns>
        public bool ValidFrequency()
        {
            return Interval > 0 && Count > 0;
        }
        public WarningCondition Condition1 { get; set; }
        public decimal Value1 { get; set; }
        public WarningLogic Logic { get; set; }
        public WarningCondition Condition2 { get; set; }
        public decimal Value2 { get; set; }

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
            }

            if (Condition1 != WarningCondition.不设置 && Condition2 != WarningCondition.不设置 && Logic == WarningLogic.不设置)
            {
                return false;
            }

            return Condition1 != WarningCondition.不设置;
        }
        public int DictionaryId { get; set; }
        /// <summary>
        /// 是否汇总
        /// </summary>
        /// <returns></returns>
        public bool IsSum { get; set; }

        public bool HaveChange(WarningSetItem warningSet)
        {
            var thisProperties = GetType().GetProperties();
            var properties = warningSet.GetType().GetProperties();
            foreach (var propInfo in typeof(WarningSetItem).GetProperties())
            {
                var attr = (IgnoreChangeAttribute)propInfo.GetCustomAttributes(typeof(IgnoreChangeAttribute), false).FirstOrDefault();
                if (attr != null)
                {
                    continue;
                }
                //var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                //if (attr == null)
                //{
                //    continue;
                //}
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(warningSet);
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    if ((DateTime)thisValue != (DateTime)value)
                    {
                        return true;
                    }
                }
                else if (propInfo.PropertyType == typeof(decimal))
                {
                    if ((decimal)thisValue != (decimal)value)
                    {
                        return true;
                    }
                }
                else
                {
                    var oldValue = thisValue?.ToString() ?? "";
                    var newValue = value?.ToString() ?? "";
                    if (oldValue != newValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class WarningSetItemDetail : WarningSetItem
    {
        public WarningType Type { get; set; }
        public int ClassId { get; set; }
        public int ScriptId { get; set; }
        public int CategoryId { get; set; }
        public string VariableName { get; set; }
        public int PointerAddress { get; set; }
        public int VariableTypeId { get; set; }
        public int Precision { get; set; }
        public string SetName { get; set; }
    }

    public class WarningCurrent : WarningSetItemDetail
    {
        public WarningCurrent()
        {
        }

        public WarningCurrent(WarningSetItemDetail config, int deviceId)
        {
            Type = config.Type;
            ClassId = config.ClassId;
            SetId = config.SetId;
            ScriptId = config.ScriptId;
            VariableTypeId = config.VariableTypeId;
            VariableName = config.VariableName;
            PointerAddress = config.PointerAddress;
            ItemId = config.Id;
            DeviceId = deviceId;
            DataType = config.DataType;
            Frequency = config.Frequency;
            Interval = config.Interval;
            Count = config.Count;
            Condition1 = config.Condition1;
            Value1 = config.Value1;
            Condition2 = config.Condition2;
            Logic = config.Logic;
            Value2 = config.Value2;
            DictionaryId = config.DictionaryId;
        }
        public void Update(WarningSetItemDetail config)
        {
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
            Current = 0;
            Trend = false;
            WarningData = new List<WarningData>();
            StartTime = EndTime;
            Value = 0;
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
        /// 最新时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 第一次出现的时间， 和EndTime相差一个频率的时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 当前满足预警条件的次数
        /// </summary>
        public int Current { get; set; }
        /// <summary>
        /// 当前值
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// 是否为上升趋势，上升 / 下降
        /// </summary>
        public bool Trend { get; set; }
        public string DeviceIds { get; set; }
        public List<int> DeviceList => DeviceIds.IsNullOrEmpty() ? new List<int>() : DeviceIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();
        public string Param { get; set; }
        public List<decimal> ParamList => Param.IsNullOrEmpty() ? new List<decimal>() : Param.Split(",").Select(x => decimal.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();

        public decimal GetParam(int index)
        {
            return ParamList.Count() <= index ? 0 : ParamList[index];
        }
        public void AddParam(int index, decimal value)
        {
            if (ParamList.Count() <= index)
            {
                for (var i = 0; i <= (index - ParamList.Count); i++)
                {
                    ParamList.Add(0);
                }
            }
            ParamList[index] += value;
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
                if (_warningData == null)
                {
                    _warningData = !Values.IsNullOrEmpty()
                        ? JsonConvert.DeserializeObject<List<WarningData>>(Values) : new List<WarningData>();
                }

                var str = _warningData.ToJson();
                if (Values != str)
                {
                    Values = str;
                }

                //_warningData = _warningData.ToList();
                return _warningData;
            }
            set
            {
                _warningData = value;
                Values = _warningData.ToJson();
                //Values = _warningData.OrderBy(x => x).ToJson();
            }
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
    }
    public class WarningSetItemConfig : WarningSetItemDetail
    {
        public string DeviceIds { get; set; }
        public IEnumerable<int> DeviceList => DeviceIds.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
    }
    public class WarningLog : WarningCurrent
    {
        public bool IsWarning { get; set; }
        public DateTime WarningTime { get; set; }
        public string Code { get; set; }
        public string CategoryName { get; set; }
        public WarningLog()
        {
        }

        public WarningLog(WarningCurrent config)
        {
            CurrentTime = config.CurrentTime;
            SetId = config.SetId;
            SetName = config.SetName;
            ScriptId = config.ScriptId;
            VariableTypeId = config.VariableTypeId;
            VariableName = config.VariableName;
            PointerAddress = config.PointerAddress;
            Type = config.Type;
            ClassId = config.ClassId;
            ItemId = config.ItemId;
            DeviceId = config.DeviceId;
            EndTime = config.EndTime;
            StartTime = config.StartTime;
            DataType = config.DataType;
            Frequency = config.Frequency;
            Interval = config.Interval;
            Condition1 = config.Condition1;
            Value1 = config.Value1;
            Condition2 = config.Condition2;
            Value2 = config.Value2;
            DictionaryId = config.DictionaryId;
            Count = config.Count;
            Trend = config.Trend;
            Current = config.Current;
            Values = config.Values;
        }
        public bool HaveChange(WarningLog warningSet)
        {
            var thisProperties = GetType().GetProperties();
            var properties = warningSet.GetType().GetProperties();
            foreach (var propInfo in typeof(WarningLog).GetProperties())
            {
                var attr = (IgnoreChangeAttribute)propInfo.GetCustomAttributes(typeof(IgnoreChangeAttribute), false).FirstOrDefault();
                if (attr != null)
                {
                    continue;
                }
                //var attr = (DescriptionAttribute)propInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                //if (attr == null)
                //{
                //    continue;
                //}
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(warningSet);
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    if ((DateTime)thisValue != (DateTime)value)
                    {
                        return true;
                    }
                }
                else if (propInfo.PropertyType == typeof(decimal))
                {
                    if ((decimal)thisValue != (decimal)value)
                    {
                        return true;
                    }
                }
                else
                {
                    var oldValue = thisValue.ToString();
                    var newValue = value.ToString();
                    if (oldValue != newValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
