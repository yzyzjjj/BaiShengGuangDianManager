using System;

namespace ApiManagement.Models.OtherModel
{
    public class FlowCardReport
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int FlowCardId { get; set; }
        public DateTime StartTime { get; set; }
    }
}
