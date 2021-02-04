using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCardHelper : DataHelper
    {
        private SmartFlowCardHelper()
        {
            Table = "t_flow_card";
            InsertSql =
                "INSERT INTO `t_flow_card` (`CreateUserId`, `MarkedDateTime`, `CreateTime`, `WorkshopId`, `FlowCard`, `TaskOrderId`, `ProcessCodeId`, `Batch`, `Number`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @CreateTime, @WorkshopId, @FlowCard, @TaskOrderId, @ProcessCodeId, @Batch, @Number, @Remark);";
            UpdateSql = "UPDATE `t_flow_card` SET `MarkedDateTime` = @MarkedDateTime, `Number` = @Number, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "FlowCard";
            MenuFields.AddRange(new[] { "Id", "CreateTime", "FlowCard", "TaskOrderId", "ProcessCodeId" });
        }
        public static readonly SmartFlowCardHelper Instance = new SmartFlowCardHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <param name="tId"></param>
        /// <param name="pcId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int wId = 0, int tId = 0, int pcId = 0)
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
            if (tId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("TaskOrderId", "=", tId));
            }
            if (pcId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ProcessCodeId", "=", pcId));
            }

            return Instance.CommonGet<SmartFlowCard>(args, true).Select(x => new { x.Id, x.CreateTime, x.FlowCard, x.TaskOrderId, x.ProcessCodeId }).OrderBy(x => x.CreateTime).ThenBy(x => x.Id);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <param name="tId"></param>
        /// <param name="pId">计划号id</param>
        /// <param name="pcId"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardDetail> GetDetail(DateTime startTime, DateTime endTime, int id = 0, int wId = 0, int tId = 0, int pId = 0, int pcId = 0)
        {

            var sql = $"SELECT a.*, b.TaskOrder, b.ProductId, b.Product, c.`Code` ProcessCode FROM `t_flow_card` a " +
                  $"LEFT JOIN (SELECT a.*, b.Product FROM `t_task_order` a JOIN `t_product` b ON a.ProductId = b.Id" +
                      $"{(pId == 0 ? "" : " WHERE a.ProductId = @productId")}) b ON a.TaskOrderId = b.Id " +
                  $"JOIN `t_process_code` c ON a.ProcessCodeId = c.Id  " +
                  $"WHERE " +
                  $"{(id == 0 ? "" : "a.Id = @id AND ")}" +
                  $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                  $"{(tId == 0 ? "" : "a.TaskOrderId = @tId AND ")}" +
                  $"{(startTime == default(DateTime) ? "" : "a.CreateTime >= @startTime AND ")}" +
                  $"{(endTime == default(DateTime) ? "" : "a.CreateTime <= @endTime AND ")}" +
                  $"a.MarkedDelete = 0;";
            return ServerConfig.ApiDb.Query<SmartFlowCardDetail>(sql, new { wId, id, tId, pcId, pId, startTime, endTime });
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("FlowCard", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<SmartFlowCard> GetSmartFlowCardsByBatch(int taskOrderId, int batch)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId AND Batch = @batch", new { taskOrderId, batch });
        }

        public static IEnumerable<SmartFlowCard> GetSmartFlowCardsByTaskOrderId(int taskOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId", new { taskOrderId });
        }
        public static IEnumerable<SmartFlowCard> GetSmartFlowCardsByWorkOrderId(int workOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT a.* FROM `t_flow_card` a JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND b.WorkOrderId = @workOrderId", new { workOrderId });
        }

        public static IEnumerable<SmartFlowCard> GetSmartFlowCardsByTaskOrderIds(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId IN @taskOrderIds", new { taskOrderIds });
        }

        public static int GetSmartFlowCardBatch(int taskOrderId)
        {
            return ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Batch), 0) FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId", new { taskOrderId }).FirstOrDefault();
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
