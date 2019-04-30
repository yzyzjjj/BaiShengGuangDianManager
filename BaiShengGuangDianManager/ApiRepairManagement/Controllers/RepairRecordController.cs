using ApiRepairManagement.Base.Server;
using ApiRepairManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiRepairManagement.Controllers
{
    /// <summary>
    /// 故障记录
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RepairRecordController : ControllerBase
    {
        // GET: api/RepairRecord
        [HttpGet]
        public DataResult GetRepairRecord()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.RepairDb.Query<RepairRecordDetail>("SELECT a.*, b.FaultTypeName FROM `repair_record` a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0;"));
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
            var data =
                ServerConfig.RepairDb.Query<RepairRecordDetail>("SELECT a.*, b.FaultTypeName FROM `repair_record` a JOIN `fault_type` b ON a.FaultTypeId = b.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
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
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `repair_record` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            repairRecord.Id = id;
            repairRecord.CreateUserId = Request.GetIdentityInformation();
            repairRecord.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "UPDATE repair_record SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`DeviceCode` = @DeviceCode, `FaultTime` = @FaultTime, `Proposer` = @Proposer, `FaultDescription` = @FaultDescription, `Priority` = @Priority, " +
                "`FaultSolver` = @FaultSolver, `SolveTime` = @SolveTime, `SolvePlan` = @SolvePlan, `FaultTypeId` = @FaultTypeId WHERE `Id` = @Id;", repairRecord);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RepairRecord
        [HttpPost]
        public Result PostRepairRecord([FromBody] RepairRecord repairRecord)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id AND MarkedDelete = 0;", new { id = repairRecord.FaultTypeId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }
            repairRecord.CreateUserId = Request.GetIdentityInformation();
            repairRecord.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "INSERT INTO repair_record (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCode`, `FaultTime`, `Proposer`, `FaultDescription`, `Priority`, `FaultSolver`, `SolveTime`, `SolvePlan`, `FaultTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCode, @FaultTime, @Proposer, @FaultDescription, @Priority, @FaultSolver, @SolveTime, @SolvePlan, @FaultTypeId);",
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
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `repair_record` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RepairRecordNotExist);
            }

            ServerConfig.RepairDb.Execute(
                "UPDATE `repair_record` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}