using ModelBase.Models.BaseModel;
using Newtonsoft.Json;

namespace ApiManagement.Models.AccountManagementModel
{
    public class OrganizationUnit : CommonBase
    {
        /// <summary>
        /// 上级部门
        /// </summary>
        public int ParentId { get; set; }
        /// <summary>
        /// 上级部门
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 上级部门
        /// </summary>
        [JsonIgnore]
        public string CodeLink { get; set; }
        /// <summary>
        /// 上级部门
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 上级部门
        /// </summary>
        public int MemberCount { get; set; }
    }

    public class OrganizationUnitMember : CommonBase
    {
        /// <summary>
        /// 组织架构Id
        /// </summary>
        public int OrganizationUnitId { get; set; }
        /// <summary>
        /// 上级部门
        /// </summary>
        public int AccountId { get; set; }
    }
}
