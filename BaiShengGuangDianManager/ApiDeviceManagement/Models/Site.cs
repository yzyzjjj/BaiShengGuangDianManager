using System;
using System.Collections.Generic;

namespace ApiDeviceManagement.Models
{
    public class Site
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string SiteName { get; set; }
        public string RegionDescription { get; set; }
        public string Manager { get; set; }
    }
}
