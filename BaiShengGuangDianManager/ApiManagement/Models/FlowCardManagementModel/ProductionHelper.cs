using System;
using System.Collections.Generic;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class ProductionHelper : DataHelper
    {
        private ProductionHelper()
        {
            Table = "production_library";
            InsertSql =
                "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `ProductionProcessName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProductionProcessName);";
            UpdateSql =
                "UPDATE production_library SET `MarkedDateTime` = @MarkedDateTime, `ProductionProcessName` = @ProductionProcessName WHERE `Id` = @Id;";

            SameField = "ProductionProcessName";
            MenuFields.AddRange(new[] { "Id", "ProductionProcessName" });
        }
        public static readonly ProductionHelper Instance = new ProductionHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cId"></param>
        /// <param name="wId"></param>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<dynamic>(
                $"SELECT a.Id, `Capacity`, CategoryId, Category FROM production_library a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId, a.Id;",
                new { id, cId, wId });
        }
        public static IEnumerable<ProductionDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<ProductionDetail>(
                $"SELECT a.*, b.Category FROM production_library a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId, a.Id;",
                new { id, cId, wId });
        }
        public static bool GetHaveSame(int wId, int cId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("CategoryId", "=", cId),
                new Tuple<string, string, dynamic>("Capacity", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }

        /// <summary>
        /// 通过流程类型id获取产能类型
        /// </summary>
        /// <param name="processCodeCategoryIds">流程类型id</param>
        /// <returns></returns>
        public static IEnumerable<Production> GetSameSmartCapacities(IEnumerable<int> processCodeCategoryIds)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (processCodeCategoryIds != null)
            {
                args.Add(new Tuple<string, string, dynamic>("CategoryId", "IN", processCodeCategoryIds));
            }
            return Instance.CommonGet<Production>(args);
        }

        public static IEnumerable<Production> GetSmartCapacities(IEnumerable<string> capacities)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (capacities != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Capacity", "IN", capacities));
            }
            return Instance.CommonGet<Production>(args);
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateCategoryId(Production capacity)
        {
            var args = new List<string>
            {
                "MarkedDateTime","CategoryId"
            };
            var cons = new List<string>
            {
                "Id"
            };
            Instance.CommonUpdate(args, cons, capacity);
        }
        #endregion

        #region Delete
        #endregion
    }
}
