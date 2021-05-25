using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.DeviceManagementController
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
        public DataResult GetProcessor([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? AccountInfoHelper.GetProcessorMenu(qId)
                : AccountInfoHelper.GetProcessorDetails(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.ProcessorNotExist;
                return result;
            }
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
        /// account
        /// </summary>
        /// <param name="account">account</param>
        /// <param name="processor"></param>
        /// <returns></returns>
        // PUT: api/Processor/5
        [HttpPut("{account}")]
        public Result PutProcessor([FromRoute] string account, [FromBody] Processor processor)
        {
            var data =
                ServerConfig.ApiDb.Query<Processor>("SELECT * FROM `processor` WHERE Account = @Account;", new { Account = account }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProcessorNotExist);
            }

            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE ProcessorName = @ProcessorName;", new { processor.ProcessorName }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    if (!processor.ProcessorName.IsNullOrEmpty() && data.ProcessorName != processor.ProcessorName)
            //    {
            //        return Result.GenError<Result>(Error.ProcessorIsExist);
            //    }
            //}

            processor.Id = data.Id;
            processor.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE processor SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ProcessorName` = @ProcessorName WHERE `Id` = @Id;",
                processor);

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
            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var processor in processors)
            {
                processor.CreateUserId = createUserId;
                processor.MarkedDateTime = time;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO processor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessorName`, `Account`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessorName, @Account);",
                processors);

            return Result.GenError<Result>(Error.Success);
        }



        /// <summary>
        /// account
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        // DELETE: api/Processor/5
        [HttpDelete("{account}")]
        public Result DeleteProcessor([FromRoute] string account)
        {
            var data =
                ServerConfig.ApiDb.Query<Processor>("SELECT * FROM `processor` WHERE Account = @Account AND MarkedDelete = 0;", new { Account = account }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProcessorNotExist);
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `processor` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}