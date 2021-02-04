using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartCapacityHelper : DataHelper
    {
        private SmartCapacityHelper()
        {
            Table = "t_capacity";
            InsertSql =
                "INSERT INTO `t_capacity` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Capacity`, `CategoryId`, `Number`, `Last`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Capacity, @CategoryId, @Number, @Last, @Remark);";
            UpdateSql =
                "UPDATE `t_capacity` SET `MarkedDateTime` = @MarkedDateTime, `Capacity` = @Capacity, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Capacity";
            MenuFields.AddRange(new[] { "Id", "Capacity", "CategoryId", "Category" });
        }
        public static readonly SmartCapacityHelper Instance = new SmartCapacityHelper();
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
                $"SELECT a.Id, `Capacity`, CategoryId, Category FROM t_capacity a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId, a.Id;",
                new { id, cId, wId });
        }
        public static IEnumerable<SmartCapacityDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartCapacityDetail>(
                $"SELECT a.*, b.Category FROM t_capacity a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
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
        public static IEnumerable<SmartCapacity> GetSameSmartCapacities(IEnumerable<int> processCodeCategoryIds)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (processCodeCategoryIds != null)
            {
                args.Add(new Tuple<string, string, dynamic>("CategoryId", "IN", processCodeCategoryIds));
            }
            return Instance.CommonGet<SmartCapacity>(args);
        }

        public static IEnumerable<SmartCapacity> GetSmartCapacities(IEnumerable<string> capacities)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (capacities != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Capacity", "IN", capacities));
            }
            return Instance.CommonGet<SmartCapacity>(args);
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateCategoryId(SmartCapacity capacity)
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
