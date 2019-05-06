using System;

namespace ApiManagement.Models
{
    public partial class UsuallyFault
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public byte MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string UsuallyFaultDesc { get; set; }
        public string SolverPlan { get; set; }
    }
}
