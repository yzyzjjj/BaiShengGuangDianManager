using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountManagementModel
{
    public class OrganizationUnitHelper : DataHelper
    {
        private OrganizationUnitHelper()
        {
            Table = "organization_units";
            InsertSql =
                "INSERT INTO organization_units (`CreateUserId`, `MarkedDateTime`, `Name`, `ParentId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @ParentId);";

            UpdateSql = "UPDATE organization_units SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly OrganizationUnitHelper Instance = new OrganizationUnitHelper();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pId">上级部门</param>
        /// <param name="sames"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static bool GetHaveSame(int pId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ParentId", "=", pId),
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pIds">上级部门</param>
        /// <param name="sames"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static bool GetHaveSame(IEnumerable<int> pIds, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ParentId", "IN", pIds),
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        /// <summary>
        /// 根据id获取组织结构
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<OrganizationUnit> GetOrganizationUnit()
        {
            var sql = "SELECT a.*, IFNULL(b.cnt, 0) MemberCount FROM `organization_units` a LEFT JOIN ( SELECT OrganizationUnitId, COUNT(1) cnt FROM `account_organization_units` " +
                    "WHERE MarkedDelete = 0 GROUP BY OrganizationUnitId ) b ON a.Id = b.OrganizationUnitId WHERE MarkedDelete = 0 ORDER BY ParentId, `Name`, Id;";
            var infos = ServerConfig.ApiDb.Query<OrganizationUnit>(sql);
            return infos;
        }

        /// <summary>
        /// 根据code获取组织结构
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static OrganizationUnit GetOrganizationUnitByCode(string code)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Code", "=", code)
            };
            return Instance.CommonGet<OrganizationUnit>(args).FirstOrDefault();
        }

        /// <summary>
        /// 根据id获取下级组织结构
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<OrganizationUnit> GetUnderOrganizationUnitsByParentId(int id)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ParentId", "=", id)
            };
            return Instance.CommonGet<OrganizationUnit>(args);
        }
        /// <summary>
        /// 根据id获取下级组织结构
        /// </summary>
        /// <param name="pIds"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IEnumerable<OrganizationUnit> GetUnderOrganizationUnits(IEnumerable<int> pIds, IEnumerable<string> names = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ParentId", "IN", pIds)
            };
            if (names != null && names.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Name", "IN", names));
            }

            return Instance.CommonGet<OrganizationUnit>(args);
        }
        /// <summary>
        /// 添加组织结构
        /// </summary>
        /// <param name="organizationUnit"></param>
        /// <returns></returns>
        public static void AddOrganizationUnit(OrganizationUnit organizationUnit)
        {
            var sql = "INSERT INTO organization_units (`CreateUserId`, `MarkedDateTime`, `ParentId`, `Code`, `CodeLink`, `Name`) " +
                      "VALUES (@CreateUserId, @MarkedDateTime, @ParentId, @Code, @CodeLink, @Name);SELECT LAST_INSERT_ID();";
            var id = ServerConfig.ApiDb.Query<int>(sql, organizationUnit).FirstOrDefault();
            sql = "UPDATE organization_units SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `CodeLink` = @CodeLink WHERE `Id` = @Id;";
            organizationUnit.Id = id;
            organizationUnit.Code = (10000 + id).ToString();
            organizationUnit.CodeLink = organizationUnit.ParentId == 0 ? $"{organizationUnit.Code}" : $"{organizationUnit.CodeLink},{organizationUnit.Code}";
            ServerConfig.ApiDb.Execute(sql, organizationUnit);
        }

        /// <summary>
        /// 更新组织结构
        /// </summary>
        /// <param name="organizationUnits"></param>
        /// <returns></returns>
        public static void UpdateOrganizationUnit(IEnumerable<OrganizationUnit> organizationUnits)
        {
            var sql = "UPDATE organization_units SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `CodeLink` = @CodeLink WHERE `Id` = @Id;";
            ServerConfig.ApiDb.Execute(sql, organizationUnits);
        }
        /// <summary>
        /// 移动组织结构
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        public static void MoveOrganizationUnit(OrganizationUnit parent, OrganizationUnit children)
        {
            var oldCodeLink = children.CodeLink;
            var newCodeLink = $"{parent.CodeLink},{children.Code}";
            children.ParentId = parent.Id;
            children.CodeLink = newCodeLink;
            var sql = "UPDATE organization_units SET `CodeLink` = @newCodeLink WHERE `CodeLink` = @oldCodeLink;";
            ServerConfig.ApiDb.Execute(sql, new { newCodeLink, oldCodeLink });

            sql = "UPDATE organization_units SET `ParentId` = @ParentId WHERE `Id` = @Id;";
            ServerConfig.ApiDb.Execute(sql, children);
        }

        /// <summary>
        /// 删除组织结构
        /// </summary>
        /// <param name="organizationUnits"></param>
        /// <returns></returns>
        public static void DeleteOrganizationUnit(IEnumerable<OrganizationUnit> organizationUnits)
        {
            var sql = "UPDATE organization_units SET `MarkedDateTime` = NOW(), `MarkedDelete` = 1 WHERE `CodeLink` LIKE @CodeLink";
            ServerConfig.ApiDb.Execute(sql, organizationUnits);
        }
        /// <summary>
        /// 删除组织结构
        /// </summary>
        /// <param name="organizationUnit"></param>
        /// <returns></returns>
        public static void DeleteOrganizationUnit(OrganizationUnit organizationUnit)
        {
            var sql = "UPDATE organization_units SET `MarkedDelete` = 1 WHERE `CodeLink` LIKE @CodeLink";
            ServerConfig.ApiDb.Execute(sql, new { CodeLink = organizationUnit.CodeLink + "%" });
        }

        /// <summary>
        /// 获取组织成员
        /// </summary>
        /// <param name="unitId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> MemberListByUnit(int unitId)
        {
            var sql = "SELECT a.Id, a.AccountId, b.`Name`, b.RoleName FROM `account_organization_units` a " +
                      //"JOIN ( SELECT a.*, b.`Name` RoleName FROM `accounts` a JOIN `roles` b ON a.Role = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 ) b ON a.AccountId = b.Id " +
                      //"WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.OrganizationUnitId = @unitId;";
                      "JOIN ( SELECT a.*, b.`Name` RoleName FROM `accounts` a JOIN `roles` b ON a.Role = b.Id ) b ON a.AccountId = b.Id " +
                      "WHERE a.MarkedDelete = 0 AND a.OrganizationUnitId = @unitId;";
            return ServerConfig.ApiDb.Query<dynamic>(sql, new { unitId });
        }

        /// <summary>
        /// 获取组织成员
        /// </summary>
        /// <param name="unitIds"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> MemberListByUnits(IEnumerable<int> unitIds)
        {
            var sql = "SELECT a.Id, a.AccountId, b.`Name`, b.RoleName, a.OrganizationUnitId FROM `account_organization_units` a " +
                    //"JOIN ( SELECT a.*, b.`Name` RoleName FROM `accounts` a JOIN `roles` b ON a.Role = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 ) b ON a.AccountId = b.Id " +
                    //"WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND a.OrganizationUnitId IN @unitIds;";
                    "JOIN ( SELECT a.*, b.`Name` RoleName FROM `accounts` a JOIN `roles` b ON a.Role = b.Id ) b ON a.AccountId = b.Id " +
                    "WHERE a.MarkedDelete = 0 AND a.OrganizationUnitId IN @unitIds;";
            return ServerConfig.ApiDb.Query<dynamic>(sql, new { unitIds });
        }
        /// <summary>
        /// 获取组织成员
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IEnumerable<OrganizationUnitMember> MemberList(IEnumerable<int> ids)
        {
            var sql = "SELECT * FROM `account_organization_units` WHERE MarkedDelete = 0 AND Id IN @ids;";
            return ServerConfig.ApiDb.Query<OrganizationUnitMember>(sql, new { ids });
        }
        /// <summary>
        /// 获取组织成员
        /// </summary>
        /// <param name="accIds"></param>
        /// <returns></returns>
        public static IEnumerable<OrganizationUnitMember> MemberListByAccountIds(IEnumerable<int> accIds)
        {
            var sql = "SELECT * FROM `account_organization_units` WHERE MarkedDelete = 0 AND AccountId IN @accIds;";
            return ServerConfig.ApiDb.Query<OrganizationUnitMember>(sql, new { accIds });
        }
        /// <summary>
        /// 批量添加组织成员
        /// </summary>
        /// <param name="organizationUnit"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        public static void AddMembers(OrganizationUnit organizationUnit, IEnumerable<OrganizationUnitMember> members)
        {
            var sql = "INSERT INTO account_organization_units (`CreateUserId`, `MarkedDateTime`, `AccountId`, `OrganizationUnitId`) VALUES (@CreateUserId, @MarkedDateTime, @AccountId, @OrganizationUnitId);";
            ServerConfig.ApiDb.Execute(sql, members.Select(x => new { x.AccountId, OrganizationUnitId = organizationUnit.Id }));
        }

        /// <summary>
        /// 批量添加组织成员
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public static void AddMembers(IEnumerable<OrganizationUnitMember> members)
        {
            var sql = "INSERT INTO account_organization_units (`CreateUserId`, `MarkedDateTime`, `AccountId`, `OrganizationUnitId`) VALUES (@CreateUserId, @MarkedDateTime, @AccountId, @OrganizationUnitId);";
            ServerConfig.ApiDb.Execute(sql, members);
        }
        /// <summary>
        /// 删除组织成员
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public static void DeleteMemberByAccountId(int accountId)
        {
            var sql = "UPDATE account_organization_units SET `MarkedDelete` = 1 WHERE `AccountId` = @accountId;";
            ServerConfig.ApiDb.Execute(sql, new { accountId });
        }

        /// <summary>
        /// 删除组织成员
        /// </summary>
        /// <param name="accountIds"></param>
        /// <returns></returns>
        public static void DeleteMemberByAccountIds(IEnumerable<int> accountIds)
        {
            var sql = "UPDATE account_organization_units SET `MarkedDelete` = 1 WHERE `AccountId` IN @accountIds;";
            ServerConfig.ApiDb.Execute(sql, new { accountIds });
        }
        /// <summary>
        /// 删除组织成员
        /// </summary>
        /// <param name="organizationUnit"></param>
        /// <param name="accountInfo"></param>
        /// <returns></returns>
        public static void DeleteMember(OrganizationUnit organizationUnit, AccountInfo accountInfo)
        {
            var sql = "UPDATE account_organization_units SET `MarkedDelete` = 1 WHERE AccountId = @AccountId AND OrganizationUnitId = @OrganizationUnitId;";
            ServerConfig.ApiDb.Execute(sql, new { AccountId = accountInfo.Id, OrganizationUnitId = organizationUnit.Id });
        }

        /// <summary>
        /// 删除组织成员
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static void DeleteMember(IEnumerable<int> ids)
        {
            var sql = "UPDATE account_organization_units SET `MarkedDelete` = 1 WHERE Id IN @ids;";
            ServerConfig.ApiDb.Execute(sql, new { ids });
        }
    }
}
