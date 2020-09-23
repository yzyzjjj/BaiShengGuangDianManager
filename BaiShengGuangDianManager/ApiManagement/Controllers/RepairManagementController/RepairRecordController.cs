using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.RepairManagementController
{
    /// <summary>
    /// 故障记录
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class RepairRecordController : ControllerBase
    {
        // GET: api/RepairRecord
        [HttpGet]
        public DataResult GetRepairRecord([FromQuery]DateTime startTime, DateTime endTime, int condition,
            string code, DateTime fStartTime, DateTime fEndTime, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId, int faultType = -1, int priority = -1, int grade = -1)
        {
            if (!maintainer.IsNullOrEmpty())
            {
                maintainer = $"%{maintainer},%";
            }
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            //var sql =
            //    $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM `fault_device_repair` a " +
            //    $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
            //    $"LEFT JOIN (SELECT * FROM (SELECT * FROM maintainer ORDER BY MarkedDelete ) a GROUP BY a.Account ) c ON a.Maintainer = c.Account " +
            //    $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";
            var sql =
                $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName FROM `fault_device_repair` a " +
                $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";

            sql += $" WHERE a.MarkedDelete = 0 AND `State` = @fState " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{((fStartTime == default(DateTime) || fEndTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(grade == -1 ? "" : (" AND a.Grade " + (condition == 0 ? "=" : "!=") + " @grade"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND CONCAT(a.FaultSolver, \",\") " + (condition == 0 ? "LIKE " : " NOT LIKE ") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";
            var faults = ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, fStartTime, fEndTime, faultType, priority, grade, maintainer, eStartTime, eEndTime, qId });
            var maintainers = ServerConfig.ApiDb.Query<Maintainer>("SELECT * FROM `maintainer` WHERE `MarkedDelete` = 0;").ToArray();
            foreach (var fault in faults)
            {
                var mans = maintainers.Where(x => fault.Maintainers.Any(y => y == x.Account));
                fault.Name = mans.Select(x => x.Name).Join() ?? "";
                fault.Account = mans.Select(x => x.Account).Join() ?? "";
                fault.Phone = mans.Select(x => x.Phone).Join() ?? "";
            }
            var result = new DataResult();
            result.datas.AddRange(faults.OrderByDescending(x => x.SolveTime).ThenByDescending(x => x.DeviceCode));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.RepairRecordNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        // GET: api/RepairRecord/DeleteLog
        [HttpGet("DeleteLog")]
        public DataResult GetRepairRecordDeleteLog([FromQuery]DateTime startTime, DateTime endTime, int condition,
            string code, DateTime fStartTime, DateTime fEndTime, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId, int faultType = -1, int priority = -1, int grade = -1)
        {
            if (!maintainer.IsNullOrEmpty())
            {
                maintainer = $"%{maintainer},%";
            }
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            //var sql =
            //    $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM `fault_device_repair` a " +
            //    $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
            //    $"LEFT JOIN (SELECT * FROM (SELECT * FROM maintainer ORDER BY MarkedDelete ) a GROUP BY a.Account ) c ON a.Maintainer = c.Account " +
            //    $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";
            var sql =
                $"SELECT {field}, IFNULL(d.`Code`, a.DeviceCode) DeviceCode, b.FaultTypeName FROM `fault_device_repair` a " +
                $"JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN `device_library` d ON a.DeviceId = d.Id";

            sql += $" WHERE a.MarkedDelete = 1 AND `State` = @fState AND a.Cancel = 1 " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{((fStartTime == default(DateTime) || fEndTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(grade == -1 ? "" : (" AND a.Grade " + (condition == 0 ? "=" : "!=") + " @grade"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND CONCAT(a.Maintainer, \",\") " + (condition == 0 ? "LIKE " : " NOT LIKE ") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";
            var faults = ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, fStartTime, fEndTime, faultType, priority, grade, maintainer, eStartTime, eEndTime, qId });
            var maintainers = ServerConfig.ApiDb.Query<Maintainer>("SELECT * FROM `maintainer` WHERE `MarkedDelete` = 0;").ToArray();
            foreach (var fault in faults)
            {
                var mans = maintainers.Where(x => fault.Maintainers.Any(y => y == x.Account));
                fault.Name = mans.Select(x => x.Name).Join() ?? "";
                fault.Account = mans.Select(x => x.Account).Join() ?? "";
                fault.Phone = mans.Select(x => x.Phone).Join() ?? "";
            }
            var result = new DataResult();
            result.datas.AddRange(faults.OrderByDescending(x => x.SolveTime).ThenByDescending(x => x.DeviceCode));
            return result;
        }

        /// <summary>
        /// 获取故障记录
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="state"></param>
        /// <param name="account"></param>
        /// <param name="qId"></param>
        /// <returns></returns>
        // GET: api/RepairRecord
        [HttpGet("ReportLog")]
        public DataResult GetReportLog([FromQuery]DateTime startTime, DateTime endTime, string account, int qId, int state = -1)
        {
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, b.FaultDescription Fault1, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone`, IFNULL(d.`FaultTypeName`, '') `FaultTypeName1`, IFNULL(d.`FaultDescription`, '') `Fault2` " +
                $"FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM (SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account " +
                $"LEFT JOIN `fault_type` d ON a.FaultTypeId1 = d.Id ";

            sql += $" WHERE a.MarkedDelete = 0 " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(state == -1 ? "" : " AND `State` = @state")}" +
                   $"{(account.IsNullOrEmpty() ? "" : " AND `Proposer` = @account")}" +
                   $"{(qId == 0 ? "" : " AND a.Id = @qId")};";

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<RepairRecordDetail>(sql, new { startTime, endTime, state, account, qId })
                .OrderByDescending(x => x.FaultTime));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.RepairRecordNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 获取评分排行
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        // GET: api/ScoreRank
        [HttpGet("ScoreRank")]
        public DataResult GetScoreRank([FromQuery]DateTime startTime, DateTime endTime)
        {
            var maintainers = ServerConfig.ApiDb.Query<MaintainerScore>("SELECT * FROM `maintainer`;");
            var scores = maintainers.Where(x => !x.MarkedDelete).ToList();
            var logs = ServerConfig.ApiDb.Query<RepairRecord>(
                "SELECT FaultSolver, Score FROM fault_device_repair WHERE `State` = @state AND MarkedDelete = 0 AND SolveTime >= @startTime AND SolveTime <= @endTime",
                new { startTime, endTime, state = RepairStateEnum.Complete });
            foreach (var log in logs)
            {
                for (var i = 0; i < log.FaultSolvers.Count; i++)
                {
                    var name = log.FaultSolvers[i];
                    if (scores.Any(x => x.Name == name))
                    {
                        scores.First(x => x.Name == name).Score += log.Scores.Count > i ? log.Scores[i] : 0;
                    }
                    else if (maintainers.Any(x => x.Name == name))
                    {
                        var maintainer = maintainers.First(x => x.Name == name);
                        scores.Add(new MaintainerScore
                        {
                            Id = maintainer.Id,
                            Name = maintainer.Name,
                            Account = maintainer.Account,
                            Score = log.Scores.Count < i ? log.Scores[i] : 0
                        });
                    }
                }
            }
            //var sql =
            //    $" SELECT a.Id, a.`Name`, a.Account, IFNULL(b.Score, 0) Score FROM maintainer a  LEFT JOIN (SELECT FaultSolver, SUM(Score) Score FROM fault_device_repair " +
            //    $"WHERE `State` = @state AND MarkedDelete = 0 GROUP BY FaultSolver) b ON a.`Name`= b.FaultSolver WHERE MarkedDelete = 0 ORDER BY b.Score DESC;";
            var result = new DataResult();
            result.datas.AddRange(scores.OrderByDescending(x => x.Score).Select(y => new
            {
                y.Id,
                y.Name,
                y.Account,
                y.Score
            }));
            return result;
        }

        /// <summary>
        /// 维修评价
        /// </summary>
        /// <param name="repairRecord"></param>
        /// <returns></returns>
        // PUT: api/RepairRecord/Id/5
        [HttpPut("Comment")]
        public Result PutRepairRecordComment([FromBody] RepairRecord repairRecord)
        {
            var record =
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `fault_device_repair` WHERE Id = @id AND MarkedDelete = 0;", new { id = repairRecord.Id }).FirstOrDefault();
            if (record == null)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            if (record.State != RepairStateEnum.Complete)
            {
                return Result.GenError<Result>(Error.RepairRecordNotComplete);
            }

            repairRecord.Comment = repairRecord.Comment ?? "";
            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `Comment` = @Comment WHERE `Id` = @Id;", repairRecord);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 维修评分
        /// </summary>
        /// <param name="repairRecords"></param>
        /// <returns></returns>
        // PUT: api/RepairRecord/Id/5
        [HttpPut("Score")]
        public Result PutRepairRecordScore([FromBody] IEnumerable<RepairRecord> repairRecords)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device_repair` WHERE Id IN @id AND `State` = @state AND MarkedDelete = 0;",
                    new { id = repairRecords.Select(x => x.Id), state = RepairStateEnum.Complete }).FirstOrDefault();
            if (cnt != repairRecords.Count())
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `Score` = @Score WHERE `Id` = @Id;", repairRecords);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        /// <returns></returns>
        // PUT: api/RepairRecord/Id/5
        [HttpPut]
        public Result PutRepairRecord([FromBody] IEnumerable<RepairRecord> repairRecords)
        {
            var data =
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` = @state AND MarkedDelete = 0;",
                    new { id = repairRecords.Select(x => x.Id), state = RepairStateEnum.Complete });
            if (data.Count() != repairRecords.Count())
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            var now = DateTime.Now;
            foreach (var record in repairRecords)
            {
                var old = data.First(x => x.Id == record.Id);
                if (record.FaultSolver != old.FaultSolver)
                {
                    var faultSolvers = new List<string>();
                    faultSolvers.AddRange(record.FaultSolvers.Where(x => old.FaultSolvers.Contains(x)));
                    var scores = faultSolvers.Select(solver => old.FaultSolvers.IndexOf(solver)).Select(index => old.Scores[index]).ToList();
                    faultSolvers.AddRange(record.FaultSolvers.Where(x => !old.FaultSolvers.Contains(x)));
                    record.FaultSolver = faultSolvers.Join();
                    record.Score = scores.Join();
                }

                record.MarkedDateTime = now;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `MarkedDateTime` = @MarkedDateTime, `FaultSolver` = @FaultSolver, `Score` = @Score, `SolveTime` = @SolveTime, `SolvePlan` = @SolvePlan, `FaultTypeId1` = @FaultTypeId1 WHERE `Id` = @Id;", repairRecords);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RepairRecord
        [HttpPost]
        public Result PostRepairRecord([FromBody] RepairRecord repairRecord)
        {
            int cnt;
            if (repairRecord.DeviceId == 0)
            {
                cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Code` = @Code AND `MarkedDelete` = 0;", new { Code = repairRecord.DeviceCode }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.ReportDeviceCodeIsExist);
                }
            }

            cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id AND MarkedDelete = 0;", new { id = repairRecord.FaultTypeId1 }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            if (!repairRecord.Maintainer.IsNullOrEmpty())
            {
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Account = @Account AND `MarkedDelete` = 0;", new { Account = repairRecord.Maintainer }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<Result>(Error.MaintainerNotExist);
                }
            }

            var device = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT * FROM `device_library` WHERE Id = @DeviceId AND MarkedDelete = 0;;",
                new { DeviceId = repairRecord.DeviceId }).FirstOrDefault();

            repairRecord.CreateUserId = createUserId;
            repairRecord.Administrator = device?.Administrator ?? "";
            repairRecord.DeviceCode = device?.Code ?? repairRecord.DeviceCode;
            repairRecord.Maintainer = repairRecord.Maintainer ?? "";
            repairRecord.State = RepairStateEnum.Complete;
            repairRecord.IsAdd = true;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_device_repair (`CreateUserId`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `Grade`, `State`, `FaultSolver`, `SolveTime`, `SolvePlan`, `FaultTypeId`, `FaultTypeId1`, `IsAdd`, `Administrator`, `Maintainer`) " +
                "VALUES (@CreateUserId, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @Grade, @State, @FaultSolver, @SolveTime, @SolvePlan, @FaultTypeId, @FaultTypeId1, @IsAdd, @Administrator, @Maintainer);",
                repairRecord);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/RepairRecord/Id/5
        [HttpDelete]
        public Result DeleteRepairRecord([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var data =
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `fault_device_repair` WHERE Id IN @id AND `State` = @state AND MarkedDelete = 0;",
                    new { id = ids, state = RepairStateEnum.Complete });
            if (data.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device_repair` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Cancel`= @Cancel WHERE `Id`IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Cancel = true,
                    Id = ids
                });
            foreach (var d in data.GroupBy(x => x.FaultTime).Select(x => x.Key))
            {
                AnalysisHelper.FaultCal(d);
            }

            foreach (var d in data.GroupBy(x => x.SolveTime).Select(x => x.Key))
            {
                AnalysisHelper.FaultCal(d);
            }

            return Result.GenError<Result>(Error.Success);
        }

    }
}