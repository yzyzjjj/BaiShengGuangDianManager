using System;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCardSpecification : CommonBase
    {
        public int FlowCardId { get; set; }
        public string SpecificationName { get; set; }
        public string SpecificationValue { get; set; }

    }
}
