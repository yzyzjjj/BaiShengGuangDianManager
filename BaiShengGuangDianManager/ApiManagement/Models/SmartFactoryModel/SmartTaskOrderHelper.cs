using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderHelper : DataHelper
    {
        private SmartTaskOrderHelper()
        {
            Table = "t_task_order";
            InsertSql =
                "INSERT INTO `t_task_order` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `TaskOrder`, `WorkOrderId`, `ProductId`, `Target`, `DeliveryTime`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @TaskOrder, @WorkOrderId, @ProductId, @Target, @DeliveryTime, @Remark);";
            UpdateSql = "UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, `TaskOrder` = @TaskOrder, " +
                        "`WorkOrderId` = @WorkOrderId, `ProductId` = @ProductId, `Target` = @Target, `DeliveryTime` = @DeliveryTime, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "TaskOrder";
            MenuFields.AddRange(new[] { "Id", "TaskOrder" });
        }
        public static readonly SmartTaskOrderHelper Instance = new SmartTaskOrderHelper();
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
            return Instance.CommonGet<SmartTaskOrder>(args, true).Select(x => new { x.Id, x.TaskOrder }).OrderByDescending(x => x.Id);
        }
        public static IEnumerable<SmartTaskOrderDetail> GetDetail(int id = 0, int wId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(
                $"SELECT c.*, b.*, a.* FROM t_task_order a JOIN t_work_order b ON a.WorkOrderId = b.Id JOIN t_product c ON a.ProductId = c.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}" +
                $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                $"a.MarkedDelete = 0 ORDER BY a.Id Desc;", new { id, wId });
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("TaskOrder", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<SmartTaskOrder> GetDetailByWorkOrderId(int workOrderId)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkOrderId", "=", workOrderId),
            };
            return Instance.CommonGet<SmartTaskOrder>(args);
        }

        public static IEnumerable<SmartTaskOrder> GetDetailByWorkOrderIds(IEnumerable<int> workOrderIds)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkOrderId", "IN", workOrderIds),
            };
            return Instance.CommonGet<SmartTaskOrder>(args);
        }
        /// <summary>
        /// 获取本次排程任务单信息
        /// </summary>
        /// <param name="wId"></param>
        /// <param name="taskOrderIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrder> GetWillArrangedSmartTaskOrders(int wId, IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, IFNULL(b.`Order`, 0) `Order` FROM `t_task_order` a LEFT JOIN `t_task_order_level` b ON a.LevelId = b.Id " +
                $"WHERE a.Id IN @taskOrderIds AND a.State NOT IN @State AND a.WorkshopId = @wId AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { wId, State = new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 }, taskOrderIds });
        }
        public static IEnumerable<SmartTaskOrder> GetArrangedButNotDoneSmartTaskOrders(int wId, DateTime deliveryTime = default(DateTime))
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, b.`Order` FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id " +
                $"WHERE a.Arranged = 1 AND a.State NOT IN @State AND a.WorkshopId = @wId AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { wId, State = new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="wId"></param>
        /// <param name="deliveryTime"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrder> GetAllArrangedButNotDoneSmartTaskOrders(int wId, DateTime deliveryTime = default(DateTime), bool all = false)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, b.`Order` FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id " +
                $"WHERE a.Arranged = 1{(deliveryTime != default(DateTime) ? " AND a.DeliveryTime <= @deliveryTime" : "")} AND a.State NOT IN @State AND a.WorkshopId = @wId AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { wId, deliveryTime, State = all ? new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } : new[] { SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="wId"></param>
        /// <param name="deliveryTime"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrderDetail> GetAllArrangedButNotDoneSmartTaskOrderDetails(int wId, DateTime deliveryTime = default(DateTime), bool all = false)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(
                $"SELECT a.*, b.`Order`, c.Product, c.CapacityId, c.CategoryId FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id JOIN t_product c ON a.ProductId = c.Id " +
                $"WHERE a.Arranged = 1{(deliveryTime != default(DateTime) ? " AND a.DeliveryTime <= @deliveryTime" : "")} AND a.State NOT IN @State AND a.WorkshopId = @wId AND a.MarkedDelete = 0 " +
                $"ORDER BY b.`Order`, a.Id;",
                new { wId, deliveryTime, State = all ? new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } : new[] { SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="wId"></param>
        /// <param name="taskOrderIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartTaskOrderDetail> GetAllArrangedButNotDoneSmartTaskOrderDetails(int wId, IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(
                $"SELECT a.*, b.`Order`, c.Product, c.CapacityId, c.CategoryId FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id JOIN t_product c ON a.ProductId = c.Id " +
                $"WHERE a.Arranged = 1 AND a.Id IN @taskOrderIds AND a.WorkshopId = @wId AND a.MarkedDelete = 0 " +
                $"ORDER BY b.`Order`, a.Id;",
                new { wId, taskOrderIds });
        }
        #endregion

        #region Add
        public static int AddSmartTaskOrderBatch(string createUserId)
        {
            return ServerConfig.ApiDb.Query<int>("Call CreateTaskBatch(@createUserId);", new { createUserId }).FirstOrDefault();
        }
        #endregion

        #region Update

        public static void ArrangedUpdate(IEnumerable<SmartTaskOrder> taskOrders)
        {
            ServerConfig.ApiDb.Execute("UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, `LevelId` = @LevelId, " +
                                       "`StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime), " +
                                       "`EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime) WHERE `Id` = @Id;", taskOrders);

        }
        public static void Arrange(IEnumerable<SmartTaskOrder> taskOrders)
        {
            ServerConfig.ApiDb.Execute("UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, Arranged = 1, LevelId = 1" +
                        ", `ArrangedTime` = @ArrangedTime" +
                        ", `StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime)" +
                        ", `EndTime` = @EndTime WHERE Id = @Id;", taskOrders);
        }

        #endregion

        #region Delete

        #endregion
    }
}
