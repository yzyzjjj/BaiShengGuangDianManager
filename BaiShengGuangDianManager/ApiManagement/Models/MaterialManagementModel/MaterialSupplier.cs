using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialSupplier : CommonBase
    {
        public int NameId { get; set; }
        public string Supplier { get; set; }
        public string Remark { get; set; }
    }
    public class MaterialSupplierDetail : MaterialSupplier
    {
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
    }
}
