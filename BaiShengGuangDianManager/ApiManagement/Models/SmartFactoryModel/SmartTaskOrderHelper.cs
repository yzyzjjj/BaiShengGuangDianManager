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
                "INSERT INTO `t_task_order` (`CreateUserId`, `MarkedDateTime`, `TaskOrder`, `WorkOrderId`, `ProductId`, `Target`, `DeliveryTime`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @TaskOrder, @WorkOrderId, @ProductId, @Target, @DeliveryTime, @Remark);";
            UpdateSql = "UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, `TaskOrder` = @TaskOrder, " +
                        "`WorkOrderId` = @WorkOrderId, `ProductId` = @ProductId, `Target` = @Target, `DeliveryTime` = @DeliveryTime, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderHelper Instance = new SmartTaskOrderHelper();
        #region Get
        public IEnumerable<SmartTaskOrder> GetSmartTaskOrdersByWorkOrderId(int workOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>("SELECT * FROM `t_task_order` WHERE MarkedDelete = 0 AND WorkOrderId = @workOrderId;", new { workOrderId });
        }

        public IEnumerable<SmartTaskOrder> GetSmartTaskOrdersByWorkOrderIds(IEnumerable<int> workOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>("SELECT * FROM `t_task_order` WHERE MarkedDelete = 0 AND WorkOrderId IN @workOrderIds;", new { workOrderIds });
        }
        /// <summary>
        /// 获取本次排程任务单信息
        /// </summary>
        /// <param name="taskOrderIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartTaskOrder> GetWillArrangedSmartTaskOrders(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, IFNULL(b.`Order`, 0) `Order` FROM `t_task_order` a LEFT JOIN `t_task_order_level` b ON a.LevelId = b.Id WHERE a.Id IN @taskOrderIds AND a.State NOT IN @State AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { State = new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 }, taskOrderIds });
        }
        public IEnumerable<SmartTaskOrder> GetArrangedButNotDoneSmartTaskOrders(DateTime deliveryTime = default(DateTime))
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, b.`Order` FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id WHERE a.Arranged = 1 AND a.State NOT IN @State AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { State = new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="deliveryTime"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public IEnumerable<SmartTaskOrder> GetAllArrangedButNotDoneSmartTaskOrders(DateTime deliveryTime = default(DateTime), bool all = false)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>(
                $"SELECT a.*, b.`Order` FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id " +
                $"WHERE a.Arranged = 1{(deliveryTime != default(DateTime) ? " AND a.DeliveryTime <= @deliveryTime" : "")} AND a.State NOT IN @State AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { deliveryTime, State = all ? new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } : new[] { SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="deliveryTime"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public IEnumerable<SmartTaskOrderDetail> GetAllArrangedButNotDoneSmartTaskOrderDetails(DateTime deliveryTime = default(DateTime), bool all = false)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(
                $"SELECT a.*, b.`Order`, c.Product, c.CapacityId, c.CategoryId FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id JOIN t_product c ON a.ProductId = c.Id " +
                $"WHERE a.Arranged = 1{(deliveryTime != default(DateTime) ? " AND a.DeliveryTime <= @deliveryTime" : "")} AND a.State NOT IN @State AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { deliveryTime, State = all ? new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } : new[] { SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        /// <summary>
        /// 获取截止日期前的任务单
        /// </summary>
        /// <param name="taskOrderIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartTaskOrderDetail> GetAllArrangedButNotDoneSmartTaskOrderDetails(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderDetail>(
                $"SELECT a.*, b.`Order`, c.Product, c.CapacityId, c.CategoryId FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id JOIN t_product c ON a.ProductId = c.Id " +
                $"WHERE a.Arranged = 1 AND a.Id IN @taskOrderIds AND a.MarkedDelete = 0 ORDER BY b.`Order`, a.Id;",
                new { taskOrderIds });
        }

        public int GetSmartTaskOrderBatch()
        {
            return ServerConfig.ApiDb.Query<int>("SELECT Id FROM `t_task_order_batch` ORDER BY Id DESC LIMIT 1;").FirstOrDefault();
        }
        #endregion

        #region Add
        public int AddSmartTaskOrderBatch(string createUserId)
        {
            return ServerConfig.ApiDb.Query<int>("Call CreateTaskBatch(@createUserId);", new { createUserId }).FirstOrDefault();
        }
        #endregion

        #region Update

        public void ArrangedUpdate(IEnumerable<SmartTaskOrder> taskOrders)
        {
            ServerConfig.ApiDb.Execute("UPDATE `t_task_order` SET `MarkedDateTime` = @MarkedDateTime, `LevelId` = @LevelId, " +
                                       "`StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime), " +
                                       "`EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime) WHERE `Id` = @Id;", taskOrders);

        }
        public void Arrange(IEnumerable<SmartTaskOrder> taskOrders)
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
