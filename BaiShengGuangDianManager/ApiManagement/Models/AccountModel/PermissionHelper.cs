using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountModel
{
    public class PermissionHelper : DataHelper
    {
        private PermissionHelper()
        {
            TableName = Table = "permissions";
            InsertSql =
                "INSERT INTO permissions (`CreateUserId`, `MarkedDateTime`, `Name`, `Url`, `IsPage`, `Order`, `IsDelete`, `HostId`, `Verb`, `Type`, `Label`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Url, @IsPage, @Order, @IsDelete, @HostId, @Verb, @Type, @Label);";

            UpdateSql = "UPDATE permissions SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly PermissionHelper Instance = new PermissionHelper();
        public static string TableName;
        public static Dictionary<int, Permission> PermissionsList;
        public static void LoadConfig()
        {
            PermissionsList = Instance.GetAll<Permission>().ToDictionary(x => x.Id);
        }
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            return id == 0
                ? PermissionsList.Values.Where(x => x.Type != 0).OrderBy(x => x.Id).Select(x => new { x.Id, x.Name })
                : PermissionsList.Values.Where(x => x.Type != 0 && x.Id == id).OrderBy(x => x.Id).Select(x => new { x.Id, x.Name });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetDetail(int id = 0)
        {
            return id == 0
                ? PermissionsList.Values.Where(x => x.Type != 0).OrderBy(x => x.Id)
                : PermissionsList.Values.Where(x => x.Type != 0 && x.Id == id).OrderBy(x => x.Id);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            return ids != null && ids.Any()
                ? PermissionsList.Values.Where(x => !ids.Contains(x.Id)).Any(x => sames.Contains(x.Name))
                : PermissionsList.Values.Any(x => sames.Contains(x.Name));
        }
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion

    }
}
