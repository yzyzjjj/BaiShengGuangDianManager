using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.MaterialManagementModel
{
    public class MaterialCategory : CommonBase
    {
        public string Category { get; set; }
        public string Remark { get; set; }
    }
}
