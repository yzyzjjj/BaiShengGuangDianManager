using ModelBase.Base.Logic;
using ModelBase.Models.BaseModel;
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
        每次,
        连续,
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

    public class WarningSet : CommonBase
    {
        /// <summary>
        /// 车间Id
        /// </summary>
        public int WorkshopId { get; set; }
        public WarningType WarningType { get; set; }
        /// <summary>
        /// 数据类型
        /// </summary>
        /// <returns></returns>
        public WarningDataType DataType { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
        public int StepId { get; set; }
        [IgnoreChange]
        public string StepName { get; set; }
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
        public IEnumerable<Tuple<int, string>> CodeList { get; set; }
        /// <summary>
        /// 设备id列表
        /// </summary>
        public string DeviceIds { get; set; }
        public IEnumerable<int> DeviceList => DeviceIds.Split(",").Select(x => int.TryParse(x, out _) ? int.Parse(x) : 0).Where(y => y != 0);
    }

    public class WarningSetWithItems : WarningSet
    {
        public WarningSetWithItems()
        {
            Items = new List<WarningSetItem>();
        }
        public List<WarningSetItem> Items { get; set; }
    }
}