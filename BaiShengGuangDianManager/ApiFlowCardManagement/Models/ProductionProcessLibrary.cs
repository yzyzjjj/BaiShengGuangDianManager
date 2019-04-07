using ModelBase.Base.Utils;
using System;

namespace ApiFlowCardManagement.Models
{
    public class ProductionProcessLibrary
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string ProductionProcessName { get; set; }
    }
    public class ProductionProcessLibraryDetail : ProductionProcessLibrary
    {
        public int FlowCardCount { get; set; }
        public int QualifiedNumber { get; set; }
        public int UnqualifiedNumber { get; set; }
        public string PassRate => 0 == QualifiedNumber + UnqualifiedNumber ? "非数字" :
            ((double)QualifiedNumber / (QualifiedNumber + UnqualifiedNumber)).ToRound(4).ToString();
    }
}
