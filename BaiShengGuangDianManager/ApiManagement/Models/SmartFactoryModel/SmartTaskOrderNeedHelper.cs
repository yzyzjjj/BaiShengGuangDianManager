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
                "INSERT INTO `t_task_order_need` (`CreateUserId`, `MarkedDateTime`, `Batch`, `TaskOrderId`, `ProcessId`, `PId`, " +
                "`ProductId`, `Target`, `Stock`, `DoneTarget`, `Put`, `HavePut`, `FirstArrangedTime`, `EstimatedStartTime`, `EstimatedEndTime`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Batch, @TaskOrderId, @ProcessId, @PId, " +
                "@ProductId, @Target, @Stock, @DoneTarget, @Put, @HavePut, @FirstArrangedTime, @EstimatedStartTime, @EstimatedEndTime);";
            UpdateSql = "UPDATE `t_task_order_need` SET `MarkedDateTime` = @MarkedDateTime, `Target` = @Target, `Stock` = @Stock" +
                        ", `FirstProcessStartTime` = IF(@FirstProcessStartTime = '0001-01-01 00:00:00', `FirstProcessStartTime`, @FirstProcessStartTime)" +
                        ", `EstimatedStartTime` = IF(@EstimatedStartTime = '0001-01-01 00:00:00', `EstimatedStartTime`, @EstimatedStartTime)" +
                        ", `EstimatedEndTime` = IF(@EstimatedEndTime = '0001-01-01 00:00:00', `EstimatedEndTime`, @EstimatedEndTime) WHERE `Id` = @Id;";
        }
        public static readonly SmartTaskOrderNeedHelper Instance = new SmartTaskOrderNeedHelper();
        #region Get
        public IEnumerable<SmartTaskOrderNeed> GetSmartTaskOrderNeedsByTaskOrderId(int taskOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderNeed>("SELECT * FROM `t_task_order_need` WHERE MarkedDelete = 0 AND TaskOrderId = @taskOrderId;", new { taskOrderId });
        }

        /// <summary>
        /// 任务单各工序需求情况
        /// </summary>
        /// <param name="taskOrderIds"></param>
        /// <param name="detail"></param>
        /// <returns></returns>
        public IEnumerable<SmartTaskOrderNeedDetail> GetSmartTaskOrderNeedsByTaskOrderIds(IEnumerable<int> taskOrderIds, bool detail = false)
        {
            return ServerConfig.ApiDb.Query<SmartTaskOrderNeedDetail>(!detail?
                "SELECT a.*, b.`Order`, SUM(a.Target) Target, SUM(a.DoneTarget) DoneTarget, SUM(a.Put) Put, SUM(a.HavePut) HavePut " +
                "FROM (SELECT * FROM (SELECT * FROM `t_task_order_need` WHERE TaskOrderId IN @taskOrderIds ORDER BY TaskOrderId, Batch DESC) a GROUP BY TaskOrderId, PId) a " +
                "JOIN `t_process` b ON a.PId = b.Id GROUP BY TaskOrderId, PId;"
                :
                "SELECT a.*, b.Process, b.`Order`, c.TaskOrder, d.Product, SUM(a.Target) Target, SUM(a.DoneTarget) DoneTarget, SUM(a.Put) Put, SUM(a.HavePut) HavePut " +
                "FROM (SELECT * FROM (SELECT * FROM `t_task_order_need` WHERE TaskOrderId IN @taskOrderIds ORDER BY TaskOrderId, Batch DESC) a GROUP BY TaskOrderId, PId) a " +
                "JOIN `t_process` b ON a.PId = b.Id  " +
                "JOIN `t_task_order` c ON a.TaskOrderId = c.Id " +
                "JOIN `t_product` d ON a.ProductId = d.Id " +
                "GROUP BY TaskOrderId, PId;", new { taskOrderIds });
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
