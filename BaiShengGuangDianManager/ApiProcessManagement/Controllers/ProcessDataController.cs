﻿using ApiProcessManagement.Base.Server;
using ApiProcessManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiProcessManagement.Controllers
{
    /// <summary>
    /// 工艺数据
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessDataController : ControllerBase
    {
        // GET: api/ProcessData
        [HttpGet]
        public DataResult GetProcessData()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ProcessDb.Query<dynamic>("SELECT * FROM `process_data` WHERE MarkedDelete = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/ProcessData/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetProcessData([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ProcessDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProcessDataNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="id">工艺编号id</param>
        /// <returns></returns>
        // GET: api/ProcessData/ProcessNumber/5
        [HttpGet("ProcessNumber/{id}")]
        public DataResult GetProcessDataByProcessNumber([FromRoute] int id)
        {
            var data =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ProcessDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE ProcessManagementId = @Id AND MarkedDelete = 0;", new { data.Id }));
            return result;
        }




        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="processData"></param>
        /// <returns></returns>
        // PUT: api/ProcessData/5
        [HttpPut("{id}")]
        public Result PutProcessData([FromRoute] int id, [FromBody] ProcessData processData)
        {
            var data =
                ServerConfig.ProcessDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProcessDataNotExist);
            }

            var cnt =
               ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` = @Id AND MarkedDelete = 0;", new { Id = processData.ProcessManagementId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            processData.Id = id;
            processData.CreateUserId = Request.GetIdentityInformation();
            processData.MarkedDateTime = DateTime.Now;
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_data SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ProcessManagementId` = @ProcessManagementId, `ProcessOrder` = @ProcessOrder, `PressurizeMinute` = @PressurizeMinute, `PressurizeSecond` = @PressurizeSecond, " +
                "`Pressure` = @Pressure, `ProcessMinute` = @ProcessMinute, `ProcessSecond` = @ProcessSecond, `Speed` = @Speed WHERE `Id` = @Id;", processData);

            return Result.GenError<Result>(Error.Success);
        }



        // POST: api/ProcessData
        [HttpPost]
        public Result PostProcessData([FromBody] ProcessData processData)
        {
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` = @Id AND MarkedDelete = 0;", new { Id = processData.ProcessManagementId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            processData.CreateUserId = Request.GetIdentityInformation();
            processData.MarkedDateTime = DateTime.Now;
            ServerConfig.ProcessDb.Execute(
                "INSERT INTO process_data (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessManagementId`, `ProcessOrder`, `PressurizeMinute`, `PressurizeSecond`, `Pressure`, `ProcessMinute`, `ProcessSecond`, `Speed`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessManagementId, @ProcessOrder, @PressurizeMinute, @PressurizeSecond, @Pressure, @ProcessMinute, @ProcessSecond, @Speed);",
                processData);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProcessData/ProcessDatas
        [HttpPost("ProcessDatas")]
        public Result PostProcessData([FromBody] List<ProcessData> processDatas)
        {
            var processManagementIds = processDatas.GroupBy(x => x.ProcessManagementId).Select(x => x.Key);
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` IN @ProcessManagementId AND MarkedDelete = 0;", new
                {
                    ProcessManagementId = processManagementIds
                }).FirstOrDefault();
            if (cnt != processManagementIds.Count())
            {
                return Result.GenError<Result>(Error.ProcessManagementNotExist);
            }
            foreach (var processData in processDatas)
            {
                processData.CreateUserId = Request.GetIdentityInformation();
                processData.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.ProcessDb.Execute(
                "INSERT INTO process_data (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessManagementId`, `ProcessOrder`, `PressurizeMinute`, `PressurizeSecond`, `Pressure`, `ProcessMinute`, `ProcessSecond`, `Speed`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessManagementId, @ProcessOrder, @PressurizeMinute, @PressurizeSecond, @Pressure, @ProcessMinute, @ProcessSecond, @Speed);",
                processDatas);

            return Result.GenError<Result>(Error.Success);
        }



        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ProcessData/Id/5
        [HttpDelete("Id/{id}")]
        public Result DeleteProcessData([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessDataNotExist);
            }

            ServerConfig.ProcessDb.Execute(
                "UPDATE `process_data` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="processNumber"></param>
        /// <returns></returns>
        // DELETE: api/ProcessData/ProcessNumber/5
        [HttpDelete("ProcessNumber/{processNumber}")]
        public Result DeleteProcessData([FromRoute] string processNumber)
        {
            var data =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT `Id` FROM `process_management` WHERE `ProcessNumber` = @processNumber AND MarkedDelete = 0;", new { processNumber }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            ServerConfig.ProcessDb.Execute(
                "UPDATE `process_data` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProcessManagementId`= @ProcessManagementId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    ProcessManagementId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}