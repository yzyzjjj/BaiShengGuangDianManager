using ApiManagement.Base.Server;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartOperatorHelper : DataHelper
    {
        private SmartOperatorHelper()
        {
            Table = "t_operator";
            InsertSql =
                "INSERT INTO `t_operator` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `UserId`, `State`, `ProcessId`, `LevelId`, `Remark`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @UserId, @State, @ProcessId, @LevelId, @Remark, @Priority);";
            UpdateSql = "UPDATE `t_operator` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `ProcessId` = @ProcessId, `LevelId` = @LevelId, `Remark` = @Remark, `Priority` = @Priority WHERE `Id` = @Id;";

            SameField = "UserId";
            MenuFields.AddRange(new[] { "Id", "UserId" });
        }
        public static readonly SmartOperatorHelper Instance = new SmartOperatorHelper();
        #region Get
        /// <summary>
        /// 菜单 add, qId, wId, levelId, processId, number, name, state, condition
        /// </summary>
        /// <param name="add"></param>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <param name="lId">等级ID</param>
        /// <param name="pId">流程ID</param>
        /// <param name="number">编号</param>
        /// <param name="name">姓名</param>
        /// <param name="state">状态</param>
        /// <param name="condition">0 相等 1 包含</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(bool add, int id = 0, int wId = 0, int lId = -1, int pId = -1,
            string number = "", string name = "", SmartOperatorState state = SmartOperatorState.全部, int condition = 0)
        {
            if (condition != 0)
            {
                number = $"%{number}%";
                name = $"%{name}%";
            }
            if (!add)
            {
                return ServerConfig.ApiDb.Query<SmartOperator>(
                "SELECT a.Id, b.`Name`, b.`Account` FROM `t_operator` a " +
                "JOIN `accounts` b ON a.UserId = b.Id " +
                $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}" +
                $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                $"{(lId == -1 ? "" : "a.LevelId = @lId AND ")}" +
                $"{(pId == -1 ? "" : "a.ProcessId = @pId AND ")}" +
                $"{(number.IsNullOrEmpty() ? "" : $"b.Number {(condition == 0 ? "=" : "LIKE")} @number AND ")}" +
                $"{(name.IsNullOrEmpty() ? "" : $"b.Name {(condition == 0 ? "=" : "LIKE")} @name AND ")}" +
                $"{(state == 0 ? "" : "a.State = @state AND ")}" +
                $"a.MarkedDelete = 0 ORDER BY a.ProcessId, a.Priority, a.Id",
                new { id, wId, lId, pId, number, name, state }).Select(x => new { x.Id, x.Name, x.Account });
            }

            return ServerConfig.ApiDb.Query<SmartOperator>(
                "SELECT a.Id, a.`Name`, a.`Account` FROM `accounts` a " +
                "LEFT JOIN (SELECT * FROM `t_operator` WHERE MarkedDelete = 0) b ON a.Id = b.UserId " +
                $"WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY b.ProcessId, b.Priority, a.Id").Select(x => new { x.Id, x.Name, x.Account });
        }

        public static IEnumerable<SmartOperatorDetail> GetDetail(bool add, int id = 0, int wId = 0, int lId = -1, int pId = -1,
            string number = "", string name = "", SmartOperatorState state = SmartOperatorState.全部, int condition = 0)
        {
            if (condition != 0)
            {
                number = $"%{number}%";
                name = $"%{name}%";
            }
            if (!add)
            {
                return ServerConfig.ApiDb.Query<SmartOperatorDetail>(
                    "SELECT a.*, b.`Number`, b.`Name`, b.`Account`, IFNULL(c.Process, '') Process, IFNULL(d.`Level`, '') Level FROM `t_operator` a " +
                    "JOIN `accounts` b ON a.UserId = b.Id " +
                    $"LEFT JOIN `t_process` c ON a.ProcessId = c.Id " +
                    $"LEFT JOIN `t_operator_level` d ON a.LevelId = d.Id " +
                    $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}" +
                    $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                    $"{(lId == -1 ? "" : "a.LevelId = @lId AND ")}" +
                    $"{(pId == -1 ? "" : "a.ProcessId = @pId AND ")}" +
                    $"{(number.IsNullOrEmpty() ? "" : $"b.Number {(condition == 0 ? "=" : "LIKE")} @number AND ")}" +
                    $"{(name.IsNullOrEmpty() ? "" : $"b.Name {(condition == 0 ? "=" : "LIKE")} @name AND ")}" +
                    $"{(state == 0 ? "" : "a.State = @state AND ")}" +
                    $"a.MarkedDelete = 0 ORDER BY a.ProcessId, a.Priority, a.Id",
                    new { id, wId, lId, pId, number, name, state });
            }

            return ServerConfig.ApiDb.Query<SmartOperatorDetail>(
                "SELECT a.* FROM `accounts` a " +
                "LEFT JOIN (SELECT * FROM `t_operator` WHERE MarkedDelete = 0) b ON a.Id = b.UserId " +
                $"WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY b.ProcessId, b.Priority, a.Id");
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("UserId", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
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
        public static IEnumerable<SmartOperatorDetail> GetNormalSmartOperators(int wId)
        {
            return ServerConfig.ApiDb.Query<SmartOperatorDetail>(
                "SELECT a.*, b.`Name`, c.`Level`, c.`Order` FROM `t_operator` a JOIN `t_user` b ON a.UserId = b.Id JOIN `t_operator_level` c ON a.LevelId = c.Id " +
                $"WHERE {(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.State = @state AND a.`MarkedDelete` = 0;"
                , new { wId, state = SmartOperatorState.正常 });
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
