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
    /// 生产计划任务列表类
    /// </summary>
    public class ManufacturePlanTask : CommonBase
    {
        public int TotalOrder { get; set; }
        public int PlanId { get; set; }
        public string Plan { get; set; }
        public int TaskId { get; set; }
        //[ManufactureDescription("任务顺序", 0)]
        public int Order { get; set; }
        [ManufactureDescription("操作员", 1, "Processor")]
        public int Person { get; set; }
        public string Processor { get; set; }
        public int GroupId { get; set; }
        public string Group { get; set; }
        [ManufactureDescription("任务模块", 2, "Module")]
        public int ModuleId { get; set; }
        public string Module { get; set; }
        public bool IsCheck { get; set; }
        [ManufactureDescription("检验单", 3, "Check")]
        public int CheckId { get; set; }
        public string Check { get; set; }
        [ManufactureDescription("任务名", 4)]
        public string Item { get; set; }
        [ManufactureDescription("预计用时小时", 5)]
        public int EstimatedHour { get; set; }
        [ManufactureDescription("预计用时分", 6)]
        public int EstimatedMin { get; set; }
        public string EstimatedTime => DateTimeExtend.ToTimeStr((EstimatedHour * 60 + EstimatedMin) * 60, 1);
        [ManufactureDescription("绩效", 8)]
        public int Score { get; set; }
        [ManufactureDescription("任务描述", 9)]
        public string Desc { get; set; }
        //[Description("任务关联")]
        [ManufactureDescription("任务关联", 10)]
        public int Relation { get; set; }
        public int OldId { get; set; }
        public string Assignor { get; set; }
        [ManufactureDescription("任务状态", 11)]
        public ManufacturePlanTaskState State { get; set; } = ManufacturePlanTaskState.WaitAssign;
        public string StateDesc => State.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        /// <summary>
        /// 是否已创建检验项
        /// </summary>
        public bool IsCheckItem { get; set; }
        [ManufactureDescription("实际开始时间", 12)]
        public DateTime FirstStartTime { get; set; }
        public DateTime ActualStartTime { get; set; }
        [ManufactureDescription("暂停时间", 13)]
        public DateTime PauseTime { get; set; }
        [ManufactureDescription("实际完成时间", 14)]
        public DateTime ActualEndTime { get; set; }
        [ManufactureDescription("实际用时小时", 15)]
        public int ActualHour { get; set; }
        [ManufactureDescription("实际用时分", 16)]
        public int ActualMin { get; set; }
        public string ActualTime => State != ManufacturePlanTaskState.Doing
            ? DateTimeExtend.ToTimeStr((ActualHour * 60 + ActualMin) * 60, 1)
            : DateTimeExtend.ToTimeStr(((int)(DateTime.Now - ActualStartTime).TotalMinutes + (ActualHour * 60 + ActualMin)) * 60, 1);
        //public string ActualTime => State != ManufacturePlanItemState.Doing
        //    ? DateTimeExtend.ToTimeStr((ActualHour * 60 + ActualMin) * 60, 1) 
        //    : PauseTime == default(DateTime)
        //        ? DateTimeExtend.ToTimeStr(((int)(DateTime.Now - ActualStartTime).TotalMinutes + (ActualHour * 60 + ActualMin)) * 60, 1)
        //        : DateTimeExtend.ToTimeStr(((int)(DateTime.Now - PauseTime).TotalMinutes + (ActualHour * 60 + ActualMin)) * 60, 1);

        [ManufactureDescription("实际绩效", 17)]
        public int ActualScore { get; set; }
        /// <summary>
        /// 检验任务结果
        /// 0 未检验  1 合格 2 返工 3 阻塞
        /// </summary>
        [ManufactureDescription("检验结果", 18)]
        public ManufacturePlanCheckState CheckResult { get; set; } = ManufacturePlanCheckState.Wait;
        public string CheckResultDesc => CheckResult.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        [ManufactureDescription("检验说明", 19)]
        public string Remark { get; set; }
        public bool IsRedo { get; set; }
        public int RedoCount { get; set; }
        public string Surveyor { get; set; }
        public string NextTask { get; set; }

        /// <summary>
        /// 检验任务名称
        /// </summary>
        public string CheckTask { get; set; }
        /// <summary>
        /// 工序完成人
        /// </summary>
        public string CheckProcessor { get; set; }
        public IEnumerable<ManufacturePlanCheckItem> Items { get; set; }
        public bool HaveChange(ManufacturePlanTask manufacturePlan, out ManufactureLog change, IEnumerable<string> keys = null)
        {
            var changeFlag = false;
            var now = DateTime.Now;
            change = new ManufactureLog
            {
                Time = now,
                Type = ManufactureLogType.TaskUpdate
            };
            var thisProperties = GetType().GetProperties();
            var properties = manufacturePlan.GetType().GetProperties();
            var tmp = new Dictionary<int, ManufactureLogItem>();
            foreach (var propInfo in typeof(ManufacturePlanTask).GetProperties())
            {
                if (keys != null && keys.All(x => x != propInfo.Name))
                {
                    continue;
                }
                var objAttrs = propInfo.GetCustomAttributes(typeof(ManufactureDescription), true);
                if (objAttrs.Length <= 0 || !(objAttrs[0] is ManufactureDescription attr))
                {
                    continue;
                }

                var description = attr.Description;
                var order = attr.Order;
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(manufacturePlan);
                if (value == null)
                {
                    continue;
                }
                var thisValue = thisProperties.First(x => x.Name == propInfo.Name).GetValue(this);

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

                    var type = ManufactureLogType.UpdateFormat;
                    switch (propInfo.Name)
                    {
                        case "Relation":
                            type = oldValue == "0"
                                ? ManufactureLogType.UpdateRelationFormat1
                                : ManufactureLogType.UpdateRelationFormat2;
                            break;
                    }

                    if (!attr.TrueValue.IsNullOrEmpty())
                    {
                        oldValue = thisProperties.First(x => x.Name == attr.TrueValue).GetValue(this).ToString();
                        newValue = properties.First(x => x.Name == attr.TrueValue).GetValue(manufacturePlan).ToString();
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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class ManufacturePlanTaskUp
    {
        public int FromOrder { get; set; }
        public int ToOrder { get; set; }
    }
}
