﻿using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCardLibrary : CommonBase
    {
        public int FId { get; set; }
        public string FlowCardName { get; set; }
        public int ProductionProcessId { get; set; }
        public int RawMateriaId { get; set; }
        public int RawMaterialQuantity { get; set; }
        public string Sender { get; set; }
        public string InboundNum { get; set; }
        public string Remarks { get; set; }
        public int Priority { get; set; }
        public DateTime CreateTime { get; set; }
        public int FlowCardTypeId { get; set; }
        public List<FlowCardSpecification> Specifications = new List<FlowCardSpecification>();
        public List<FlowCardProcessStepDetail> ProcessSteps = new List<FlowCardProcessStepDetail>();

        public int YanMoFaChu { get; set; }
        public DateTime YanMoTime { get; set; }

        public int YanMoHeGe { get; set; }
        public int YanMoLiePian { get; set; }
        public int YanMoDeviceId { get; set; }
        public string YanMoJiaGongRen { get; set; }

        public DateTime CuPaoTime { get; set; }
        public int CuPaoFaChu { get; set; }
        public int CuPaoHeGe { get; set; }
        public int CuPaoLiePian { get; set; }
        public int CuPaoDeviceId { get; set; }
        public string CuPaoJiaGongRen { get; set; }

        public DateTime JingPaoTime { get; set; }
        public int JingPaoFaChu { get; set; }
        public int JingPaoHeGe { get; set; }
        public int JingPaoLiePian { get; set; }
        public int JingPaoDeviceId { get; set; }
        public string JingPaoJiaGongRen { get; set; }


    }

    public class FlowCardLibraryDetail : FlowCardLibrary
    {
        public string ProductionProcessName { get; set; }
        public string RawMateriaName { get; set; }
        public string CategoryName { get; set; }
        public string StepName { get; set; }
        public DateTime ProcessTime { get; set; }
        public string QualifiedNumber { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
        public string TypeName { get; set; }
    }
}
