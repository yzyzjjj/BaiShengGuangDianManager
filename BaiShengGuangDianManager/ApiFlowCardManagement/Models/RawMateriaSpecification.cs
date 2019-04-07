using System;
using System.Collections.Generic;

namespace ApiFlowCardManagement.Models
{
    public class RawMateriaSpecification
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int RawMateriaId { get; set; }
        public string SpecificationName { get; set; }
        public string SpecificationValue { get; set; }

    }
}
