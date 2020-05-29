using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产计划下发
    /// </summary>
    public class ManufacturePlanAssign
    {
        public int PlanId { get; set; }
        public string TaskIds { get; set; }
    }
}
