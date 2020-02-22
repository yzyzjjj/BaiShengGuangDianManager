using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class RawMateria : CommonBase
    {
        public string RawMateriaName { get; set; }

        public List<RawMateriaSpecification> RawMateriaSpecifications = new List<RawMateriaSpecification>();
    }
}
