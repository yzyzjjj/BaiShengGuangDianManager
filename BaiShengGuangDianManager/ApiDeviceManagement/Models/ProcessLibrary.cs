using System;
using System.Collections.Generic;

namespace ApiDeviceManagement.Models
{
    public class ProcessLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string ProcessName { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
    }
}
