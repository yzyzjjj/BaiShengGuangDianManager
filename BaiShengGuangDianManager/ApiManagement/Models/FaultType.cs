using System;

namespace ApiManagement.Models
{
    public class FaultType
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public byte MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string FaultTypeName { get; set; }
        public string FaultDescription { get; set; }
    }
}
