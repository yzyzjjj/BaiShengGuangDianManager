using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产计划配置
    /// </summary>
    public class ManufacturePlanItem : CommonBase
    {
        public int PlanId { get; set; }
        public string Plan { get; set; }
        [ManufactureDescription("任务顺序", 1)]
        public int Order { get; set; }
        [ManufactureDescription("操作员", 2, "Processor")]
        public int Person { get; set; }
        public string Processor { get; set; }
        public int GroupId { get; set; }
        public string Group { get; set; }
        [ManufactureDescription("任务模块", 3, "Module")]
        public int ModuleId { get; set; }
        public string Module { get; set; }
        public bool IsCheck { get; set; }
        [ManufactureDescription("检验单", 4, "Check")]
        public int CheckId { get; set; }
        public string Check { get; set; }
        [ManufactureDescription("任务名", 5)]
        public string Item { get; set; }
        [ManufactureDescription("预计用时小时", 6)]
        public int EstimatedHour { get; set; }
        [ManufactureDescription("预计用时分", 7)]
        public int EstimatedMin { get; set; }
        public string EstimatedTime => DateTimeExtend.ToTimeStr(EstimatedHour * 3600 + EstimatedMin * 60, 1);
        [ManufactureDescription("绩效", 8)]
        public int Score { get; set; }
        [ManufactureDescription("任务描述", 9)]
        public string Desc { get; set; }
        [ManufactureDescription("任务关联", 10)]
        public int Relation { get; set; }
        public bool HaveChange(ManufacturePlanItem manufacturePlanItem, out ManufactureLog change)
        {
            var changeFlag = false;
            var now = DateTime.Now;
            change = new ManufactureLog
            {
                Time = now,
                Type = ManufactureLogType.TaskUpdate
            };
            var thisProperties = GetType().GetProperties();
            var properties = manufacturePlanItem.GetType().GetProperties();
            var tmp = new Dictionary<int, ManufactureLogItem>();
            foreach (var propInfo in typeof(ManufacturePlanItem).GetProperties())
            {
                var attr = (ManufactureDescription)propInfo.GetCustomAttributes(typeof(ManufactureDescription), false).FirstOrDefault();
                if (attr == null)
                {
                    continue;
                }

                var description = attr.Description;
                var order = attr.Order;
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(manufacturePlanItem);
                string oldValue, newValue;
                if (propInfo.PropertyType == typeof(DateTime))
                {
                    oldValue = ((DateTime)thisValue).ToStr();
                    newValue = ((DateTime)value).ToStr();
                }
                else if (propInfo.PropertyType.BaseType == typeof(Enum))
                {
                    var newEnum = Enum.Parse(propInfo.PropertyType, thisValue.ToString());
                    oldValue = ((DescriptionAttribute)newEnum.GetType().GetField(newEnum.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault())?.Description ?? "";
                    var oldEnum = Enum.Parse(propInfo.PropertyType, value.ToString());
                    newValue = ((DescriptionAttribute)oldEnum.GetType().GetField(oldEnum.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault())?.Description ?? "";
                }
                else
                {
                    oldValue = thisValue.ToString();
                    newValue = value.ToString();
                }

                if (oldValue != newValue)
                {
                    changeFlag = true;
                    var type = propInfo.Name != "Relation"
                        ? ManufactureLogType.UpdateFormat
                        : oldValue == "0" ? ManufactureLogType.UpdateRelationFormat1 : ManufactureLogType.UpdateRelationFormat2;

                    if (!attr.TrueValue.IsNullOrEmpty())
                    {
                        oldValue = thisProperties.First(x => x.Name == attr.TrueValue).GetValue(this).ToString();
                        newValue = properties.First(x => x.Name == attr.TrueValue).GetValue(manufacturePlanItem).ToString();
                    }
                    var item = new ManufactureLogItem
                    {
                        Type = type,
                        Old = oldValue,
                        New = newValue,
                        Field = description,
                    };
                    if (!tmp.ContainsKey(order))
                    {
                        tmp.Add(order, item);
                    }
                }
            }
            change.ParamList.AddRange(tmp.OrderBy(x => x.Key).Select(y => y.Value));
            return changeFlag;
        }
    }
}
