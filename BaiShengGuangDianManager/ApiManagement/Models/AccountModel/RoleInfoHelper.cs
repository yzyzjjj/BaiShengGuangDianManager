using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountModel
{
    public class RoleInfoHelper : DataHelper
    {
        private RoleInfoHelper()
        {
            Table = "roles";
            InsertSql =
                "INSERT INTO roles (`CreateUserId`, `MarkedDateTime`, `Name`, `Permissions`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Permissions);";

            UpdateSql = "UPDATE roles SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Permissions` = @Permissions WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly RoleInfoHelper Instance = new RoleInfoHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<RoleInfo>(args, true).Select(x => new { x.Id, x.Name });
        }
        public static IEnumerable<RoleInfo> GetDetail(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            return Instance.CommonGet<RoleInfo>(args);
        }
        public static IEnumerable<RoleInfo> GetDetail(IEnumerable<string> names = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (names != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Name", "IN", names));
            }
            return Instance.CommonGet<RoleInfo>(args);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Name", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        /// <summary>
        /// 获取角色使用次数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static int GetUseRoleCount(int id)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Role", "=", id)
            };

            return Instance.CommonGetCount(args);
        }

        /// <summary>
        /// 获取角色使用次数
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static int GetUseRoleCount(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Role", "IN", ids)
            };

            return AccountInfoHelper.Instance.CommonGetCount(args);
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
