using System;

namespace ApiManagement.Models
{
    public class VariableType
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string TypeName { get; set; }

    }
}
