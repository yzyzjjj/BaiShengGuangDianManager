using System;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class ProductionSpecification : CommonBase
    {
        public int ProductionProcessId { get; set; }
        public string SpecificationName { get; set; }
        public string SpecificationValue { get; set; }

    }
}
