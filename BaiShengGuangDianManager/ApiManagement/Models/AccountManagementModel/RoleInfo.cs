using ModelBase.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountManagementModel
{
    public class RoleInfo : CommonBase
    {
        /// <summary>
        /// 角色名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 权限组ID列表
        /// </summary>
        public string Permissions { get; set; }
        /// <summary>
        /// 权限组ID列表
        /// </summary>
        public IEnumerable<int> PermissionsList => Permissions.Split(',').Select(int.Parse).Distinct();
        /// <summary>
        /// 默认角色不可删除
        /// </summary>
        public bool Default { get; set; }
        /// <summary>
        /// 新账号默认角色
        /// </summary>
        public bool New { get; set; }
    }
}
