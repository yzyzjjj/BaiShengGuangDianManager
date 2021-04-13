using ModelBase.Base.Logic;
using System;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCardProcessStep : CommonBase
    {
        public int FlowCardId { get; set; }
        public int ProcessStepOrder { get; set; }
        public int ProcessStepId { get; set; }
        public string ProcessStepRequirements { get; set; }
        public decimal ProcessStepRequirementMid { get; set; }
        public int ProcessorId { get; set; }
        public DateTime ProcessTime { get; set; }
        public DateTime ProcessEndTime { get; set; }
        public int SurveyorId { get; set; }
        public DateTime SurveyTime { get; set; }
        public int QualifiedNumber { get; set; }
        public int UnqualifiedNumber { get; set; }
        public int DeviceId { get; set; }
        public bool IsReport { get; set; }
        public string QualifiedRange { get; set; }
        public decimal QualifiedMode { get; set; }

    }

    public class FlowCardProcessStepDetail : FlowCardProcessStep
    {
        [IgnoreChange]
        public bool IsSurvey { get; set; }
        [IgnoreChange]
        public string CategoryName { get; set; }
        [IgnoreChange]
        public string StepName { get; set; }
        [IgnoreChange]
        public string ProcessorName { get; set; }
        [IgnoreChange]
        public string SurveyorName { get; set; }
        [IgnoreChange]
        public string Code { get; set; } = string.Empty;
        [IgnoreChange]
        public string ProcessStepOrderName { get; set; }
        public bool HaveChange(FlowCardProcessStep info)
        {
            var thisProperties = GetType().GetProperties();
            var properties = info.GetType().GetProperties();
            foreach (var propInfo in typeof(FlowCardProcessStep).GetProperties())
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
                var value = properties.First(x => x.Name == propInfo.Name).GetValue(info);
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
