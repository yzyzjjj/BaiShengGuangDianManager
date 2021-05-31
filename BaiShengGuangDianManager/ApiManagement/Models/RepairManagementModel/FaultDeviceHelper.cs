using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.RepairManagementModel
{
    public class FaultDeviceHelper : DataHelper
    {
        private FaultDeviceHelper()
        {
            Table = "fault_device_repair";
            InsertSql =
                "INSERT INTO fault_device_repair (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `Supplement`, `Priority`, `Grade`, `FaultTypeId`, `Administrator`, `Maintainer`, `IsReport`, `Images`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @Supplement, @Priority, @Grade, @FaultTypeId, @Administrator, @Maintainer, @IsReport, @Images);";
            UpdateSql = "UPDATE `fault_device_repair` SET `MarkedDateTime` = @MarkedDateTime, `FaultTime` = @FaultTime, `Proposer` = @Proposer, " +
                        "`FaultDescription` = @FaultDescription, `Priority` = @Priority, `Grade` = @Grade, `Maintainer` = @Maintainer " +
                        "WHERE `Id` = @Id;";

            SameField = "DeviceCode";
            MenuFields.AddRange(new[] { "Id", "DeviceCode" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly FaultDeviceHelper Instance = new FaultDeviceHelper();
        #region Get
        public static IEnumerable<FaultDeviceDetail> GetFaultDeviceDetails(int workshopId, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime),
            int condition = 0, string code = "", DateTime eStartTime = default(DateTime), DateTime eEndTime = default(DateTime), int qId = 0, string maintainer = "-1", int faultType = -1,
            int priority = -1, int grade = -1, int state = -1, int cancel = -1,
            IEnumerable<string> fields = null)
        {
            var field = fields != null && fields.Any()
                ? fields.Select(x => $"a.`{x}`").Join(", ")
                : RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");

            if (!maintainer.IsNullOrEmpty() && maintainer != "-1")
            {
                maintainer = $"%{maintainer},%";
            }

            var delete = cancel == 1 ? 1 : 0;
            var sql =
                $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName, b.FaultDescription FROM `fault_device_repair` a " +
                $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";
            sql += $" WHERE `State` != @fState " +
                   $"{(workshopId == -1 ? "" : (" AND a.WorkshopId " + (condition == 0 ? "=" : "!=") + " @workshopId"))}" +
                   $"{(cancel == -1 ? "" : " AND a.Cancel = @cancel")}" +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(grade == -1 ? "" : (" AND a.Grade " + (condition == 0 ? "=" : "!=") + " @grade"))}" +
                   $"{(state == -1 ? "" : (" AND a.State " + (condition == 0 ? "=" : "!=") + " @state"))}" +
                   $"{((maintainer.IsNullOrEmpty() || maintainer == "-1") ? "" : (" AND CONCAT(a.Maintainer, \",\") " + (condition == 0 ? "LIKE " : " NOT LIKE ") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))} AND a.MarkedDelete = @delete";
            var faults = ServerConfig.ApiDb.Query<FaultDeviceDetail>(sql,
                new
                {
                    fState = RepairStateEnum.Complete,
                    workshopId,
                    cancel,
                    startTime,
                    endTime,
                    condition,
                    code,
                    faultType,
                    priority,
                    grade,
                    state,
                    maintainer,
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


        public static IEnumerable<FaultDeviceDetail> GetFaultDeviceDetails(DateTime startTime, DateTime endTime,
            int condition, string code, DateTime eStartTime, DateTime eEndTime, int qId, string maintainer = "-1", int faultType = -1,
            int priority = -1, int grade = -1, int state = -1, int cancel = -1,
            IEnumerable<string> fields = null)
        {
            return GetFaultDeviceDetails(-1, startTime, endTime, condition, code, eStartTime, eEndTime, qId, maintainer, faultType, priority, grade, state, 0, fields);
        }

        public static IEnumerable<FaultDeviceDetail> GetDeleteFaultDeviceDetails(DateTime startTime, DateTime endTime,
            int condition, string code, DateTime eStartTime, DateTime eEndTime, int qId, string maintainer = "-1", int faultType = -1,
            int priority = -1, int grade = -1, int state = -1,
            IEnumerable<string> fields = null)
        {
            return GetFaultDeviceDetails(-1, startTime, endTime, condition, code, eStartTime, eEndTime, qId, maintainer, faultType, priority, grade, state, 1, fields);
        }

        public static IEnumerable<FaultDeviceDetail> GetDetails(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("State", "!=", RepairStateEnum.Complete)
            };
            if (ids != null && ids.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<FaultDeviceDetail>(args);
        }
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        public static void Cancel(IEnumerable<int> ids)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device_repair` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true, `Cancel`= true WHERE `Id`IN @ids;", new{ids});
        }
        #endregion
    }
}