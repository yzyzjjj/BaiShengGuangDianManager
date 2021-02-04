using System;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeHelper : DataHelper
    {
        private SmartProcessCodeHelper()
        {
            Table = "t_process_code";
            InsertSql =
                "INSERT INTO `t_process_code` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Code`, `CategoryId`, `List`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Code, @CategoryId, @List, @Remark);";
            UpdateSql =
                "UPDATE `t_process_code` SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `CategoryId` = @CategoryId, `List` = @List, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Code";
            MenuFields.AddRange(new[] { "Id", "Code" });
        }
        public static readonly SmartProcessCodeHelper Instance = new SmartProcessCodeHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cId"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int cId = 0, int wId = 0)
        {
            return GetDetail(id, cId, wId).Select(x => new { x.Id, x.Code });
        }
        public static IEnumerable<SmartProcessCodeDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeDetail>(
                $"SELECT a.*, IFNULL(b.Category, '') Category FROM t_process_code a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}" +
                $"{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}" +
                $"{(wId == 0 ? "" : "b.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.Id;",
                new { id, cId, wId });
        }
        public static bool GetHaveSame(int cId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("CategoryId", "=", cId),
                new Tuple<string, string, dynamic>("Code", "IN", sames)
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
