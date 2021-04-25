using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
            MenuFields.AddRange(new[] { "Id", "MarkedDateTime", "ProductionProcessName" });
        }
        public static readonly ProductionHelper Instance = new ProductionHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<Production>(args, true).Select(x => new { x.Id, x.MarkedDateTime, x.ProductionProcessName }).OrderByDescending(x => x.Id);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<Production> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids == null || !ids.Any())
            {
                return new List<Production>();
            }
            args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            return Instance.CommonGet<Production>(args, true);
        }
        public static IEnumerable<ProductionDetail> GetDetail(int id = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("MarkedDateTime", ">=", startTime.DayBeginTime()));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("MarkedDateTime", "<=", endTime.DayEndTime()));
            }
            return Instance.CommonGet<ProductionDetail>(args).OrderByDescending(x => x.MarkedDateTime).ThenByDescending(x => x.Id);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ProductionProcessName", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static Production GetProduction(int fId)
        {
            return ServerConfig.ApiDb
                .Query<Production>(
                    "SELECT a.Id, b.ProductionProcessName FROM flowcard_library a JOIN production_library b ON a.ProductionProcessId = b.Id WHERE a.Id = @fId", new{ fId })
                .FirstOrDefault();
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
