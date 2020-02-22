using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.RepairManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;

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
        public DataResult GetRepairRecord([FromQuery]DateTime startTime, DateTime endTime, string code)
        {
            string sql;
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 ORDER BY a.SolveTime DESC;";
            }
            else if (!code.IsNullOrEmpty())
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 AND a.DeviceCode = @code ORDER BY a.SolveTime DESC;";
            }
            else
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 AND a.SolveTime >= @startTime AND a.SolveTime <= @endTime ORDER BY a.SolveTime DESC;";
            }
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<RepairRecordDetail>(sql, new { startTime, endTime, code }).OrderByDescending(x => x.SolveTime));
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        // GET: api/RepairRecord/DeleteLog
        [HttpGet("DeleteLog")]
        public DataResult GetRepairRecordDeleteLog([FromQuery]DateTime startTime, DateTime endTime)
        {
            string sql;
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            if (startTime == default(DateTime) || endTime == default(DateTime))
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 1 AND a.Cancel = 1;";
            }
            else
            {
                sql =
                    $"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 1 AND a.Cancel = 1 AND a.MarkedDateTime >= @startTime AND a.MarkedDateTime <= @endTime;";
            }
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<RepairRecordDetail>(sql, new { startTime, endTime }).OrderByDescending(x => x.SolveTime);
            result.datas.AddRange(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/RepairRecord/5
        [HttpGet("{id}")]
        public DataResult GetRepairRecord([FromRoute] int id)
        {
            var result = new DataResult();
            var field = RepairRecord.GetField(new List<string> { "DeviceCode" }, "a.");
            var data =
                ServerConfig.ApiDb.Query<RepairRecordDetail>($"SELECT a.*, b.FaultTypeName FROM (SELECT {field}, IFNULL(b.`Code`, a.DeviceCode) DeviceCode FROM `repair_record` a LEFT JOIN `device_library` b ON a.DeviceId = b.Id) a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.RepairRecordNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="repairRecord"></param>
        /// <returns></returns>
        // PUT: api/RepairRecord/Id/5
        [HttpPut("{id}")]
        public Result PutRepairRecord([FromRoute] int id, [FromBody] RepairRecord repairRecord)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `repair_record` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            repairRecord.Id = id;
            repairRecord.CreateUserId = Request.GetIdentityInformation();
            repairRecord.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE repair_record SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`DeviceId` = @DeviceId, `DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority, " +
                "`FaultSolver` = @FaultSolver, `SolveTime` = @SolveTime, `SolvePlan` = @SolvePlan, `FaultTypeId` = @FaultTypeId WHERE `Id` = @Id;", repairRecord);

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
            cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id AND MarkedDelete = 0;", new { id = repairRecord.FaultTypeId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }
            repairRecord.CreateUserId = Request.GetIdentityInformation();
            repairRecord.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO repair_record (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `FaultSolver`, `SolveTime`, `SolvePlan`, `FaultTypeId`, `FaultTypeId1`, `FaultLogId`, `IsAdd`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @FaultSolver, @SolveTime, @SolvePlan, @FaultTypeId, @FaultTypeId1, @FaultLogId, @IsAdd);",
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
                ServerConfig.ApiDb.Query<RepairRecord>("SELECT * FROM `repair_record` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `repair_record` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `Cancel`= @Cancel WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Cancel = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `fault_device` SET `MarkedDateTime`= @MarkedDateTime, `Cancel`= @Cancel WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    Cancel = true,
                    Id = data.FaultLogId
                });
            AnalysisHelper.FaultCal(data.FaultTime);
            AnalysisHelper.FaultCal(data.SolveTime);
            return Result.GenError<Result>(Error.Success);
        }
    }
}