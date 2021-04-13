using ApiManagement.Base.Server;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartLineWorkOrderHelper : DataHelper
    {
        private SmartLineWorkOrderHelper()
        {
            Table = "t_line_work_order";
            InsertSql =
                "INSERT INTO `t_line_work_order` (`CreateUserId`, `MarkedDateTime`, `WorkOrderId`, `ProcessCodeCategoryId`, `ProcessId`, `Before`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @WorkOrderId, @ProcessCodeCategoryId, @ProcessId, @Before);";
            UpdateSql =
                "UPDATE `t_line_work_order` SET `MarkedDateTime` = @MarkedDateTime, `StartTime` = @StartTime, `EndTime` = @EndTime, `Before` = @Before, " +
                "`Qualified` = @Qualified, `Unqualified` = @Unqualified WHERE `Id` = @Id;";
        }
        public static readonly SmartLineWorkOrderHelper Instance = new SmartLineWorkOrderHelper();
        #region Get
        /// <summary>
        /// 通过任务单id获取 任务单生产线所有流程详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="workOrderId">任务单id</param>
        /// <returns></returns>
        public static IEnumerable<SmartLineWorkOrderDetail> GetSmartLineWorkOrderDetailsByWorkOrderId(int workOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartLineWorkOrderDetail>("SELECT a.*, b.WorkOrder, c.Category ProcessCodeCategory, d.Process FROM `t_line_work_order` a " +
                                                                      "JOIN `t_work_order` b ON a.WorkOrderId = b.Id " +
                                                                      "JOIN `t_process_code_category` c ON a.ProcessCodeCategoryId = c.Id " +
                                                                      "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                      "WHERE a.MarkedDelete = 0 AND WorkOrderId = @workOrderId;", new { workOrderId });
        }
        /// <summary>
        /// 通过任务单id获取 任务单生产线所有流程 详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="workOrderIds">任务单id</param>
        /// <returns></returns>
        public static IEnumerable<SmartLineWorkOrderDetail> GetSmartLineWorkOrderDetailsByWorkOrderIds(IEnumerable<int> workOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartLineWorkOrderDetail>("SELECT a.*, b.WorkOrder, c.Category ProcessCodeCategory, d.Process FROM `t_line_work_order` a " +
                                                                      "JOIN `t_work_order` b ON a.WorkOrderId = b.Id " +
                                                                      "JOIN `t_process_code_category` c ON a.ProcessCodeCategoryId = c.Id " +
                                                                      "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                      "WHERE a.MarkedDelete = 0 AND WorkOrderId IN @workOrderIds;", new { workOrderIds });
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