using ApiRepairManagement.Base.Server;
using ApiRepairManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiRepairManagement.Controllers
{
    /// <summary>
    /// 常见故障
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuallyFaultController : ControllerBase
    {
        // GET: api/UsuallyFault
        [HttpGet]
        public DataResult GetUsuallyFault()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.RepairDb.Query<UsuallyFault>("SELECT * FROM `usually_fault` WHERE MarkedDelete = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/UsuallyFault/5
        [HttpGet("{id}")]
        public DataResult GetUsuallyFault([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.RepairDb.Query<UsuallyFault>("SELECT * FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.UsuallyFaultNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="usuallyFault"></param>
        /// <returns></returns>
        // PUT: api/UsuallyFault/Id/5
        [HttpPut("{id}")]
        public Result PutUsuallyFault([FromRoute] int id, [FromBody] UsuallyFault usuallyFault)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyFaultNotExist);
            }

            usuallyFault.Id = id;
            usuallyFault.CreateUserId = Request.GetIdentityInformation();
            usuallyFault.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "UPDATE usually_fault SET `Id` = @Id, `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `UsuallyFaultDesc` = @UsuallyFaultDesc, `SolverPlan` = @SolverPlan WHERE `Id` = @Id;", usuallyFault);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyFault
        [HttpPost]
        public Result PostUsuallyFault([FromBody] UsuallyFault usuallyFault)
        {
            usuallyFault.CreateUserId = Request.GetIdentityInformation();
            usuallyFault.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "INSERT INTO usually_fault (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `UsuallyFaultDesc`, `SolverPlan`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @UsuallyFaultDesc, @SolverPlan);",
                usuallyFault);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyFault/UsuallyFaults
        [HttpPost("UsuallyFaults")]
        public Result PostUsuallyFault([FromBody] List<UsuallyFault> usuallyFaults)
        {
            foreach (var usuallyFault in usuallyFaults)
            {
                usuallyFault.CreateUserId = Request.GetIdentityInformation();
                usuallyFault.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.RepairDb.Execute(
                "INSERT INTO usually_fault (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `UsuallyFaultDesc`, `SolverPlan`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @UsuallyFaultDesc, @SolverPlan);",
                usuallyFaults);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/UsuallyFault/Id/5
        [HttpDelete("{id}")]
        public Result DeleteUsuallyFault([FromRoute] int id)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyFaultNotExist);
            }

            ServerConfig.RepairDb.Execute(
                "UPDATE `usually_fault` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}