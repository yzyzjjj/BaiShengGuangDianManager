using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class FlowCardHelper : DataHelper
    {
        private FlowCardHelper()
        {
            Table = "flowcard_library";
            InsertSql =
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `FlowCardTypeId`, `FId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @FlowCardTypeId, @FId);";
            UpdateSql =
                "UPDATE flowcard_library SET `MarkedDateTime` = @MarkedDateTime, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, `InboundNum` = @InboundNum, " +
                "`Remarks` = @Remarks, `Priority` = @Priority WHERE `Id` = @Id;";

            SameField = "FlowCardName";
            MenuFields.AddRange(new[] { "Id", "FlowCardName", "ProductionProcessId", "RawMateriaId" });
        }

        public static readonly FlowCardHelper Instance = new FlowCardHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        public static IEnumerable<FlowCard> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<FlowCard>(args, true);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cId"></param>
        /// <param name="wId"></param>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<dynamic>(
                $"SELECT a.Id, `Capacity`, CategoryId, Category FROM flowcard_library a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId, a.Id;",
                new { id, cId, wId });
        }
        public static FlowCardDetail GetDetail(string flowCard)
        {
            return ServerConfig.ApiDb.Query<FlowCardDetail>("SELECT a.Id, a.ProductionProcessId, a.RawMateriaId, a.Priority, b.ProductionProcessName, c.RawMateriaName FROM `flowcard_library` " +
                                                            "a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c " +
                                                            "ON a.RawMateriaId = c.Id WHERE a.FlowCardName = @flowCard AND a.MarkedDelete = 0;",
                new { flowCard }).FirstOrDefault();
        }
        public static FlowCard GetFlowCard(string flowCard)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("FlowCardName", "=", flowCard)
            };
            return Instance.CommonGet<FlowCard>(args).FirstOrDefault();
        }
        public static IEnumerable<FlowCard> GetFlowCards(IEnumerable<string> flowCards)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("FlowCardName", "IN", flowCards)
            };
            return Instance.CommonGet<FlowCard>(args);
        }
        public static IEnumerable<FlowCard> GetFlowCardsByProduction(int productId)
        {
            return ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM flowcard_library WHERE ProductionProcessId = @productId;",
                new { productId });
        }
        public static IEnumerable<FlowCard> GetFlowCardsByProductions(IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM flowcard_library WHERE ProductionProcessId IN @productIds;",
                new { productIds });
        }
        public static IEnumerable<FlowCardDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<FlowCardDetail>(
                $"SELECT a.*, b.Category FROM flowcard_library a JOIN t_process_code_category b ON a.CategoryId = b.Id " +
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
                #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}
