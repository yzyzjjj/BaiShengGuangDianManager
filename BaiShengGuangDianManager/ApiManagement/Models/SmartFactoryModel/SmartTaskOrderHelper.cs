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

        #endregion

        #region Add
        #endregion

        #region Update

        #endregion

        #region Delete

        #endregion
    }
}
