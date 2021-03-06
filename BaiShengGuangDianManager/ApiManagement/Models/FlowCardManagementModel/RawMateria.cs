using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class RawMateria : CommonBase
    {
        /// <summary>
        /// 原料批号
        /// </summary>
        public string RawMateriaName { get; set; }
        /// <summary>
        /// 原料规格
        /// </summary>

        public List<RawMateriaSpecification> RawMateriaSpecifications = new List<RawMateriaSpecification>();
    }
}
