using System;

namespace ApiFlowCardManagement.Models
{
    public class FlowcardLibrary
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

    }

    public class FlowcardLibraryDetail : FlowcardLibrary
    {
        public string ProductionProcessName { get; set; }
        public string RawMateriaName { get; set; }
        public string ProcessStepName { get; set; }
        public string QualifiedNumber { get; set; }
        public int DeviceId { get; set; }
        public string Code { get; set; }
    }
}
