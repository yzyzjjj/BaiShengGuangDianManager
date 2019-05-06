using System;

namespace ApiManagement.Models
{
    public class RepairRecord
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public byte MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string DeviceCode { get; set; }
        public DateTime FaultTime { get; set; }
        public string Proposer { get; set; }
        public string FaultDescription { get; set; }
        public int Priority { get; set; }
        public string FaultSolver { get; set; }
        public DateTime SolveTime { get; set; }
        public string SolvePlan { get; set; }
        public int FaultTypeId { get; set; }
    }
    public class RepairRecordDetail : RepairRecord
    {
        public string FaultTypeName { get; set; }
    }
}
