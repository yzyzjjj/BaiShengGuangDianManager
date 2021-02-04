using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

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
                "UPDATE `t_process_code_category_process` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ProcessCodeCategoryId` = @ProcessCodeCategoryId, `ProcessId` = @ProcessId, `Order` = @Order WHERE `Id` = @Id;";
        }
        public static readonly SmartProcessCodeCategoryProcessHelper Instance = new SmartProcessCodeCategoryProcessHelper();
        #region Get





        public static IEnumerable<SmartProcessCodeCategoryProcessDetail> GetDetail(int id = 0, int cId = 0)
        {
            return ServerConfig.ApiDb.Query<SmartProcessCodeCategoryProcessDetail>($"SELECT a.*, b.Process, b.Remark FROM `t_process_code_category_process` a " +
                                                                                   $"JOIN `t_process` b ON a.ProcessId = b.Id " +
                                                                                   $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}" +
                                                                                   $"{(cId == 0 ? "" : "ProcessCodeCategoryId = @cId AND ")}" +
                                                                                   $"a.MarkedDelete = 0 AND b.MarkedDelete = 0" +
                                                                                   $" ORDER BY `Order`;", new { id, cId });
        }
        public static dynamic GetDetailByProcessId(int pId)
        {
            return ServerConfig.ApiDb.Query<dynamic>(
                "SELECT a.Id, b.DeviceCategoryId, b.Id ProcessId FROM `t_process_code_category_process` a " +
                "JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.Id = @pId AND a.MarkedDelete = 0;", new
                {
                    pId
                }).FirstOrDefault();
        }
        /// <summary>
        /// 通过流程编号类型获取标准流程
        /// </summary>
        /// <param name="cId"></param>
        /// <returns></returns>
        public static IEnumerable<SmartProcessCodeCategoryProcess> GetDetailByCategoryId(int cId)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ProcessCodeCategoryId", "=", cId)
            };
            return Instance.CommonGet<SmartProcessCodeCategoryProcess>(args);
        }
        /// <summary>
        /// 通过流程编号类型获取标准流程
        /// </summary>
        /// <param name="cIds"></param>
        /// <returns></returns>
        public static IEnumerable<SmartProcessCodeCategoryProcess> GetDetailByCategoryId(IEnumerable<int> cIds)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ProcessCodeCategoryId", "IN", cIds)
            };
            return Instance.CommonGet<SmartProcessCodeCategoryProcess>(args);
        }
        public static IEnumerable<SmartProcess> GetProcess(IEnumerable<int> ids)
        {
            return ServerConfig.ApiDb.Query<SmartProcess>($"SELECT a.*, b.Process, b.Remark FROM `t_process_code_category_process` a " +
                                                           $"JOIN `t_process` b ON a.ProcessId = b.Id " +
                                                           $"WHERE {(ids == null ? "" : "a.Id IN @ids AND ")}" +
                                                           $"a.MarkedDelete = 0 AND b.MarkedDelete = 0" +
                                                           $" ORDER BY `Order`;", new { ids }); ;
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