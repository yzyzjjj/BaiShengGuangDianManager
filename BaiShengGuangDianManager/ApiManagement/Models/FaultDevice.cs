using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models
{
    public class FaultDevice
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public DateTime FaultTime { get; set; }
        public string Proposer { get; set; }
        public string FaultDescription { get; set; }
        public int Priority { get; set; }
        public int State { get; set; }
        public int FaultTypeId { get; set; }
        public bool Cancel { get; set; }
        private bool Cancel1 { get; set; }
        private bool Cancel2 { get; set; }

        public static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(FaultDevice).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public static string GetField(IEnumerable<string> except = null, string pre = "")
        {
            return GetMembers(except).Select(x => $"{pre}`{x}`").Join(", ");
        }
    }

    public class FaultDeviceDetail : FaultDevice
    {
        public string FaultTypeName { get; set; }
        public string SiteName { get; set; }
    }
}
