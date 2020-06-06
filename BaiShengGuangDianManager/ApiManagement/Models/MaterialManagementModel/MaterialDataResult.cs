using ModelBase.Models.Result;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialDataResult : DataResult
    {
        public decimal Count { get; set; }
        public decimal Sum { get; set; }
    }
}