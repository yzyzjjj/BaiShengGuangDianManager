using ApiManagement.Models.OtherModel;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckItem : CommonBase
    {
        public string Item { get; set; }
        public int PlanId { get; set; }
        public bool Enable { get; set; }
        public bool Remind { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public string Unit { get; set; }
        /// <summary>
        /// 标准
        /// </summary>
        public string Reference { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
        public IntervalEnum Interval { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int NormalHour { get; set; }
        public int Week { get; set; }
        public int WeekHour { get; set; }
    }
}
