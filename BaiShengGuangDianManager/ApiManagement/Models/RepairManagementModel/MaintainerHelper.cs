using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.RepairManagementModel
{
    public class MaintainerHelper : DataHelper
    {
        private MaintainerHelper()
        {
            Table = "maintainer";
            InsertSql =
                "INSERT INTO maintainer (`CreateUserId`, `MarkedDateTime`, `Name`, `Account`, `Phone`, `Remark`, `Order`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Account, @Phone, @Remark, @Order);";
            UpdateSql = "UPDATE maintainer SET `MarkedDateTime` = @MarkedDateTime, `Phone` = @Phone, `Remark` = @Remark, `Order` = @Order WHERE `Id` = @Id";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name", "Account" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly MaintainerHelper Instance = new MaintainerHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, string account = "")
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            if (!account.IsNullOrEmpty())
            {
                args.Add(new Tuple<string, string, dynamic>("Account", "=", account));
            }

            return Instance.CommonGet<Maintainer>(args, true).Select(x => new { x.Id, x.Name, x.Account });
        }

        public static int GetCountByAccounts(IEnumerable<string> accounts)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (accounts != null && accounts.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Account", "IN", accounts));
            }

            return Instance.CommonGetCount(args);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Account", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public void Repair(IEnumerable<Maintainer> faults)
        {

        }
        #endregion

        #region Delete
        #endregion
    }
}