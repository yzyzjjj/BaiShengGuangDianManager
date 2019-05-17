using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 常见故障
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class UsuallyFaultController : ControllerBase
    {
        // GET: api/UsuallyFault
        [HttpGet]
        public DataResult GetUsuallyFault()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<UsuallyFault>("SELECT * FROM `usually_fault` WHERE MarkedDelete = 0;"));
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
                ServerConfig.ApiDb.Query<UsuallyFault>("SELECT * FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
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
            var data =
                ServerConfig.ApiDb.Query<UsuallyFault>("SELECT * FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.UsuallyFaultNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE UsuallyFaultDesc = @UsuallyFaultDesc;", new { usuallyFault.UsuallyFaultDesc }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!usuallyFault.UsuallyFaultDesc.IsNullOrEmpty() && data.UsuallyFaultDesc != usuallyFault.UsuallyFaultDesc)
                {
                    return Result.GenError<Result>(Error.UsuallyFaultIsExist);
                }
            }

            usuallyFault.Id = id;
            usuallyFault.CreateUserId = Request.GetIdentityInformation();
            usuallyFault.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE usually_fault SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `UsuallyFaultDesc` = @UsuallyFaultDesc, `SolverPlan` = @SolverPlan WHERE `Id` = @Id;", usuallyFault);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyFault
        [HttpPost]
        public Result PostUsuallyFault([FromBody] UsuallyFault usuallyFault)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE UsuallyFaultDesc = @UsuallyFaultDesc;", new { usuallyFault.UsuallyFaultDesc }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.UsuallyFaultIsExist);
            }

            usuallyFault.CreateUserId = Request.GetIdentityInformation();
            usuallyFault.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_fault (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `UsuallyFaultDesc`, `SolverPlan`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @UsuallyFaultDesc, @SolverPlan);",
                usuallyFault);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyFault/UsuallyFaults
        [HttpPost("UsuallyFaults")]
        public Result PostUsuallyFault([FromBody] List<UsuallyFault> usuallyFaults)
        {
            var usuallyFaultDesc = usuallyFaults.GroupBy(x => x.UsuallyFaultDesc).Select(x => x.Key);
            if (usuallyFaultDesc.Any())
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE UsuallyFaultDesc IN @UsuallyFaultDesc;", new { UsuallyFaultDesc = usuallyFaultDesc }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.UsuallyFaultIsExist);
                }
            }
            foreach (var usuallyFault in usuallyFaults)
            {
                usuallyFault.CreateUserId = Request.GetIdentityInformation();
                usuallyFault.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_fault` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyFaultNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `usually_fault` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}