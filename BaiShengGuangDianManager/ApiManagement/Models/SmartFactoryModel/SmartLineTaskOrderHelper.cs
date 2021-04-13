using ApiManagement.Base.Server;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartLineTaskOrderHelper : DataHelper
    {
        private SmartLineTaskOrderHelper()
        {
            Table = "t_line_task_order";
            InsertSql =
            "INSERT INTO `t_line_task_order` (`CreateUserId`, `MarkedDateTime`, `TaskOrderId`, `ProcessCodeCategoryId`, `ProcessId`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @TaskOrderId, @ProcessCodeCategoryId, @ProcessId);";
            UpdateSql =
                "UPDATE `t_line_task_order` SET `MarkedDateTime` = @MarkedDateTime, `StartTime` = @StartTime, `EndTime` = @EndTime, `Before` = @Before, " +
                "`Qualified` = @Qualified, `Unqualified` = @Unqualified WHERE `Id` = @Id;";
        }
        public static readonly SmartLineTaskOrderHelper Instance = new SmartLineTaskOrderHelper();
        #region Get
        /// <summary>
        /// 通过任务单id获取 任务单生产线所有流程详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="taskOrderId">任务单id</param>
        /// <returns></returns>
        public static IEnumerable<SmartLineTaskOrderDetail> GetSmartLineTaskOrderDetailsByTaskOrderId(int taskOrderId)
        {
            return ServerConfig.ApiDb.Query<SmartLineTaskOrderDetail>("SELECT a.*, b.TaskOrder, c.Category ProcessCodeCategory, d.Process FROM `t_line_task_order` a " +
                                                                      "JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                                                                      "JOIN `t_process_code_category` c ON a.ProcessCodeCategoryId = c.Id " +
                                                                      "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                      "WHERE a.MarkedDelete = 0 AND TaskOrderId = @taskOrderId;", new { taskOrderId });
        }
        /// <summary>
        /// 通过任务单id获取 任务单生产线所有流程 详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="taskOrderIds">任务单id</param>
        /// <returns></returns>
        public static IEnumerable<SmartLineTaskOrderDetail> GetSmartLineTaskOrderDetailsByTaskOrderIds(IEnumerable<int> taskOrderIds)
        {
            return ServerConfig.ApiDb.Query<SmartLineTaskOrderDetail>("SELECT a.*, b.TaskOrder, c.Category ProcessCodeCategory, d.Process FROM `t_line_task_order` a " +
                                                                      "JOIN `t_task_order` b ON a.TaskOrderId = b.Id " +
                                                                      "JOIN `t_process_code_category` c ON a.ProcessCodeCategoryId = c.Id " +
                                                                      "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                      "WHERE a.MarkedDelete = 0 AND TaskOrderId IN @taskOrderIds;", new { taskOrderIds });
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