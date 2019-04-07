using System;

namespace ApiFlowCardManagement.Models
{
    public class RawMateria
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string RawMateriaName { get; set; }

    }
}
