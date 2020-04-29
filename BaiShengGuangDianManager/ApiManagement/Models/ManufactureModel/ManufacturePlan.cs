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
    public class ManufacturePlan : CommonBase
    {
        [ManufactureDescription("状态", 0)]
        public ManufacturePlanState State { get; set; } = ManufacturePlanState.Wait;
        public string StateDesc => State.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        [ManufactureDescription("计划号", 1)]
        //[Description("计划号")]
        public string Plan { get; set; }
        //public string PlanDescription = "计划号";
        [ManufactureDescription("开始时间", 2)]
        //[Description("开始时间")]
        public DateTime PlannedStartTime { get; set; }
        //public string PlannedStartTimeDescription = "开始时间";
        [ManufactureDescription("完成时间", 3)]
        //[Description("结束时间")]
        public DateTime PlannedEndTime { get; set; }
        //public string PlannedEndTimeDescription = "开始时间";
        [ManufactureDescription("预计用时小时", 4)]
        //[Description("预计用时小时")]
        public int EstimatedHour { get; set; }
        //public string EstimatedHourDescription = "预计用时小时";
        [ManufactureDescription("预计用时分", 5)]
        //[Description("预计用时分")]
        public int EstimatedMin { get; set; }
        public string EstimatedTime => DateTimeExtend.ToTimeStr(EstimatedHour * 3600 + EstimatedMin * 60, 1);
        //public string EstimatedMinDescription = "预计用时分";
        [ManufactureDescription("任务配置", 6, "Task")]
        //[Description("任务配置")]
        public int TaskId { get; set; }
        //public string TaskIdDescription = "任务配置";
        public string Task { get; set; }
        public DateTime AssignedTime { get; set; }
        public bool HaveChange(ManufacturePlan manufacturePlan, out ManufactureLog change)
        {
            var changeFlag = false;
            change = new ManufactureLog
            {
                Type = ManufactureLogType.PlanUpdate,
                ParamList = new List<ManufactureLogItem>()
            };
            var thisProperties = GetType().GetProperties();
            var properties = manufacturePlan.GetType().GetProperties();
            var tmp = new Dictionary<int, ManufactureLogItem>();
            foreach (var propInfo in typeof(ManufacturePlan).GetProperties())
            {
                var attr = (ManufactureDescription)propInfo.GetCustomAttributes(typeof(ManufactureDescription), false).FirstOrDefault();
                if (attr == null)
                {
                    continue;
                }

                var description = attr.Description;
                var order = attr.Order;
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(manufacturePlan);
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
                    if (!attr.TrueValue.IsNullOrEmpty())
                    {
                        oldValue = thisProperties.First(x => x.Name == attr.TrueValue).GetValue(this).ToString();
                        newValue = properties.First(x => x.Name == attr.TrueValue).GetValue(manufacturePlan).ToString();
                    }

                    var item = new ManufactureLogItem
                    {
                        Type = ManufactureLogType.UpdateFormat,
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
    public class ManufacturePlanItems : ManufacturePlan
    {
        public IEnumerable<ManufacturePlanItem> Items { get; set; }
    }
    public class ManufacturePlanCondition : ManufacturePlan
    {
        public int Sum { get; set; }
    }
}
