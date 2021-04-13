using ApiManagement.Models.OtherModel;
using ModelBase.Models.BaseModel;
using Newtonsoft.Json;
using System;

namespace ApiManagement.Models.DeviceSpotCheckModel
{
    public class SpotCheckDevice : CommonBase
    {
        public int DeviceId { get; set; }
        public int ItemId { get; set; }
        public int PlanId { get; set; }
        public int SurveyorId { get; set; }
        public string SurveyorName { get; set; }
        public DateTime PlannedTime { get; set; }
        public int LogId { get; set; }
        /// <summary>
        /// 是否过期
        /// </summary>
        [JsonIgnore]
        public bool Expired => PlannedTime < DateTime.Today.AddDays(1);
    }

    public class SpotCheckDeviceDetail : SpotCheckDevice
    {
        public string Item { get; set; }
        public string Plan { get; set; }
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

    public class SpotCheckDeviceNext : SpotCheckDeviceDetail
    {
        public string Code { get; set; }
        public string Devices { get; set; }
    }
}
