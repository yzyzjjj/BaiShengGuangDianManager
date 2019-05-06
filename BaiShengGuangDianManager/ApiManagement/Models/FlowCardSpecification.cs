using System;

namespace ApiManagement.Models
{
    public class FlowCardSpecification
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public int FlowCardId { get; set; }
        public string SpecificationName { get; set; }
        public string SpecificationValue { get; set; }

    }
}
