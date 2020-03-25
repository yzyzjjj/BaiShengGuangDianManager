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
        [Description("任务顺序")]
        public int Order { get; set; }
        [Description("操作员")]
        public int Person { get; set; }
        public string Processor { get; set; }
        public int GroupId { get; set; }
        public string Group { get; set; }
        [Description("任务模块")]
        public int ModuleId { get; set; }
        public string Module { get; set; }
        public bool IsCheck { get; set; }
        [Description("检验单")]
        public int CheckId { get; set; }
        public string Check { get; set; }
        [Description("任务名")]
        public string Item { get; set; }
        [Description("预计用时小时")]
        public int EstimatedHour { get; set; }
        [Description("预计用时分")]
        public int EstimatedMin { get; set; }
        public string EstimatedTime => DateTimeExtend.ToTimeStr((ActualHour * 60 + ActualMin) * 60);
        [Description("绩效")]
        public int Score { get; set; }
        [Description("任务描述")]
        public string Desc { get; set; }
        [Description("任务关联")]
        public int Relation { get; set; }
        public int OldId { get; set; }
        public string Assignor { get; set; }
        [ManufactureDescription("任务状态", 1)]
        public ManufacturePlanItemState State { get; set; } = ManufacturePlanItemState.Wait;
        public string StateDesc => State.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        /// <summary>
        /// 是否已创建检验项
        /// </summary>
        public bool IsCheckItem { get; set; }
        [ManufactureDescription("实际开始时间", 2)]
        public DateTime FirstStartTime { get; set; }
        public DateTime ActualStartTime { get; set; }
        [ManufactureDescription("暂停时间", 3)]
        public DateTime PauseTime { get; set; }
        [ManufactureDescription("实际完成时间", 4)]
        public DateTime ActualEndTime { get; set; }
        [ManufactureDescription("实际用时小时", 5)]
        public int ActualHour { get; set; }
        [ManufactureDescription("实际用时分", 6)]
        public int ActualMin { get; set; }
        public string ActualTime => State != ManufacturePlanItemState.Doing
            ? DateTimeExtend.ToTimeStr((ActualHour * 60 + ActualMin) * 60)
            : PauseTime == default(DateTime)
                ? DateTimeExtend.ToTimeStr((int)(DateTime.Now - ActualStartTime).TotalSeconds)
                : DateTimeExtend.ToTimeStr((int)(DateTime.Now - PauseTime).TotalSeconds);

        [ManufactureDescription("实际绩效", 7)]
        public int ActualScore { get; set; }
        /// <summary>
        /// 检验任务结果
        /// 0 未检验  1 合格 2 返工 3 阻塞
        /// </summary>
        [ManufactureDescription("检验结果", 8)]
        public ManufacturePlanCheckState CheckResult { get; set; } = ManufacturePlanCheckState.Wait;
        public string CheckResultDesc => CheckResult.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        [ManufactureDescription("检验说明", 9)]
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
        public bool HaveChange(ManufacturePlanTask manufacturePlan, out ManufactureLog change)
        {
            var changeFlag = false;
            change = new ManufactureLog
            {
                Type = ManufactureLogType.TaskUpdate
            };
            var thisProperties = GetType().GetProperties();
            var properties = manufacturePlan.GetType().GetProperties();
            var tmp = new Dictionary<int, ManufactureLogItem>();
            foreach (var propInfo in typeof(ManufacturePlanTask).GetProperties())
            {
                var objAttrs = propInfo.GetCustomAttributes(typeof(ManufactureDescription), true);
                if (objAttrs.Length <= 0 || !(objAttrs[0] is ManufactureDescription attr))
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
