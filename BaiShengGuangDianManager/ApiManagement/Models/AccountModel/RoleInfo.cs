using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountModel
{
    public class RoleInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Default { get; set; }
        public bool IsDeleted { get; set; }
        public string Permissions { get; set; }
        public IEnumerable<int> PermissionsList => Permissions.Split(',').Select(int.Parse).Distinct();

    }
}
