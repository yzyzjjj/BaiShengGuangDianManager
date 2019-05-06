using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 加工人
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessorController : ControllerBase
    {
        // GET: api/Processor
        [HttpGet]
        public DataResult GetProcessor()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<Processor>("SELECT * FROM `processor` WHERE MarkedDelete = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/Processor/5
        [HttpGet("{id}")]
        public DataResult GetProcessor([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<Processor>("SELECT * FROM `processor` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProcessorNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="processor"></param>
        /// <returns></returns>
        // PUT: api/Processor/5
        [HttpPut("{id}")]
        public Result PutProcessor([FromRoute] int id, [FromBody] Processor processor)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessorNotExist);
            }

            processor.Id = id;
            processor.CreateUserId = Request.GetIdentityInformation();
            processor.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE processor SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `ProcessorName` = @ProcessorName WHERE `Id` = @Id;", processor);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Processor
        [HttpPost]
        public Result PostProcessor([FromBody] Processor processor)
        {
            processor.CreateUserId = Request.GetIdentityInformation();
            processor.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO processor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessorName`, `Account`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessorName, @Account);",
                processor);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Processor/Processors
        [HttpPost("Processors")]
        public Result PostProcessor([FromBody] List<Processor> processors)
        {
            foreach (var processor in processors)
            {
                processor.CreateUserId = Request.GetIdentityInformation();
                processor.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO processor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessorName`, `Account`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessorName, @Account);",
                processors);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/Processor/Id/5
        [HttpDelete("{id}")]
        public Result DeleteProcessor([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessorNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `processor` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}