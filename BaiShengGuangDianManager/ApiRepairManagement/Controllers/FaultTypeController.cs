using ApiRepairManagement.Base.Server;
using ApiRepairManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.ServerConfig.Enum;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiRepairManagement.Controllers
{
    /// <summary>
    /// 故障类型
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FaultTypeController : ControllerBase
    {
        // GET: api/FaultType
        [HttpGet]
        public DataResult GetFaultType()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.RepairDb.Query<FaultType>("SELECT * FROM `fault_type`;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/FaultType/5
        [HttpGet("{id}")]
        public DataResult GetFaultType([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.RepairDb.Query<FaultType>("SELECT * FROM `fault_type` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.FaultTypeNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="faultType"></param>
        /// <returns></returns>
        // PUT: api/FaultType/Id/5
        [HttpPut("{id}")]
        public Result PutFaultType([FromRoute] int id, [FromBody] FaultType faultType)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }

            faultType.Id = id;
            faultType.CreateUserId = Request.GetIdentityInformation();
            faultType.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "UPDATE fault_type SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `FaultTypeName` = @FaultTypeName, `FaultDescription` = @FaultDescription WHERE `Id` = @Id;", faultType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultType
        [HttpPost]
        public Result PostFaultType([FromBody] FaultType faultType)
        {
            faultType.CreateUserId = Request.GetIdentityInformation();
            faultType.MarkedDateTime = DateTime.Now;
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FaultTypeName`, `FaultDescription`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FaultTypeName, @FaultDescription);",
                faultType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FaultType/FaultTypes
        [HttpPost("FaultTypes")]
        public Result PostFaultType([FromBody] List<FaultType> faultTypes)
        {
            foreach (var faultType in faultTypes)
            {
                faultType.CreateUserId = Request.GetIdentityInformation();
                faultType.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.RepairDb.Execute(
                "INSERT INTO fault_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FaultTypeName`, `FaultDescription`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FaultTypeName, @FaultDescription);",
                faultTypes);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/FaultType/Id/5
        [HttpDelete("{id}")]
        public Result DeleteFaultType([FromRoute] int id)
        {
            var cnt =
                ServerConfig.RepairDb.Query<int>("SELECT COUNT(1) FROM `fault_type` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FaultTypeNotExist);
            }

            ServerConfig.RepairDb.Execute(
                "UPDATE `fault_type` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}