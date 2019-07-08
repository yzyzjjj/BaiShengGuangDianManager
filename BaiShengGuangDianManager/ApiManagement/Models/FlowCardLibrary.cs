using System;
using System.Collections.Generic;

namespace ApiManagement.Models
{
    public class FlowCardLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string FlowCardName { get; set; }
        public int ProductionProcessId { get; set; }
        public int RawMateriaId { get; set; }
        public int RawMaterialQuantity { get; set; }
        public string Sender { get; set; }
        public string InboundNum { get; set; }
        public string Remarks { get; set; }
        public int Priority { get; set; }
        public DateTime CreateTime { get; set; }
        public int WorkshopId { get; set; }
        public List<FlowCardSpecification> Specifications = new List<FlowCardSpecification>();
        public List<FlowCardProcessStepDetail> ProcessSteps = new List<FlowCardProcessStepDetail>();

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
        public string WorkshopName { get; set; }
    }
}
