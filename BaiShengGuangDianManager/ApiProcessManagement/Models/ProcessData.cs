using System;
using System.Collections.Generic;

namespace ApiProcessManagement.Models
{
    public class ProcessData
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int ProcessManagementId { get; set; }
        public int ProcessOrder { get; set; }
        public int PressurizeMinute { get; set; }
        public int PressurizeSecond { get; set; }
        public int Pressure { get; set; }
        public int ProcessMinute { get; set; }
        public int ProcessSecond { get; set; }
        public int Speed { get; set; }

    }
}
