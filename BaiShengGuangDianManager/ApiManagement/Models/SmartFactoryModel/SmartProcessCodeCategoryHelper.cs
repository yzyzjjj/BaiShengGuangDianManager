using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ModelBase.Base.EnumConfig;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeCategoryHelper : DataHelper
    {
        private SmartProcessCodeCategoryHelper()
        {
            Table = "t_process_code_category";
            InsertSql =
                "INSERT INTO `t_process_code_category` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Category`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Category, @Remark);";
            UpdateSql =
                "UPDATE `t_process_code_category` SET `MarkedDateTime` = @MarkedDateTime, `Category` = @Category, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Category";
            MenuFields.AddRange(new[] { "Id", "Category" });
        }
        public static readonly SmartProcessCodeCategoryHelper Instance = new SmartProcessCodeCategoryHelper();
        #region Get
        //public static IEnumerable<SmartProcessCodeCategory> GetSmartProcessCodeCategoriesByCategories(IEnumerable<string> categories)
        //{
        //    return ServerConfig.ApiDb.Query<SmartProcessCodeCategory>("SELECT * FROM `t_process_code_category` WHERE MarkedDelete = 0 AND Category IN @categories;", new { categories });
        //}
        /// <summary>
        /// 菜单
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
            return Instance.CommonGet<SmartProcessCodeCategory>(args, true).Select(x => new { x.Id, x.Category });
        }
        public static IEnumerable<SmartProcessCodeCategory> GetDetail(int id = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeCategory>(
                $"SELECT a.*, IFNULL(b.List, '') List FROM `t_process_code_category` a " +
                $"LEFT JOIN (SELECT ProcessCodeCategoryId, GROUP_CONCAT(Process ORDER BY a.`Order`) List FROM `t_process_code_category_process` a " +
                $"JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 GROUP BY ProcessCodeCategoryId) b ON a.Id = b.ProcessCodeCategoryId " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0;",
                new { id, wId });
        }
        public static IEnumerable<SmartProcessCodeCategory> GetDetail(IEnumerable<string> categories = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (categories != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Category", "IN", categories));
            }
            return Instance.CommonGet<SmartProcessCodeCategory>(args);
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Category", "IN", sames)
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