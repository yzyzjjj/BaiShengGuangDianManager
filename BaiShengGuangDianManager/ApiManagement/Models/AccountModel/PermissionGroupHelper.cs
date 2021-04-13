using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountModel
{
    public class PermissionGroupHelper : DataHelper
    {
        private PermissionGroupHelper()
        {
            TableName = Table = "permissions_group";
            InsertSql =
                "INSERT INTO permissions_group (`CreateUserId`, `MarkedDateTime`, `Parent`, `Level`, `Self`, `List`, `Name`, `Url`, `Order`, `Type`, `Label`, `IsPage`, `IsMenu`, `Icon`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Parent, @Level, @Self, @List, @Name, @Url, @Order, @Label, @IsPage, @IsMenu, @Icon);";

            UpdateSql = "UPDATE permissions_group SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `List` = @List WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly PermissionGroupHelper Instance = new PermissionGroupHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            return id == 0
                ? PermissionGroupsList.Values.Where(x => x.Type != 0).OrderBy(x => x.Id).Select(x => new { x.Id, x.Name })
                : PermissionGroupsList.Values.Where(x => x.Type != 0 && x.Id == id).OrderBy(x => x.Id).Select(x => new { x.Id, x.Name });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetDetail(int id = 0)
        {
            return id == 0
                ? PermissionGroupsList.Values.Where(x => x.Type != 0).OrderBy(x => x.Id)
                : PermissionGroupsList.Values.Where(x => x.Type != 0 && x.Id == id).OrderBy(x => x.Id);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            return ids != null && ids.Any()
                ? PermissionGroupsList.Values.Where(x => !ids.Contains(x.Id)).Any(x => sames.Contains(x.Name))
                : PermissionGroupsList.Values.Any(x => sames.Contains(x.Name));
        }
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion


        public static Dictionary<int, PermissionGroup> PermissionGroupsList;
        public static string TableName = "permissions_group";

        public static void LoadConfig()
        {
            PermissionGroupsList = Instance.GetAll<PermissionGroup>().ToDictionary(x => x.Id);
        }

        public static bool CheckPermissionGroup(AccountInfo accountInfo, string url)
        {
            if (PermissionGroupsList.Any())
            {
                var permission = PermissionGroupsList.Values.FirstOrDefault(x => x.Url == url);
                if (permission != null)
                {
                    var permissionsDetailList = PermissionGroupsList
                        .Where(x => accountInfo.PermissionsList.Contains(x.Key))
                        .SelectMany(y => y.Value.List.IsNullOrEmpty() ? new int[0] : y.Value.List.Split(",").Select(int.Parse))
                        .Distinct();
                    if (permissionsDetailList.Contains(permission.Id))
                    {
                        //Console.WriteLine(AccountHelper.CurrentUser.Id);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CheckPermissionGroup(IEnumerable<int> list, int id)
        {
            if (PermissionGroupsList.Any() && list != null && list.Any())
            {
                var permission = PermissionGroupsList.Where(x => list.Contains(x.Key)).SelectMany(x => x.Value.PList);
                if (permission.Any() && permission.Contains(id))
                {
                    return true;
                }
            }
            return false;
        }
        public static PermissionGroup Get(int id)
        {
            return PermissionGroupsList.ContainsKey(id) ? PermissionGroupsList[id] : null;
        }

        public static PermissionGroup Get(string url)
        {
            return PermissionGroupsList.Values.FirstOrDefault(x => x.Url == url);
        }

        public static IEnumerable<int> GetDefault()
        {
            return PermissionGroupsList.Values.Where(x => x.Type == 0).Select(x => x.Id);
        }

        public static void UpdateOrder()
        {
            var permissionsList = PermissionGroupsList.Values.OrderBy(x => x.Id);
            foreach (var parent in permissionsList.GroupBy(x => x.Parent).Select(z => z.Key))
            {
                var levels = permissionsList.Where(x => x.Parent == parent).GroupBy(y => y.Level).Select(z => z.Key);
                foreach (var level in levels)
                {
                    var i = 1;
                    var permissions = permissionsList.Where(x => x.Parent == parent && x.Level == level);
                    foreach (var permission in permissions)
                    {
                        permission.Order = i++;
                    }
                }
            }

            ServerConfig.ApiDb.Execute("UPDATE `permissions_group` SET `Order` = @Order WHERE Id = @Id AND `Order` = 0;", permissionsList);
        }
    }
}
