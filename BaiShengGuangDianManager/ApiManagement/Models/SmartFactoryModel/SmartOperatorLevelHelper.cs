using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorLevelHelper : DataHelper
    {
        private SmartOperatorLevelHelper()
        {
            Table = "t_operator_level";
            InsertSql =
                "INSERT INTO `t_operator_level` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Level`, `Order`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Level, @Order, @Remark);";
            UpdateSql = "UPDATE `t_operator_level` SET `MarkedDateTime` = @MarkedDateTime, `Level` = @Level, `Order` = @Order, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Level";
            MenuFields.AddRange(new[] { "Id", "Level", "Order" });
        }
        public static readonly SmartOperatorLevelHelper Instance = new SmartOperatorLevelHelper();
        #region Get
        /// <summary>
        /// 菜单 "Id", "`Level`", "`Order`"
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int wId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }

            return Instance.CommonGet<SmartOperatorLevel>(args, true).Select(x => new { x.Id, x.Level, x.Order }).OrderBy(x => x.Order).ThenBy(x => x.Id);
        }
        public static IEnumerable<SmartOperatorLevel> GetDetail(int id = 0, int wId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            return Instance.CommonGet<SmartOperatorLevel>(args).OrderBy(x => x.Order).ThenBy(x => x.Id);
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Level", "IN", sames)
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
        #endregion

        #region Delete
        #endregion
    }
}
