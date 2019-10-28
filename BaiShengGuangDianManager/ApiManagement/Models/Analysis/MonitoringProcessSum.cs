﻿using System;

namespace ApiManagement.Models.Analysis
{
    public class MonitoringProcessSum
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int Count { get; set; }
        public string ProcessData { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
