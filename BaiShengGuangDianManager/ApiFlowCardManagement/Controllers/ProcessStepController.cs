using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiFlowCardManagement.Controllers
{
    /// <summary>
    /// 工序
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ProcessStepController : ControllerBase
    {
        // GET: api/ProcessStep
        [HttpGet]
        public DataResult GetProcessStep()
        {
            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id;");
            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id;", new { Id = datas.Select(x => x.DeviceId) });

            foreach (var data in datas)
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                if (code != null)
                {
                    data.Code = code.Code;
                }
            }
            result.datas.AddRange(datas);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/ProcessStep/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetProcessStep([FromRoute] int id)
        {
            var result = new DataResult();
            var data = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.Id = @id;", new { id }).FirstOrDefault();
            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` = @Id;", new { Id = data.DeviceId });

            if (deviceCodes.Any())
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                if (code != null)
                {
                    data.Code = code.Code;
                }
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // GET: api/ProcessStep/ProductionProcessName/5
        [HttpGet("ProductionProcessName/{productionProcessName}")]
        public DataResult GetProcessStep([FromRoute] string productionProcessName)
        {
            var productionProcess =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT * FROM `production_process_library` WHERE ProductionProcessName = @ProductionProcessName;", new
                {
                    productionProcessName
                }).FirstOrDefault();
            if (productionProcess == null)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.ProductionProcessId = @ProductionProcessId;", new
                {
                    ProductionProcessId = productionProcess.Id
                });
            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id;", new { Id = datas.Select(x => x.DeviceId) });

            foreach (var data in datas)
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                if (code != null)
                {
                    data.Code = code.Code;
                }
            }
            result.datas.AddRange(datas);
            return result;
        }

        /// <summary>
        /// 加工人
        /// </summary>
        /// <param name="processorName">加工人</param>
        /// <returns></returns>
        // GET: api/ProcessStep/ProcessorName/5
        [HttpGet("ProcessorName/{processorName}")]
        public DataResult GetProcessStepByProcessorName([FromRoute] string processorName)
        {
            if (processorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT `Id` FROM `processor` WHERE ProcessorName = @ProcessorName;", new { ProcessorName = processorName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE b.ProcessorName = @ProcessorName;", new
                {
                    ProcessorName = processorName
                });
            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id;", new { Id = datas.Select(x => x.DeviceId) });

            foreach (var data in datas)
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                if (code != null)
                {
                    data.Code = code.Code;
                }
            }
            result.datas.AddRange(datas);
            return result;
        }

        /// <summary>
        /// 检验员
        /// </summary>
        /// <param name="surveyorName">检验员</param>
        /// <returns></returns>
        // GET: api/ProcessStep/SurveyorName/5
        [HttpGet("SurveyorName/{surveyorName}")]
        public DataResult GetProcessStepBySurveyorName([FromRoute] string surveyorName)
        {
            if (surveyorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE SurveyorName = @SurveyorName;", new { SurveyorName = surveyorName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.SurveyorNotExist);
            }


            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE c.SurveyorName = @SurveyorName;", new
                {
                    SurveyorName = surveyorName
                });
            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id;", new { Id = datas.Select(x => x.DeviceId) });

            foreach (var data in datas)
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                if (code != null)
                {
                    data.Code = code.Code;
                }
            }
            result.datas.AddRange(datas); return result;
        }

        /// <summary>
        /// 机台号
        /// </summary>
        /// <param name="code">机台号</param>
        /// <returns></returns>
        // GET: api/ProcessStep/Code/5
        [HttpGet("Code/{code}")]
        public DataResult GetProcessStepByCode([FromRoute] string code)
        {
            if (code.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var device =
                ServerConfig.DeviceDb.Query<ProcessStepDetail>("SELECT Id FROM `device_library` WHERE `Code` = @code;", new { code }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<DataResult>(Error.DeviceNotExist);
            }


            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<ProcessStepDetail>(
                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE DeviceId = @DeviceId;", new
                {
                    DeviceId = device.Id
                });

            foreach (var data in datas)
            {
                data.Code = code;
            }
            result.datas.AddRange(datas); return result;
        }



        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="processStep"></param>
        /// <returns></returns>
        // PUT: api/ProcessStep/5
        [HttpPut("{id}")]
        public Result PutProcessStep([FromRoute] int id, [FromBody] ProcessStep processStep)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `process_step` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessStepNotExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id = @Id;", new
                {
                    Id = processStep.ProductionProcessId
                }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            if (processStep.ProcessorId != 0)
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` = @ProcessorId;", new { processStep.ProcessorId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
                }
            }
            if (processStep.SurveyorId != 0)
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` = @SurveyorId;", new { processStep.SurveyorId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
                }
            }

            if (processStep.DeviceId != 0)
            {
                var device =
                    ServerConfig.DeviceDb.Query<ProcessStepDetail>("SELECT Id FROM `device_library` WHERE `Id` = @DeviceId;", new { processStep.DeviceId }).FirstOrDefault();
                if (device == null)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
            }

            processStep.Id = id;
            processStep.CreateUserId = Request.GetIdentityInformation();
            processStep.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute("UPDATE process_step SET `Id` = @Id, `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                                            "`ModifyId` = @ModifyId, `ProductionProcessId` = @ProductionProcessId, `ProcessStepOrder` = @ProcessStepOrder, `ProcessStepName` " +
                                            "= @ProcessStepName, `ProcessStepRequirements` = @ProcessStepRequirements, `ProcessorId` = @ProcessorId, `ProcessorTime` = " +
                                            "@ProcessorTime, `SurveyorId` = @SurveyorId, `SurveyTime` = @SurveyTime, `QualifiedNumber` = @QualifiedNumber, `UnqualifiedNumber` = @UnqualifiedNumber, " +
                                            "`DeviceId` = @DeviceId WHERE `Id` = @Id;", processStep);

            return Result.GenError<Result>(Error.Success);
        }



        // POST: api/ProcessStep
        [HttpPost]
        public Result PostProcessStep([FromBody] ProcessStep processStep)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id = @Id;", new
                {
                    Id = processStep.ProductionProcessId
                }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }
            if (processStep.ProcessorId != 0)
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` = @ProcessorId;", new { processStep.ProcessorId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
                }
            }
            if (processStep.SurveyorId != 0)
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` = @SurveyorId;", new { processStep.SurveyorId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
                }
            }

            if (processStep.DeviceId != 0)
            {
                var device =
                    ServerConfig.DeviceDb.Query<ProcessStepDetail>("SELECT Id FROM `device_library` WHERE `Id` = @DeviceId;", new { processStep.DeviceId }).FirstOrDefault();
                if (device == null)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
            }
            processStep.CreateUserId = Request.GetIdentityInformation();
            processStep.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO process_step(`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, " +
                "`ProcessStepName`, `ProcessStepRequirements`, `ProcessorId`, `ProcessorTime`, `SurveyorId`, `SurveyTime`, `QualifiedNumber`, " +
                "`UnqualifiedNumber`, `DeviceId`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, " +
                "@ProcessStepName, @ProcessStepRequirements, @ProcessorId, @ProcessorTime, @SurveyorId, @SurveyTime, @QualifiedNumber, @UnqualifiedNumber," +
                " @DeviceId); ",
                processStep);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="processSteps"></param>
        /// <returns></returns>
        // POST: api/ProcessStep/ProcessSteps
        [HttpPost("ProcessSteps")]
        public Result PostProcessStep([FromBody] List<ProcessStep> processSteps)
        {
            var productionProcessIds = processSteps.GroupBy(x => x.ProductionProcessId);
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id IN @ProductionProcessIds;", new
                {
                    ProductionProcessIds = productionProcessIds.Select(x => x.Key)
                }).FirstOrDefault();
            if (cnt != productionProcessIds.Count())
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }
            var processorIds = processSteps.Where(x => x.ProcessorId != 0).GroupBy(x => x.ProcessorId).Select(x => x.Key);
            if (processorIds.Any())
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` IN @ProcessorId;", new
                    {
                        ProcessorId = processorIds
                    }).FirstOrDefault();
                if (cnt != processorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
                }
            }

            var surveyorIds = processSteps.Where(x => x.SurveyorId != 0).GroupBy(x => x.SurveyorId).Select(x => x.Key);
            if (surveyorIds.Any())
            {
                cnt =
                    ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` IN @SurveyorId;", new
                    {
                        SurveyorId = surveyorIds
                    }).FirstOrDefault();
                if (cnt != surveyorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
                }
            }

            var deviceIds = processSteps.Where(x => x.DeviceId != 0).GroupBy(x => x.DeviceId).Select(x => x.Key);
            if (deviceIds.Any())
            {
                cnt =
                    ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Id` IN @DeviceId;", new
                    {
                        DeviceId = deviceIds
                    }).FirstOrDefault();
                if (cnt != deviceIds.Count())
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
            }
            foreach (var processStep in processSteps)
            {
                processStep.CreateUserId = Request.GetIdentityInformation();
                processStep.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO process_step(`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, " +
                "`ProcessStepName`, `ProcessStepRequirements`, `ProcessorId`, `ProcessorTime`, `SurveyorId`, `SurveyTime`, `QualifiedNumber`, " +
                "`UnqualifiedNumber`, `DeviceId`) " +
                "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, " +
                "@ProcessStepName, @ProcessStepRequirements, @ProcessorId, @ProcessorTime, @SurveyorId, @SurveyTime, @QualifiedNumber, @UnqualifiedNumber," +
                " @DeviceId); ",
                processSteps.OrderBy(x => x.ProductionProcessId).ThenBy(y => y.ProcessStepOrder));

            return Result.GenError<Result>(Error.Success);
        }




        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ProcessStep/Id/5
        [HttpDelete("Id/{id}")]
        public Result DeleteProcessStep([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `process_step` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessStepNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `process_step` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // DELETE: api/ProcessStep/ProductionProcessName/5
        [HttpDelete("ProductionProcessName/{productionProcessName}")]
        public Result DeleteProcessStep([FromRoute] string productionProcessName)
        {
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `process_step` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @ProductionProcessId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    ProductionProcessId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}