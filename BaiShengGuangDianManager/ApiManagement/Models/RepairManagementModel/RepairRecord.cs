using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;
using ServiceStack;

namespace ApiManagement.Models.RepairManagementModel
{
    public class RepairRecord : CommonBase
    {
        public int DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public DateTime FaultTime { get; set; }
        public string Proposer { get; set; }
        public string FaultDescription { get; set; }
        public int Priority { get; set; }
        public string FaultSolver { get; set; }
        public DateTime SolveTime { get; set; }
        public string SolvePlan { get; set; }
        public int FaultTypeId { get; set; }
        public int FaultTypeId1 { get; set; }
        public int FaultLogId { get; set; }
        public bool Cancel { get; set; }
        public bool IsAdd { get; set; }

        public static IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(RepairRecord).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public static string GetField(IEnumerable<string> except = null, string pre = "")
        {
            return GetMembers(except).Select(x => $"{pre}`{x}`").Join(", ");
        }

    }
    public class RepairRecordDetail : RepairRecord
    {
        public string FaultTypeName { get; set; }
        public string SiteName { get; set; }
    }
}
