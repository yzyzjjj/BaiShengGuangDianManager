using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorHelper : DataHelper
    {
        private SmartOperatorHelper()
        {
            Table = "t_operator";
            InsertSql =
                "INSERT INTO `t_operator` (`CreateUserId`, `MarkedDateTime`, `UserId`, `State`, `ProcessId`, `LevelId`, `Remark`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @UserId, @State, @ProcessId, @LevelId, @Remark, @Priority);";
            UpdateSql = "UPDATE `t_operator` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `ProcessId` = @ProcessId, `LevelId` = @LevelId, `Remark` = @Remark, `Priority` = @Priority WHERE `Id` = @Id;";
        }
        public static readonly SmartOperatorHelper Instance = new SmartOperatorHelper();
        #region Get
        public static string GetSmartOperatorNameById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT b.`Name` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
        }
        public static string GetSmartOperatorAccountById(int id)
        {
            return ServerConfig.ApiDb.Query<string>("SELECT b.`Account` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
        }
        public static IEnumerable<SmartOperatorCount> GetOperatorCount(IEnumerable<int> processIds)
        {
            return ServerConfig.ApiDb.Query<SmartOperatorCount>(
                "SELECT a.ProcessId, LevelId, COUNT(1) Count FROM `t_operator` a " +
                "JOIN `t_process_code_category_process` b ON a.ProcessId = b.ProcessId WHERE b.Id IN @processIds AND a.MarkedDelete = 0 GROUP BY a.ProcessId, LevelId", new
                {
                    processIds
                });
        }
        /// <summary>
        /// 获取所有员工
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SmartOperator> GetAllSmartOperators()
        {
            return ServerConfig.ApiDb.Query<SmartOperator>(
                "SELECT a.*, b.`Name` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id;");
        }
        /// <summary>
        /// 获取状态正常的员工
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SmartOperatorDetail> GetNormalSmartOperators()
        {
            return ServerConfig.ApiDb.Query<SmartOperatorDetail>(
                "SELECT a.*, b.`Name`, c.`Level`, c.`Order` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id JOIN `t_operator_level` c ON a.LevelId = c.Id WHERE a.`MarkedDelete` = 0 AND a.State = @state;"
                    , new { state = SmartOperatorState.正常 });
        }
        public static IEnumerable<SmartOperatorCount> GetNormalOperatorCount(IEnumerable<int> processIds)
        {
            return ServerConfig.ApiDb.Query<SmartOperatorCount>(
                "SELECT a.ProcessId, LevelId, COUNT(1) Count FROM `t_operator` a " +
                "JOIN `t_process_code_category_process` b ON a.ProcessId = b.ProcessId WHERE b.Id IN @processIds AND a.MarkedDelete = 0 AND a.State = @state GROUP BY a.ProcessId, LevelId", new
                {
                    processIds,
                    state = SmartOperatorState.正常
                });
        }

        public static IEnumerable<SmartOperatorCount> GetOperatorCount(int processId)
        {
            return ServerConfig.ApiDb.Query<SmartOperatorCount>(
                "SELECT a.ProcessId, LevelId, COUNT(1) Count FROM `t_operator` a " +
                "JOIN `t_process_code_category_process` b ON a.ProcessId = b.ProcessId WHERE b.Id = @processId AND a.MarkedDelete = 0 GROUP BY a.ProcessId, LevelId", new
                {
                    processId
                });
        }
        public static IEnumerable<SmartOperatorCount> GetNormalOperatorCount(int processId)
        {
            return ServerConfig.ApiDb.Query<SmartOperatorCount>(
                "SELECT a.ProcessId, LevelId, COUNT(1) Count FROM `t_operator` a " +
                "JOIN `t_process_code_category_process` b ON a.ProcessId = b.ProcessId WHERE b.Id = @processId AND a.MarkedDelete = 0 AND a.State = @state GROUP BY a.ProcessId, LevelId", new
                {
                    processId,
                    state = SmartOperatorState.正常
                });
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
