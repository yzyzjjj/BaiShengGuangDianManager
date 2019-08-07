using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlowCardTypeController : ControllerBase
    {
        // GET: api/FlowCardType
        [HttpGet]
        public DataResult GetFlowCardType()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FlowCardType>("SELECT * FROM `flowcard_type` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/FlowCardType/5
        [HttpGet("{id}")]
        public DataResult GetFlowCardType([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<FlowCardType>("SELECT * FROM `flowcard_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.FlowCardTypeNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/FlowCardType/5
        [HttpPut("{id}")]
        public Result PutFlowCardType([FromRoute] int id, [FromBody] FlowCardType flowCardType)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardTypeNotExist);
            }

            flowCardType.Id = id;
            flowCardType.CreateUserId = Request.GetIdentityInformation();
            flowCardType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE flowcard_type SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `TypeName` = @TypeName, `Abbre` = @Abbre WHERE `Id` = @Id;", flowCardType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FlowCardType
        [HttpPost]
        public Result PostFlowCardType([FromBody] FlowCardType flowCardType)
        {
            flowCardType.CreateUserId = Request.GetIdentityInformation();
            flowCardType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO flowcard_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `TypeName`, `Abbre`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @TypeName, @Abbre);",
              flowCardType);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/FlowCardType/5
        [HttpDelete("{id}")]
        public Result DeleteFlowCardType([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardTypeNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_type` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}