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
    public class RepairRecord : FaultDevice
    {
        public string FaultSolver { get; set; }
        public DateTime SolveTime { get; set; }
        public string SolvePlan { get; set; }
        public int FaultTypeId1 { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; }
        public bool IsAdd { get; set; }
        public new static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(RepairRecord).GetProperties().Where(x => (IgnoreAttribute)x.GetCustomAttributes(typeof(IgnoreAttribute), false).FirstOrDefault() == null).Select(y => y.Name);
            //var result = typeof(RepairRecord).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public new static string GetField(IEnumerable<string> except = null, string pre = "")
        {
            return GetMembers(except).Select(x => $"{pre}`{x}`").Join(", ");
        }

    }
    public class RepairRecordDetail : RepairRecord
    {
        public string FaultTypeName { get; set; }
        public string Fault1 { get; set; }
        public string SiteName { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Phone { get; set; }
        public string FaultTypeName1 { get; set; }
        public string Fault2 { get; set; }
    }
}
