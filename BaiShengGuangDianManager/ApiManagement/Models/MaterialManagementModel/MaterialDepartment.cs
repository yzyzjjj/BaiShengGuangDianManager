using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.ComponentModel;
using System.Linq;

namespace ApiManagement.Models.MaterialManagementModel
{
    /// <summary>
    /// 核价人
    /// </summary>
    public class MaterialValuer : CommonBase
    {
        public string Valuer { get; set; }
        public string Remark { get; set; }
        public bool IsErp { get; set; }
    }
    public class MaterialDepartment : CommonBase
    {
        public string Department { get; set; }
        public string Remark { get; set; }
        public bool IsErp { get; set; }
        public bool Get { get; set; }
        public int ErpId { get; set; }
    }
    public class MaterialDepartmentMember : CommonBase
    {
        public int DepartmentId { get; set; }
        public string Member { get; set; }
        public bool IsErp { get; set; }
    }

}
