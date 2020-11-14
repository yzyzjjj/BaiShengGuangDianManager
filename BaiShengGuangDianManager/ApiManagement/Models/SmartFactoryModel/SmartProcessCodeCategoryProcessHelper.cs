using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProcessCodeCategoryProcessHelper : DataHelper
    {
        private SmartProcessCodeCategoryProcessHelper()
        {
            Table = "t_process_code_category_process";
            InsertSql =
                "INSERT INTO `t_process_code_category_process` (`CreateUserId`, `MarkedDateTime`, `ProcessCodeCategoryId`, `Order`, `ProcessId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ProcessCodeCategoryId, @Order, @ProcessId);";
            UpdateSql =
                "UPDATE `t_process_code_category_process` SET `MarkedDateTime` = @MarkedDateTime, `ProcessCodeCategoryId` = @ProcessCodeCategoryId, `ProcessId` = @ProcessId, `Order` = @Order WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessCodeCategoryProcessHelper Instance = new SmartProcessCodeCategoryProcessHelper();
        #region Get
        /// <summary>
        /// 通过流程编号类型获取标准流程
        /// </summary>
        /// <param name="processCodeCategoryIds"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessCodeCategoryProcess> GetSmartProcessCodeCategoryProcessesByProcessCodeCategoryIds(IEnumerable<int> processCodeCategoryIds)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeCategoryProcess>("SELECT * FROM `t_process_code_category_process` WHERE MarkedDelete = 0 AND ProcessCodeCategoryId IN @processCodeCategoryIds;", new { processCodeCategoryIds });
        }
        /// <summary>
        /// 通过流程编号类型获取标准流程
        /// </summary>
        /// <param name="processCodeCategoryId"></param>
        /// <returns></returns>
        public IEnumerable<SmartProcessCodeCategoryProcess> GetSmartProcessCodeCategoryProcessesByProcessCodeCategoryId(int processCodeCategoryId)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeCategoryProcess>("SELECT * FROM `t_process_code_category_process` WHERE MarkedDelete = 0 AND ProcessCodeCategoryId = @processCodeCategoryId;", new { processCodeCategoryId });
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