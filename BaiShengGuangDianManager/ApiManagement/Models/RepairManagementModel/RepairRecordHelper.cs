using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.RepairManagementModel
{
    public class RepairRecordHelper : DataHelper
    {
        private RepairRecordHelper()
        {
            Table = "fault_device_repair";
            InsertSql =
                "INSERT INTO fault_device_repair (`CreateUserId`, `MarkedDateTime`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName, @Remark);";
            UpdateSql = "UPDATE `fault_device_repair` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `StartTime` = @StartTime, `EstimatedTime` = @EstimatedTime, `Remark` = @Remark, " +
                        "`FaultSolver` = @FaultSolver, `SolveTime` = @SolveTime, `SolvePlan` = @SolvePlan, `FaultTypeId1` = @FaultTypeId1 " +
                        "WHERE `Id` = @Id;";

            SameField = "DeviceCode";
            MenuFields.AddRange(new[] { "Id", "DeviceCode" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly RepairRecordHelper Instance = new RepairRecordHelper();
        #region Get
        public static IEnumerable<RepairRecordDetail> GetRepairRecordDetails(int workshopId, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime),
            int condition = 0, string code = "", DateTime fStartTime = default(DateTime), DateTime fEndTime = default(DateTime), DateTime eStartTime = default(DateTime), DateTime eEndTime = default(DateTime), int qId = 0,
            string maintainer = "-1", string faultSolver = "-1", int faultType = -1, int priority = -1, int grade = -1, int cancel = -1, IEnumerable<string> fields = null)
        {
            var field = fields != null && fields.Any()
                ? fields.Select(x => $"a.`{x}`").Join(", ")
                : RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");

            if (!maintainer.IsNullOrEmpty() && maintainer != "-1")
            {
                maintainer = $"%{maintainer},%";
            }
            if (!faultSolver.IsNullOrEmpty() && faultSolver != "-1")
            {
                faultSolver = $"%{faultSolver},%";
            }
            var delete = cancel == 1 ? 1 : 0;
            var sql =
                $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName, b.FaultDescription, c.FaultTypeName FaultTypeName1, c.FaultDescription FaultDescription1 FROM `fault_device_repair` a " +
                $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"JOIN `fault_type` c ON a.FaultTypeId1 = c.Id " +
                $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";

            sql += $" WHERE `State` = @fState " +
                   $"{(workshopId == -1 ? "" : (" AND a.WorkshopId " + (condition == 0 ? "=" : "!=") + " @workshopId"))}" +
                   $"{(cancel == -1 ? "" : " AND a.Cancel = @cancel")}" +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{((fStartTime == default(DateTime) || fEndTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(grade == -1 ? "" : (" AND a.Grade " + (condition == 0 ? "=" : "!=") + " @grade"))}" +
                   $"{((maintainer.IsNullOrEmpty() || maintainer == "-1") ? "" : (" AND CONCAT(a.Maintainer, \",\") " + (condition == 0 ? "LIKE " : " NOT LIKE ") + " @maintainer"))}" +
                   $"{((faultSolver.IsNullOrEmpty() || faultSolver == "-1") ? "" : (" AND CONCAT(a.FaultSolver, \",\") " + (condition == 0 ? "LIKE " : " NOT LIKE ") + " @faultSolver"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))} AND a.MarkedDelete = @delete";
            var faults = ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                new
                {
                    fState = RepairStateEnum.Complete,
                    workshopId,
                    cancel,
                    startTime,
                    endTime,
                    condition,
                    code,
                    fStartTime,
                    fEndTime,
                    faultType,
                    priority,
                    grade,
                    maintainer,
                    faultSolver,
                    eStartTime,
                    eEndTime,
                    qId,
                    delete
                });
            var maintainers = MaintainerHelper.Instance.GetAll<Maintainer>().ToArray();
            foreach (var fault in faults)
            {
                var mans = maintainers.Where(x => fault.Maintainers.Any(y => y == x.Account));
                fault.Name = mans.Select(x => x.Name).Join() ?? "";
                fault.Account = mans.Select(x => x.Account).Join() ?? "";
                fault.Phone = mans.Select(x => x.Phone).Join() ?? "";
            }
            return faults;
        }

        public static IEnumerable<RepairRecordDetail> GetRepairRecordDetails(DateTime startTime = default(DateTime), DateTime endTime = default(DateTime),
            int condition = 0, string code = "", DateTime fStartTime = default(DateTime), DateTime fEndTime = default(DateTime), DateTime eStartTime = default(DateTime), DateTime eEndTime = default(DateTime), int qId = 0,
            string maintainer = "-1", string faultSolver = "-1", int faultType = -1, int priority = -1, int grade = -1, IEnumerable<string> fields = null)
        {
            return GetRepairRecordDetails(-1, startTime, endTime, condition, code, fStartTime, fEndTime, eStartTime, eEndTime, qId, maintainer, faultSolver, faultType, priority, grade, 0, fields);
        }

        public static IEnumerable<RepairRecordDetail> GetDeleteRepairRecordDetails(DateTime startTime = default(DateTime), DateTime endTime = default(DateTime),
            int condition = 0, string code = "", DateTime fStartTime = default(DateTime), DateTime fEndTime = default(DateTime), DateTime eStartTime = default(DateTime), DateTime eEndTime = default(DateTime), int qId = 0,
            string maintainer = "-1", string faultSolver = "-1", int faultType = -1, int priority = -1, int grade = -1, IEnumerable<string> fields = null)
        {
            return GetRepairRecordDetails(-1, startTime, endTime, condition, code, fStartTime, fEndTime, eStartTime, eEndTime, qId, maintainer, faultSolver, faultType, priority, grade, 1, fields);
        }

        public static IEnumerable<RepairRecordDetail> GetKanBan(int workshopId = -1, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime), IEnumerable<string> fields = null)
        {
            var field = fields != null && fields.Any()
                ? fields.Select(x => $"a.`{x}`").Join(", ")
                : RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");

            var sql =
                $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, IFNULL(c.FaultTypeName, b.FaultTypeName) FaultTypeName, IFNULL(c.FaultDescription, b.FaultDescription) FaultDescription FROM `fault_device_repair` a " +
                $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN `fault_type` c ON a.FaultTypeId1 = c.Id " +
                $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";

            sql += $" WHERE `State` != @state " +
                   $"{(workshopId == -1 ? "" : (" AND a.WorkshopId = @workshopId"))}" +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")} " +
                   $"AND a.`MarkedDelete` = 0 AND a.`Cancel` = 0;";
            var faults = ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                new
                {
                    state = RepairStateEnum.Complete,
                    workshopId,
                    startTime,
                    endTime,
                });
            var maintainers = MaintainerHelper.Instance.GetAll<Maintainer>().ToArray();
            foreach (var fault in faults)
            {
                var mans = maintainers.Where(x => fault.Maintainers.Any(y => y == x.Account));
                fault.Name = mans.Select(x => x.Name).Join() ?? "";
                fault.Account = mans.Select(x => x.Account).Join() ?? "";
                fault.Phone = mans.Select(x => x.Phone).Join() ?? "";
            }
            return faults;
        }

        #endregion

        #region Add
        #endregion

        #region Update
        public void Repair(IEnumerable<RepairRecord> faults)
        {

        }
        #endregion

        #region Delete
        #endregion
    }
}