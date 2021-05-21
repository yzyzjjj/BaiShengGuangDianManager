using ApiManagement.Base.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class StatisticProcessHelper : DataHelper
    {
        private StatisticProcessHelper()
        {
            Table = "npc_monitoring_process_log";
            //InsertSql =
            //    "INSERT INTO  `npc_monitoring_process_log` (`Id`, `OpName`, `DeviceId`, `StartTime`, `EndTime`, `FlowCardId`, `FlowCard`, `ProcessorId`, `Processor`, `ProcessData`, `RequirementMid`, `ActualThickness`) " +
            //    "VALUES (@Id, @OpName, @DeviceId, @StartTime, @EndTime, @FlowCardId, @FlowCard, @ProcessorId, @Processor, @ProcessData, @RequirementMid, @ActualThickness);";
            InsertSql =
                "INSERT INTO `npc_monitoring_process_log` (`Id`, `ProcessType`, `OpName`, `DeviceId`, `StartTime`, `EndTime`, `FlowCardId`, `FlowCard`, `ProcessorId`, `ProcessData`, `RequirementMid`, `ActualThickness`) " +
                "VALUES (@Id, @ProcessType, @OpName, @DeviceId, @StartTime, IF(@EndTime = '0001-01-01 00:00:00', NULL, @EndTime), @FlowCardId, @FlowCard, @ProcessorId, @ProcessData, @RequirementMid, @ActualThickness);";
            UpdateSql = "UPDATE `npc_monitoring_process_log` SET `EndTime` = @EndTime WHERE `Id` = @Id;";

            SameField = "OpName";
            MenuFields.AddRange(new[] { "Id", "ProcessType", "OpName", "DeviceId", "StartTime", "EndTime" });
        }
        public static readonly StatisticProcessHelper Instance = new StatisticProcessHelper();
        #region Get
        ///// <summary>
        ///// 菜单
        ///// </summary>
        ///// <param name="sId">脚本</param>
        ///// <param name="vId">常用变量类型id</param>
        ///// <param name="vType">1 变量 2输入口 3输出口</param>
        ///// <returns></returns>
        //public static IEnumerable<dynamic> GetMenu(int sId = -1, int vId = 0, int vType = 0)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>();
        //    if (sId != -1)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("ScriptId", "=", sId));
        //    }
        //    if (vId != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("VariableNameId", "=", vId));
        //    }
        //    if (vType != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("DictionaryId", "=", vType));
        //    }

        //    return Instance.CommonGet<StatisticProcess>(args, true).Select(x => new { x.Id, x.ScriptId, x.VariableNameId, x.DictionaryId, x.VariableTypeId });
        //}
        //public static IEnumerable<StatisticProcessDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        //{
        //    return ServerConfig.ApiDb.Query<StatisticProcessDetail>(
        //        $"SELECT a.*, b.`Category` FROM `usually_dictionary` a JOIN `t_device_category` b ON a.CategoryId = b.Id " +
        //        $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId;",
        //        new { id, cId, wId });
        //}
        //public static bool GetHaveSame(int cId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>
        //    {
        //        new Tuple<string, string, dynamic>("CategoryId", "=", cId),
        //        new Tuple<string, string, dynamic>("Model", "IN", sames)
        //    };
        //    if (ids != null)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
        //    }
        //    return Instance.CommonHaveSame(args);
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessFlag> GetDistinctProcessLogs(bool valid)
        {
            return ServerConfig.ApiDb.Query<StatisticProcessFlag>(
                valid ? "SELECT * FROM (SELECT * FROM (SELECT * FROM `npc_monitoring_process_log` ORDER BY Id DESC) a GROUP BY a.DeviceId) a WHERE ISNULL(EndTime);"
                    : "SELECT * FROM (SELECT * FROM `npc_monitoring_process_log` ORDER BY Id DESC) a GROUP BY a.DeviceId;", null, 1000);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcess> GetProcessLogs(IEnumerable<int> ids = null)
        {
            return Instance.GetByIds<StatisticProcess>(ids,
                $"SELECT * FROM `npc_monitoring_process_log`{(ids != null && ids.Any() ? " Where Id IN @ids" : "")};");
            //return ServerConfig.ApiDb.Query<StatisticProcess>($"SELECT * FROM `npc_monitoring_process_log`{(ids != null && ids.Any() ? " Where Id IN @ids" : "")};", new { ids });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<StatisticProcessFlag> GetProcessLogFlags(IEnumerable<int> ids = null)
        {
            return Instance.GetByIds<StatisticProcessFlag>(ids,
                $"SELECT * FROM `npc_monitoring_process_log`{(ids != null && ids.Any() ? " Where Id IN @ids" : "")};");
            //return ServerConfig.ApiDb.Query<StatisticProcess>($"SELECT * FROM `npc_monitoring_process_log`{(ids != null && ids.Any() ? " Where Id IN @ids" : "")};", new { ids });
        }
        #endregion

        #region Add

        public void Add(IEnumerable<StatisticProcess>)
        {

        }
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}