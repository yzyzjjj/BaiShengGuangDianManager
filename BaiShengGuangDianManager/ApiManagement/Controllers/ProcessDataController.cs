using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers
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
            result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT * FROM `process_data` WHERE MarkedDelete = 0;"));
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
                ServerConfig.ApiDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProcessDataNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        public class ProcessDataQuery
        {
            /// <summary>
            /// 工艺编号
            /// </summary>
            public int Id;
            /// <summary>
            /// 当前厚度
            /// </summary>
            public decimal CurLand;
            /// <summary>
            /// 上次成厚
            /// </summary>
            public decimal LastLand;
            /// <summary>
            /// 目标成厚
            /// </summary>
            public decimal TarLand;
        }


        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="processDataQuery">工艺请求参数</param>
        /// <returns></returns>
        // Post: api/ProcessData/ProcessNumber
        [HttpPost("ProcessNumber")]
        public DataResult GetProcessDataByProcessNumber([FromBody] ProcessDataQuery processDataQuery)
        {
            var data =
                ServerConfig.ApiDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE Id = @id AND MarkedDelete = 0;", new { id = processDataQuery.Id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var pd = ServerConfig.ApiDb.Query<ProcessData>(
                "SELECT * FROM `process_data` WHERE ProcessManagementId = @Id AND MarkedDelete = 0;", new { data.Id });

            var result = new DataResult();
            if (processDataQuery.CurLand != 0)
            {
                if (pd.Any())
                {
                    if (processDataQuery.CurLand != processDataQuery.LastLand)
                    {
                        var pdOrder = pd.OrderByDescending(x => x.PressurizeMinute * 60 + x.PressurizeSecond).ThenBy(x => x.Id)
                            .First();
                        var t = (pdOrder.ProcessMinute * 60 + pdOrder.ProcessSecond) -
                                (pdOrder.PressurizeMinute * 60 + pdOrder.PressurizeSecond);
                        var pTime = (pdOrder.PressurizeMinute * 60 + pdOrder.PressurizeSecond) *
                                    ((processDataQuery.CurLand - processDataQuery.TarLand) /
                                     (processDataQuery.LastLand - processDataQuery.TarLand));

                        pdOrder.PressurizeMinute = (int)pTime / 60;
                        pdOrder.PressurizeSecond = (int)pTime % 60;
                        pdOrder.ProcessMinute = (int)(pTime + t) / 60;
                        pdOrder.ProcessSecond = (int)(pTime + t) % 60;
                    }
                }
            }

            result.datas.AddRange(pd);
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
                ServerConfig.ApiDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProcessDataNotExist);
            }

            var cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` = @Id AND MarkedDelete = 0;", new { Id = processData.ProcessManagementId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            processData.Id = id;
            processData.CreateUserId = Request.GetIdentityInformation();
            processData.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE process_data SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ProcessManagementId` = @ProcessManagementId, `ProcessOrder` = @ProcessOrder, `PressurizeMinute` = @PressurizeMinute, `PressurizeSecond` = @PressurizeSecond, " +
                "`Pressure` = @Pressure, `ProcessMinute` = @ProcessMinute, `ProcessSecond` = @ProcessSecond, `Speed` = @Speed WHERE `Id` = @Id;", processData);

            return Result.GenError<Result>(Error.Success);
        }



        // POST: api/ProcessData
        [HttpPost]
        public Result PostProcessData([FromBody] ProcessData processData)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` = @Id AND MarkedDelete = 0;", new { Id = processData.ProcessManagementId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            processData.CreateUserId = Request.GetIdentityInformation();
            processData.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `Id` IN @ProcessManagementId AND MarkedDelete = 0;", new
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
            ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `process_data` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessDataNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `process_data` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
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
                ServerConfig.ApiDb.Query<ProcessManagement>("SELECT `Id` FROM `process_management` WHERE `ProcessNumber` = @processNumber AND MarkedDelete = 0;", new { processNumber }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `process_data` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProcessManagementId`= @ProcessManagementId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    ProcessManagementId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}