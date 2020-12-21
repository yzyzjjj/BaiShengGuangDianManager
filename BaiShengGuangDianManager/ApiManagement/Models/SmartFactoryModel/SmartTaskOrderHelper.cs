using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

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

        public IEnumerable<SmartTaskOrder> GetArrangedButNotDoneSmartTaskOrders()
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrder>("SELECT a.*, b.`Order` FROM `t_task_order` a JOIN `t_task_order_level` b ON a.LevelId = b.Id WHERE a.MarkedDelete = 0 AND a.Arranged = 1 AND a.State NOT IN @State ORDER BY b.`Order`, a.Id;",
                new { State = new[] { SmartTaskOrderState.已完成, SmartTaskOrderState.已取消, SmartTaskOrderState.暂停中 } });
        }
        #endregion

        #region Add
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
                        ", `StartTime` = IF(@StartTime = '0001-01-01 00:00:00', `StartTime`, @StartTime)" +
                        ", `EndTime` = IF(@EndTime = '0001-01-01 00:00:00', `EndTime`, @EndTime) WHERE Id = @Id;", taskOrders);
        }

        #endregion

        #region Delete

        #endregion
    }
}
