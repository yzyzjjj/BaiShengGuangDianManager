using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace ApiManagement.Models.RepairManagementModel
{
    public class FaultDevice : CommonBase
    {
        public int DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public DateTime FaultTime { get; set; }
        public string Proposer { get; set; }
        public string FaultDescription { get; set; }
        public int Priority { get; set; }
        public int Grade { get; set; }
        public RepairStateEnum State { get; set; }
        [Ignore]
        public string StateDesc => State.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        public int FaultTypeId { get; set; }
        public bool Cancel { get; set; }
        private bool Cancel1 { get; set; }
        private bool Cancel2 { get; set; }
        public string Administrator { get; set; }
        public string Maintainer { get; set; }
        public List<string> Maintainers => Maintainer.IsNullOrEmpty() ? new List<string>() : Maintainer.Split(",").ToList();
        public DateTime AssignTime { get; set; }
        public DateTime EstimatedTime { get; set; }
        public string Remark { get; set; }
        public string Images { get; set; }
        [Ignore]
        public string[] ImageList => Images != null ? JsonConvert.DeserializeObject<string[]>(Images) : new string[0];
        public bool IsReport { get; set; }


        public static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(FaultDevice).GetProperties().Where(x => (IgnoreAttribute)x.GetCustomAttributes(typeof(IgnoreAttribute), false).FirstOrDefault() == null).Select(y => y.Name);
            //var result = typeof(FaultDevice).GetProperties().Select(x => x.Name);
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
        public string Name { get; set; }
        public string Account { get; set; }
        public string Phone { get; set; }
    }
}
