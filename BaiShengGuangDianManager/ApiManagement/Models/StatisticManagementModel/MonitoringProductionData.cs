using System;

namespace ApiManagement.Models.StatisticManagementModel
{

    public class MonitoringProductionData
    {
        public DateTime Time { get; set; }
        public int FaChu { get; set; }
        public int HeGe { get; set; }
        public int LiePian { get; set; }
        public decimal Rate { get; set; }
        public long ProcessTime { get; set; }
    }
}
