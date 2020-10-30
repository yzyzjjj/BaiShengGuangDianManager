using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessFaultHelper : DataHelper
    {
        private SmartProcessFaultHelper()
        {
            Table = "t_process_fault";
            InsertSql =
                "INSERT INTO  `t_process_fault` (`CreateUserId`, `MarkedDateTime`, `FaultTime`, `Type`, `Fault`, `DeviceId`, `FlowCardId`, `ProcessId`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @FaultTime, @Type, @Fault, @DeviceId, @FlowCardId, @ProcessId, @Remark);";
            UpdateSql =
                "UPDATE `t_process_fault` SET `MarkedDateTime` = @MarkedDateTime, `IsDeal` = @IsDeal, `DealTime` = @DealTime, `DealType` = @DealType, `Deal` = @Deal WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessFaultHelper Instance = new SmartProcessFaultHelper();
        #region Get
        /// <summary>
        /// 通过流程卡id获取未解决的问题
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFault> GetSmartProcessFaultsByFlowCardIds(IEnumerable<int> flowCardIds)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFault>("SELECT * FROM `t_process_fault` WHERE MarkedDelete = 0 AND FlowCardId IN @flowCardIds AND IsDeal = false;", new { flowCardIds });
        }

        /// <summary>
        /// 通过流程卡id获取未解决的问题详情 ProcessId为流程卡流程id
        /// </summary>
        /// <param name="flowCardId"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFaultDetail> GetSmartProcessFaultDetails(int flowCardId)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFaultDetail>("SELECT a.*, IFNULL(b.`Code`, '') `Code`, c.FlowCard, d.Process FROM `t_process_fault` a " +
                                                                     "LEFT JOIN `t_device` b ON a.DeviceId = b.Id JOIN `t_flow_card` c ON a.FlowCardId = c.Id " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_flow_card_process` a " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                                                                     "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                     "WHERE a.MarkedDelete = 0 AND FlowCardId = @flowCardId AND IsDeal = FALSE;", new { flowCardId });
        }
        /// <summary>
        /// 通过流程卡id获取未解决的问题详情 ProcessId为流程卡流程id
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFaultDetail> GetSmartProcessFaultDetails(IEnumerable<int> flowCardIds)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFaultDetail>("SELECT a.*, IFNULL(b.`Code`, '') `Code`, c.FlowCard, d.Process FROM `t_process_fault` a " +
                                                                     "LEFT JOIN `t_device` b ON a.DeviceId = b.Id JOIN `t_flow_card` c ON a.FlowCardId = c.Id " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_flow_card_process` a " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a " +
                                                                     "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a " +
                                                                     "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                     "WHERE a.MarkedDelete = 0 AND FlowCardId IN @flowCardIds AND IsDeal = FALSE;", new { flowCardIds });
        }

        /// <summary>
        /// 通过流程卡id获取未解决的问题详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="flowCardId"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFaultDetail> GetSmartProcessFaultDetailsByFlowCardId(int flowCardId)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFaultDetail>("SELECT a.*, IFNULL(b.`Code`, '') `Code`, c.FlowCard, d.ProcessId, d.Process FROM `t_process_fault` a " +
                                                                     "LEFT JOIN `t_device` b ON a.DeviceId = b.Id JOIN `t_flow_card` c ON a.FlowCardId = c.Id " +
                                                                     "JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                                                                     "JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                                                                     "JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                                                                     "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                     "WHERE a.MarkedDelete = 0 AND FlowCardId = @flowCardId AND IsDeal = FALSE;", new { flowCardId });
        }
        /// <summary>
        /// 通过流程卡id获取未解决的问题详情 ProcessId为标准流程id
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFaultDetail> GetSmartProcessFaultDetailsByFlowCardIds(IEnumerable<int> flowCardIds)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFaultDetail>("SELECT a.*, IFNULL(b.`Code`, '') `Code`, c.FlowCard, d.ProcessId, d.Process FROM `t_process_fault` a " +
                                                                     "LEFT JOIN `t_device` b ON a.DeviceId = b.Id JOIN `t_flow_card` c ON a.FlowCardId = c.Id " +
                                                                     "JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_flow_card_process` a " +
                                                                     "JOIN (SELECT a.Id, b.ProcessId, b.Process FROM `t_product_process` a " +
                                                                     "JOIN (SELECT a.Id, a.ProcessId, b.Process FROM `t_process_code_category_process` a " +
                                                                     "JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) d ON a.ProcessId = d.Id " +
                                                                     "WHERE a.MarkedDelete = 0 AND FlowCardId IN @flowCardIds AND IsDeal = FALSE;", new { flowCardIds });
        }
        /// <summary>
        /// 通过流程卡id获取所有问题
        /// </summary>
        /// <param name="flowCardIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessFault> GetAllSmartProcessFaultsByFlowCardIds(IEnumerable<int> flowCardIds)
        {
            return ServerConfig.ApiDb.Query<SmartProcessFault>("SELECT * FROM `t_process_fault` WHERE MarkedDelete = 0 AND FlowCardId IN @flowCardIds;", new { flowCardIds });
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