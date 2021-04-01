using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.Logic;

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
        [IgnoreChange]
        public List<RawMateriaSpecification> Specifications = new List<RawMateriaSpecification>();
    }
}
