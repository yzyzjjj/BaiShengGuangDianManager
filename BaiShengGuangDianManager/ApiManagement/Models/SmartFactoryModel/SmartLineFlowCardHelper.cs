﻿using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartLineFlowCardHelper : DataHelper
    {
        private SmartLineFlowCardHelper()
        {
        }
        public static readonly SmartLineFlowCardHelper Instance = new SmartLineFlowCardHelper();
        #region Get
        /// <summary>
        /// 通过流程卡id获取流程卡流程详情  ProcessId为计划号流程id
        /// </summary>
        /// <param name="flowCardId"></param>
        /// <returns></returns>
        public IEnumerable<SmartLineFlowCard> GetSmartLineFlowCardsByFlowCardId(int flowCardId)
        {
            return ServerConfig.ApiDb.Query<SmartLineFlowCard>("SELECT a.*, b.Process FROM `t_flow_card_process` a " +
                                                               "JOIN (SELECT a.Id, b.Process FROM `t_product_process` a JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                                                               "WHERE a.MarkedDelete = 0 AND FlowCardId = @flowCardId;", new { flowCardId });
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