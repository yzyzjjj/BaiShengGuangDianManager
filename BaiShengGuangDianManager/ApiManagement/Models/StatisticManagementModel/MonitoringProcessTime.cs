using System;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringProcessTime
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int Count { get; set; }
        public string ProcessData { get; set; }
        public int LastTime { get; set; }
        public int MinTime { get; set; }
        public int MaxTime { get; set; }
        public int AvgTime => (MaxTime + MinTime) / 2;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
