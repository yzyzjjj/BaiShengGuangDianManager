using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartTaskOrderNeedHelper : DataHelper
    {
        private SmartTaskOrderNeedHelper()
        {
            Table = "t_task_order_need";
            InsertSql =
                "INSERT INTO `t_task_order_need` (`CreateUserId`, `MarkedDateTime`, `TaskOrderId`, `ProcessId`, `ProductId`, `Target`, `Stock`, `EstimatedTime`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @TaskOrderId, @ProcessId, @ProductId, @Target, @Stock, @EstimatedTime);";
            UpdateSql = "UPDATE `t_task_order_need` SET `MarkedDateTime` = @MarkedDateTime, `Target` = @Target, `Stock` = @Stock, `EstimatedTime` = @EstimatedTime WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderNeedHelper Instance = new SmartTaskOrderNeedHelper();
        #region Get
        public IEnumerable<SmartTaskOrderNeed> GetSmartTaskOrderNeedsByTaskOrderId(int workOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderNeed>("SELECT * FROM `t_task_order` WHERE MarkedDelete = 0 AND WorkOrderId = @workOrderId;", new { workOrderId });
        }

        public IEnumerable<SmartTaskOrderNeed> GetSmartTaskOrderNeedsByTaskOrderIds(IEnumerable<int> workOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderNeed>("SELECT * FROM `t_task_order` WHERE MarkedDelete = 0 AND WorkOrderId IN @workOrderIds;", new { workOrderIds });
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
