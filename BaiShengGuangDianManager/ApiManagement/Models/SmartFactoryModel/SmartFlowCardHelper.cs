using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
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
                "INSERT INTO `t_flow_card` (`CreateUserId`, `MarkedDateTime`, `CreateTime`, `FlowCard`, `TaskOrderId`, `ProcessCodeId`, `Batch`, `Number`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @CreateTime, @FlowCard, @TaskOrderId, @ProcessCodeId, @Batch, @Number, @Remark);";
            UpdateSql = "UPDATE `t_flow_card` SET `MarkedDateTime` = @MarkedDateTime, `Number` = @Number, `Remark` = @Remark WHERE `Id` = @Id;";
        }
        public static readonly SmartFlowCardHelper Instance = new SmartFlowCardHelper();
        #region Get
        public IEnumerable<SmartFlowCard> GetSmartFlowCardsByBatch(int taskOrderId, int batch)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId AND Batch = @batch", new { taskOrderId, batch });
        }

        public IEnumerable<SmartFlowCard> GetSmartFlowCardsByTaskOrderId(int taskOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId", new { taskOrderId });
        }
        public IEnumerable<SmartFlowCard> GetSmartFlowCardsByWorkOrderId(int workOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT a.* FROM `t_flow_card` a JOIN `t_task_order` b ON a.TaskOrderId = b.Id WHERE a.MarkedDelete = 0 AND b.MarkedDelete = 0 AND b.WorkOrderId = @workOrderId", new { workOrderId });
        }

        public IEnumerable<SmartFlowCard> GetSmartFlowCardsByTaskOrderIds(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCard>("SELECT * FROM `t_flow_card` WHERE MarkedDelete = 0 AND TaskOrderId IN @taskOrderIds", new { taskOrderIds });
        }

        public int GetSmartFlowCardBatch(int taskOrderId)
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
