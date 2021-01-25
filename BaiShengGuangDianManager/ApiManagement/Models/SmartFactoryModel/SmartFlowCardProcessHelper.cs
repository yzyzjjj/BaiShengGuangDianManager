using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartFlowCardProcessHelper : DataHelper
    {
        private SmartFlowCardProcessHelper()
        {
            Table = "t_flow_card_process";
            InsertSql =
                "INSERT INTO `t_flow_card_process` (`CreateUserId`, `MarkedDateTime`, `FlowCardId`, `ProcessId`, `ProcessorId`, `Before`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @FlowCardId, @ProcessId, @ProcessorId, @Before);";
            UpdateSql = "UPDATE `t_flow_card_process` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, " +
                        "`StartTime` = IF(@StartTime='0001-01-01 00:00:00', `StartTime`, @StartTime), " +
                        "`EndTime` = IF(@EndTime='0001-01-01 00:00:00', `EndTime`, @EndTime), " +
                        "`Count` = @Count, `ProcessorId` = @ProcessorId, `DeviceId` = @DeviceId, `Doing` = @Doing, `Qualified` = @Qualified, `Unqualified` = @Unqualified, `Fault` = @Fault WHERE `Id` = @Id;";
        }
        public static readonly SmartFlowCardProcessHelper Instance = new SmartFlowCardProcessHelper();
        #region Get
        /// <summary>
        /// 通过流程卡id获取流程卡流程
        /// </summary>
        /// <param name="flowCardId"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardProcess> GetSmartFlowCardProcessesByFlowCardId(int flowCardId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCardProcess>("SELECT * FROM `t_flow_card_process` WHERE MarkedDelete = 0 AND FlowCardId = @flowCardId;", new { flowCardId });
        }
        /// <summary>
        /// 通过流程卡id获取流程卡流程
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardProcess> GetSmartFlowCardProcessesByFlowCardIds(IEnumerable<int> flowCardIds)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCardProcess>("SELECT * FROM `t_flow_card_process` WHERE MarkedDelete = 0 AND FlowCardId IN @flowCardIds;", new { flowCardIds });
        }
        /// <summary>
        /// 通过流程卡id获取流程卡流程 详情
        /// </summary>
        /// <param name="flowCardId"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardProcessDetail> GetSmartFlowCardProcessDetailsByFlowCardId(int flowCardId)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCardProcessDetail>("SELECT a.*, b.Process FROM `t_flow_card_process` a " +
                                                                        "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                                                                        "WHERE a.MarkedDelete = 0 AND FlowCardId = @flowCardId;", new { flowCardId });
        }
        ///// <summary>
        ///// 通过任务单id获取流程卡流程
        ///// </summary>
        ///// <param name="flowCardIds"></param>
        ///// <returns></returns>
        //public static IEnumerable<SmartFlowCardProcess> GetSmartFlowCardProcessByFlowCardIds(IEnumerable<int> flowCardIds)
        //{
        //    return ServerConfig.ApiDb.Query<SmartFlowCardProcess>("SELECT * FROM `t_flow_card_process` WHERE MarkedDelete = 0 AND FlowCardId IN @flowCardIds;", new { flowCardIds });
        //}

        /// <summary>
        /// 通过任务单id， 流程编号类型id，计划号流程id获取流程卡流程
        /// </summary>
        /// <param name="taskOrderIds"></param>
        /// <param name="processCodeCategoryIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardProcessStandard1> GetSmartFlowCardProcesses1(IEnumerable<int> taskOrderIds, IEnumerable<int> processCodeCategoryIds)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCardProcessStandard1>("SELECT a.*, b.TaskOrderId, c.ProcessCodeCategoryId, c.ProcessId StandardId FROM `t_flow_card_process` a " +
                                                                           "JOIN `t_flow_card` b ON a.FlowCardId = b.Id " +
                                                                           "JOIN (SELECT a.Id, b.ProcessCodeCategoryId, b.Id ProcessId FROM `t_product_process` a " +
                                                                           "JOIN `t_process_code_category_process` b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                                                                           "WHERE a.MarkedDelete = 0 AND b.TaskOrderId IN @taskOrderIds " +
                                                                           "AND c.ProcessCodeCategoryId IN @processCodeCategoryIds " +
                                                                           "AND a.State != @state GROUP BY b.TaskOrderId, c.ProcessCodeCategoryId, a.ProcessId",
                new { taskOrderIds, processCodeCategoryIds, state = SmartFlowCardProcessState.未加工 });
        }

        /// <summary>
        /// 通过工单id， 流程编号类型id，计划号流程id获取流程卡流程
        /// </summary>
        /// <param name="workOrderIds"></param>
        /// <param name="processCodeCategoryIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartFlowCardProcessStandard2> GetSmartFlowCardProcesses2(IEnumerable<int> workOrderIds, IEnumerable<int> processCodeCategoryIds)
        {
            return ServerConfig.ApiDb.Query<SmartFlowCardProcessStandard2>("SELECT a.*, b.WorkOrderId, c.ProcessCodeCategoryId, c.ProcessId StandardId FROM `t_flow_card_process` a " +
                                                                           "JOIN (SELECT a.*, b.WorkOrderId FROM `t_flow_card` a " +
                                                                           "JOIN `t_task_order` b ON a.TaskOrderId = b.Id) b ON a.FlowCardId = b.Id " +
                                                                           "JOIN (SELECT a.Id, b.ProcessCodeCategoryId, b.Id ProcessId FROM `t_product_process` a " +
                                                                           "JOIN `t_process_code_category_process` b ON a.ProcessId = b.Id) c ON a.ProcessId = c.Id " +
                                                                           "WHERE a.MarkedDelete = 0 AND b.WorkOrderId IN @workOrderIds " +
                                                                           "AND c.ProcessCodeCategoryId IN @processCodeCategoryIds " +
                                                                           "AND a.State != @state GROUP BY b.WorkOrderId, c.ProcessCodeCategoryId, a.ProcessId;",
                new { workOrderIds, processCodeCategoryIds, state = SmartFlowCardProcessState.未加工 });
        }
        #endregion

        #region Add
        #endregion

        #region Update
        /// <summary>
        /// 更新流程是否异常
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static void UpdateSmartFlowCardProcessFault(int id)
        {
            ServerConfig.ApiDb.Execute("UPDATE `t_flow_card_process` SET Fault = IF((SELECT COUNT(1) FROM `t_process_fault` " +
                                       "WHERE MarkedDelete = 0 AND IsDeal = 0 AND ProcessId = @id) > 0, 1, 0) WHERE Id = @id", new { id });
        }

        /// <summary>
        /// 更新流程安排
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public static void UpdateSmartFlowCardProcessArrange(IEnumerable<SmartFlowCardProcessDevice> processes)
        {
            ServerConfig.ApiDb.Execute("UPDATE `t_flow_card_process` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `ProcessorId` = @ProcessorId, `DeviceId` = @DeviceId WHERE `Id` = @Id", processes);
        }

        /// <summary>
        /// 更新下道流程发出数量
        /// </summary>
        /// <returns></returns>
        public static void UpdateSmartFlowCardProcessNextBefore(int flowCardId, int id, int before)
        {
            //ServerConfig.ApiDb.Execute("UPDATE `t_flow_card_process` SET `Before` = @before, `State` = @state " +
            //                           "WHERE MarkedDelete = 0 AND FlowCardId = @flowCardId AND Id > @id LIMIT 1;", new { flowCardId, id, before, state = SmartFlowCardProcessState.等待中 });
            ServerConfig.ApiDb.Execute("UPDATE `t_flow_card_process` SET `Before` = @before " +
                                       "WHERE MarkedDelete = 0 AND FlowCardId = @flowCardId AND Id > @id LIMIT 1;", new { flowCardId, id, before });
        }
        #endregion

        #region Delete
        /// <summary>
        /// 通过流程卡删除流程
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public static void DeleteByFlowCardIs(IEnumerable<int> flowCardIds)
        {
            ServerConfig.ApiDb.Execute("DELETE FROM `t_flow_card_process` WHERE FlowCardId IN @flowCardIds;", new { flowCardIds });
        }
        #endregion
    }
}