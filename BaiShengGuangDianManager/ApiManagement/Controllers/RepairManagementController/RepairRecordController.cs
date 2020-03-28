using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
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
            string code, DateTime fStartTime, DateTime fEndTime, int faultType, int priority, int state, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId)
        {
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account ";
            sql += $"WHERE a.MarkedDelete = 0 AND `State` = @fState " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : " AND a.DeviceCode = @code")}" +
                   $"{(qId == 0 ? "" : " AND a.Id = @qId")};";

            sql += $"WHERE a.MarkedDelete = 0 AND `State` = @fState " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{((fStartTime == default(DateTime) || fEndTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(state == -1 ? "" : (" AND a.State " + (condition == 0 ? "=" : "!=") + " @state"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND a.Maintainer " + (condition == 0 ? "=" : "!=") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                    new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, fStartTime, fEndTime, faultType, priority, state, maintainer, eStartTime, eEndTime, qId })
                .OrderByDescending(x => x.SolveTime).ThenByDescending(x => x.DeviceCode));
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
            string code, DateTime fStartTime, DateTime fEndTime, int faultType, int priority, int state, string maintainer, DateTime eStartTime, DateTime eEndTime, int qId)
        {
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account ";

            sql += $"WHERE a.MarkedDelete = 0 AND `State` = @fState AND a.Cancel = 1 " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime")}" +
                   $"{(code.IsNullOrEmpty() ? "" : (" AND a.DeviceCode " + (condition == 0 ? "=" : "!=") + " @code"))}" +
                   $"{((fStartTime == default(DateTime) || fEndTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(faultType == -1 ? "" : (" AND a.FaultTypeId " + (condition == 0 ? "=" : "!=") + " @faultType"))}" +
                   $"{(priority == -1 ? "" : (" AND a.Priority " + (condition == 0 ? "=" : "!=") + " @priority"))}" +
                   $"{(state == -1 ? "" : (" AND a.State " + (condition == 0 ? "=" : "!=") + " @state"))}" +
                   $"{(maintainer.IsNullOrEmpty() ? "" : (" AND a.Maintainer " + (condition == 0 ? "=" : "!=") + " @maintainer"))}" +
                   $"{((eStartTime == default(DateTime) || eEndTime == default(DateTime)) ? "" : " AND a.EstimatedTime >= @eStartTime AND a.EstimatedTime <= @eEndTime")}" +
                   $"{(qId == 0 ? "" : (" AND a.Id " + (condition == 0 ? "=" : "!=") + " @qId"))}";
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<RepairRecordDetail>(sql,
                    new { fState = RepairStateEnum.Complete, startTime, endTime, condition, code, fStartTime, fEndTime, faultType, priority, state, maintainer, eStartTime, eEndTime, qId })
                .OrderByDescending(x => x.SolveTime).ThenByDescending(x => x.DeviceCode));
            return result;
        }

        /// <summary>
        /// 获取故障记录
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="state"></param>
        /// <param name="qId"></param>
        /// <returns></returns>
        // GET: api/RepairRecord
        [HttpGet("ReportLog")]
        public DataResult GetReportLog([FromQuery]DateTime startTime, DateTime endTime, int state, int qId)
        {
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            var sql =
                $"SELECT a.*, b.FaultTypeName, IFNULL(c.`Name`, '') `Name`, IFNULL(c.`Account`, '') `Account`, IFNULL(c.`Phone`, '') `Phone` FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `fault_device_repair` a " +
                $"LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id " +
                $"LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete) a GROUP BY a.Account) c ON a.Maintainer = c.Account ";
            sql += $"WHERE a.MarkedDelete = 0 " +
                   $"{((startTime == default(DateTime) || endTime == default(DateTime)) ? "" : " AND a.FaultTime >= @startTime AND a.FaultTime <= @endTime")}" +
                   $"{(state == -1 ? "" : " AND `State` = @state")}" +
                   $"{(qId == 0 ? "" : " AND a.Id = @qId")};";

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<RepairRecordDetail>(sql, new { startTime, endTime, state, qId })
                .OrderByDescending(x => x.FaultTime));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.RepairRecordNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 维修评分
        /// </summary>
        /// <param name="repairRecord"></param>
        /// <returns></returns>
        // PUT: api/RepairRecord/Id/5
        [HttpPut("Score")]
        public Result PutRepairRecord([FromBody] RepairRecord repairRecord)
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
                "UPDATE fault_device_repair SET `Score` = @Score, `Comment` = @Comment WHERE `Id` = @Id;", repairRecord);

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
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_device_repair` WHERE Id IN @id AND `State` = @state AND MarkedDelete = 0;",
                    new { id = repairRecords.Select(x => x.Id), state = RepairStateEnum.Complete }).FirstOrDefault();
            if (cnt != repairRecords.Count())
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE fault_device_repair SET `MarkedDelete` = @MarkedDelete, `DeviceId` = @DeviceId, `DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority, " +
                "`FaultSolver` = @FaultSolver, `SolveTime` = @SolveTime, `SolvePlan` = @SolvePlan, `FaultTypeId` = @FaultTypeId, `FaultTypeId` = @FaultTypeId, `Maintainer` = @Maintainer, `Score` = @Score, `Comment` = @Comment " +
                "WHERE `Id` = @Id;", repairRecords);

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
            cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `maintainer` WHERE Account = @Account AND `MarkedDelete` = 0;", new { Account = createUserId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaintainerNotExist);
            }

            var device = ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT * FROM `device_library` WHERE Id = @DeviceId AND MarkedDelete = 0;;",
                new { DeviceId = repairRecord.DeviceId }).FirstOrDefault();

            var time = DateTime.Now;
            repairRecord.CreateUserId = createUserId;
            repairRecord.MarkedDateTime = time;
            repairRecord.Administrator = device?.Administrator ?? "";
            repairRecord.Maintainer = createUserId;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO fault_device_repair (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `FaultSolver`, `SolveTime`, `SolvePlan`, `FaultTypeId`, `FaultTypeId1`, `IsAdd`, `Administrator`, `Maintainer`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @FaultSolver, @SolveTime, @SolvePlan, @FaultTypeId, @FaultTypeId1, @IsAdd, @Administrator, @Maintainer);",
                repairRecord);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/RepairRecord/Id/5
        [HttpDelete("{id}")]
        public Result DeleteRepairRecord([FromRoute] int id)
        {
            var data =
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `fault_device_repair` WHERE Id = @id AND `State` = @state AND MarkedDelete = 0;",
                    new { id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device_repair` SET `MarkedDelete`= @MarkedDelete, `Cancel`= @Cancel WHERE `Id`= @Id;", new
                {
                    MarkedDelete = true,
                    Cancel = true,
                    Id = id
                });

            AnalysisHelper.FaultCal(data.FaultTime);
            AnalysisHelper.FaultCal(data.SolveTime);
            return Result.GenError<Result>(Error.Success);
        }

    }
}