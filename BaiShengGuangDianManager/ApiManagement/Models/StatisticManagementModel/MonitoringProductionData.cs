using System;

namespace ApiManagement.Models.StatisticManagementModel
{

    public class MonitoringProductionData
    {
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public DateTime Time { get; set; }
        public int FaChu { get; set; }
        public int HeGe { get; set; }
        public int LiePian { get; set; }
        public decimal Rate { get; set; }
        public long ProcessTime { get; set; }
    }
}
