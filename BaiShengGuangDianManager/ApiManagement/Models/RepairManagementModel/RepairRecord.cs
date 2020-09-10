using ModelBase.Base.Utils;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.RepairManagementModel
{
    public class RepairRecord : FaultDevice
    {
        public string FaultSolver { get; set; }
        public DateTime SolveTime { get; set; }
        public string SolvePlan { get; set; }
        public int FaultTypeId1 { get; set; }
        public string Score { get; set; }
        [Ignore]
        public List<int> Scores => Score.IsNullOrEmpty() ? new List<int>() : Score.Split(",").Select(int.Parse).ToList();
        public string Comment { get; set; }
        [Ignore]
        public string CostTime => FaultTime == default(DateTime) || SolveTime == default(DateTime)
                ? "" : DateTimeExtend.ToTimeStr((int)(SolveTime.NoSecond() - FaultTime.NoSecond()).TotalSeconds);

        public bool IsAdd { get; set; }
        public static new IEnumerable<string> GetMembers(IEnumerable<string> except = null)
        {
            var result = typeof(RepairRecord).GetProperties().Where(x => (IgnoreAttribute)x.GetCustomAttributes(typeof(IgnoreAttribute), false).FirstOrDefault() == null).Select(y => y.Name);
            //var result = typeof(RepairRecord).GetProperties().Select(x => x.Name);
            return except == null ? result : result.Where(x => !except.Contains(x));
        }

        public static new string GetField(IEnumerable<string> except = null, string pre = "")
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
