using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.StatisticManagementModel
{
    public class MonitoringProcessHelper : DataHelper
    {
        /// <summary>
        ///{0, "npc_monitoring_process_min"},
        ///{1, "npc_monitoring_process_hour"},
        ///{2, "npc_monitoring_process_day"},
        ///{3, "npc_monitoring_process_month"},
        /// </summary>
        public static Dictionary<int, string> Tables = new Dictionary<int, string>
        {
            {0, "npc_monitoring_process_min"},
            {1, "npc_monitoring_process_hour"},
            {2, "npc_monitoring_process_day"},
            {3, "npc_monitoring_process_month"},
        };
        private MonitoringProcessHelper()
        {
            Table = "npc_monitoring_process";
            InsertSql =
                "INSERT INTO `npc_monitoring_process` (`Time`, `DeviceId`, `State`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `RunTime`, `TotalRunTime`, `Use`, `Total`, `Rate`) " +
                "VALUES (@Time, @DeviceId, @State, @ProcessCount, @TotalProcessCount, @ProcessTime, @TotalProcessTime, @RunTime, @TotalRunTime, @Use, @Total, @Rate);";
            UpdateSql = "UPDATE `npc_monitoring_process` SET `State` = @State, `ProcessCount` = @ProcessCount, `TotalProcessCount` = @TotalProcessCount, `ProcessTime` = @ProcessTime, " +
                        "`TotalProcessTime` = @TotalProcessTime, `RunTime` = @RunTime, `TotalRunTime` = @TotalRunTime, `Use` = @Use, `Total` = @Total, `Rate` = @Rate WHERE `Time` = @Time AND `DeviceId` = @DeviceId;";

            SameField = "Time";
            MenuFields.AddRange(new[] { "Id", "Time", "DeviceId" });
        }
        public static readonly MonitoringProcessHelper Instance = new MonitoringProcessHelper();
        #region Get
        public static IEnumerable<MonitoringProcess> GetMonitoringProcesses(IEnumerable<int> ids = null)
        {
            return ServerConfig.ApiDb.Query<MonitoringProcess>(
                $"SELECT b.*, a.`Code` FROM `device_library` a JOIN `npc_proxy_link` b ON a.Id = b.DeviceId " +
                $"WHERE {(ids != null && ids.Any() ? "a.Id IN @ids AND" : "")} a.MarkedDelete = 0;", new { ids });
        }
        /// <summary>
        /// type 0 分  1 小时   2 天   3 月   
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MonitoringProcess> GetMonitoringProcesses(int type, DateTime time, IEnumerable<int> deviceIds = null)
        {
            var r = new List<MonitoringProcess>();
            if (Tables.ContainsKey(type))
            {
                r.AddRange(ServerConfig.ApiDb.Query<MonitoringProcess>(
                    $"SELECT * FROM `{Tables[type]}` WHERE Time = @time" +
                    $"{(deviceIds != null && deviceIds.Any() ? " AND DeviceId IN @deviceIds" : "")}", new { time, deviceIds }));
            }
            return r;
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