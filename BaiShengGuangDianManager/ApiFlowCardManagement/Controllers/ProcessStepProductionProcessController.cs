//using ApiFlowCardManagement.Base.Server;
//using ApiFlowCardManagement.Models;
//using Microsoft.AspNetCore.Mvc;
//using ModelBase.Base.EnumConfig;
//using ModelBase.Base.Utils;
//using ModelBase.Models.Result;
//using ServiceStack;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ApiFlowCardManagement.Controllers
//{
//    /// <summary>
//    /// 工序
//    /// </summary>
//    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
//    [ApiController]
//    //[Authorize]
//    public class ProcessStepProductionProcessController : ControllerBase
//    {
//        // GET: api/ProcessStepProductionProcess
//        [HttpGet]
//        public DataResult GetProcessStepProductionProcess()
//        {
//            var result = new DataResult();
//            var datas = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.MarkedDelete = 0;");
//            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

//            foreach (var data in datas)
//            {
//                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
//                if (code != null)
//                {
//                    data.Code = code.Code;
//                }
//            }
//            result.datas.AddRange(datas);
//            return result;
//        }

//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id">自增Id</param>
//        /// <returns></returns>
//        // GET: api/ProcessStepProductionProcess/Id/5
//        [HttpGet("Id/{id}")]
//        public DataResult GetProcessStepProductionProcess([FromRoute] int id)
//        {
//            var result = new DataResult();
//            var data = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.Id = @id AND a.MarkedDelete = 0;", new { id }).FirstOrDefault();
//            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT Id, `Code` FROM `device_library` WHERE `Id` = @Id AND MarkedDelete = 0;", new { Id = data.DeviceId });

//            if (deviceCodes.Any())
//            {
//                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
//                if (code != null)
//                {
//                    data.Code = code.Code;
//                }
//            }
//            result.datas.Add(data);
//            return result;
//        }

//        /// <summary>
//        /// 计划号
//        /// </summary>
//        /// <param name="productionProcessName">计划号</param>
//        /// <returns></returns>
//        // GET: api/ProcessStepProductionProcess/ProductionProcessName/5
//        [HttpGet("ProductionProcessName/{productionProcessName}")]
//        public DataResult GetProcessStepProductionProcess([FromRoute] string productionProcessName)
//        {
//            var productionProcess =
//                ServerConfig.FlowCardDb.Query<ProductionProcessLibrary>("SELECT * FROM `production_process_library` WHERE ProductionProcessName = @ProductionProcessName AND MarkedDelete = 0;", new
//                {
//                    productionProcessName
//                }).FirstOrDefault();
//            if (productionProcess == null)
//            {
//                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
//            }

//            var result = new DataResult();
//            var datas = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b " +
//                "ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE a.ProductionProcessId = @ProductionProcessId AND a.MarkedDelete = 0;", new
//                {
//                    ProductionProcessId = productionProcess.Id
//                });
//            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

//            foreach (var data in datas)
//            {
//                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
//                if (code != null)
//                {
//                    data.Code = code.Code;
//                }
//            }
//            result.datas.AddRange(datas);
//            return result;
//        }

//        /// <summary>
//        /// 加工人
//        /// </summary>
//        /// <param name="processorName">加工人</param>
//        /// <returns></returns>
//        // GET: api/ProcessStepProductionProcess/ProcessorName/5
//        [HttpGet("ProcessorName/{processorName}")]
//        public DataResult GetProcessStepProductionProcessByProcessorName([FromRoute] string processorName)
//        {
//            if (processorName.IsNullOrEmpty())
//            {
//                return Result.GenError<DataResult>(Error.ProcessorNotExist);
//            }

//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT `Id` FROM `processor` WHERE ProcessorName = @ProcessorName AND MarkedDelete = 0;", new { ProcessorName = processorName }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<DataResult>(Error.ProcessorNotExist);
//            }

//            var result = new DataResult();
//            var datas = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
//                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE b.ProcessorName = @ProcessorName AND a.MarkedDelete = 0;", new
//                {
//                    ProcessorName = processorName
//                });
//            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

//            foreach (var data in datas)
//            {
//                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
//                if (code != null)
//                {
//                    data.Code = code.Code;
//                }
//            }
//            result.datas.AddRange(datas);
//            return result;
//        }

//        /// <summary>
//        /// 检验员
//        /// </summary>
//        /// <param name="surveyorName">检验员</param>
//        /// <returns></returns>
//        // GET: api/ProcessStepProductionProcess/SurveyorName/5
//        [HttpGet("SurveyorName/{surveyorName}")]
//        public DataResult GetProcessStepProductionProcessBySurveyorName([FromRoute] string surveyorName)
//        {
//            if (surveyorName.IsNullOrEmpty())
//            {
//                return Result.GenError<DataResult>(Error.ProcessorNotExist);
//            }

//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE SurveyorName = @SurveyorName AND MarkedDelete = 0;", new { SurveyorName = surveyorName }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<DataResult>(Error.SurveyorNotExist);
//            }


//            var result = new DataResult();
//            var datas = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
//                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE c.SurveyorName = @SurveyorName AND a.MarkedDelete = 0;", new
//                {
//                    SurveyorName = surveyorName
//                });
//            var deviceCodes = ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

//            foreach (var data in datas)
//            {
//                var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
//                if (code != null)
//                {
//                    data.Code = code.Code;
//                }
//            }
//            result.datas.AddRange(datas); return result;
//        }

//        /// <summary>
//        /// 机台号
//        /// </summary>
//        /// <param name="code">机台号</param>
//        /// <returns></returns>
//        // GET: api/ProcessStepProductionProcess/Code/5
//        [HttpGet("Code/{code}")]
//        public DataResult GetProcessStepProductionProcessByCode([FromRoute] string code)
//        {
//            if (code.IsNullOrEmpty())
//            {
//                return Result.GenError<DataResult>(Error.ProcessorNotExist);
//            }

//            var device =
//                ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>("SELECT Id FROM `device_library` WHERE `Code` = @code AND MarkedDelete = 0;", new { code }).FirstOrDefault();
//            if (device == null)
//            {
//                return Result.GenError<DataResult>(Error.DeviceNotExist);
//            }


//            var result = new DataResult();
//            var datas = ServerConfig.FlowCardDb.Query<ProcessStepProductionProcessDetail>(
//                "SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName FROM `process_step_production_process` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id " +
//                "LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE DeviceId = @DeviceId AND a.MarkedDelete = 0;", new
//                {
//                    DeviceId = device.Id
//                });

//            foreach (var data in datas)
//            {
//                data.Code = code;
//            }
//            result.datas.AddRange(datas); return result;
//        }



//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id">自增Id</param>
//        /// <param name="processStep"></param>
//        /// <returns></returns>
//        // PUT: api/ProcessStepProductionProcess/5
//        [HttpPut("{id}")]
//        public Result PutProcessStepProductionProcess([FromRoute] int id, [FromBody] ProcessStepProductionProcess processStep)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `process_step_production_process` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.ProcessStepNotExist);
//            }

//            cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id = @Id AND MarkedDelete = 0;", new
//                {
//                    Id = processStep.ProductionProcessId
//                }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
//            }

//            if (processStep.ProcessorId != 0)
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` = @ProcessorId AND MarkedDelete = 0;", new { processStep.ProcessorId }).FirstOrDefault();
//                if (cnt == 0)
//                {
//                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
//                }
//            }
//            if (processStep.SurveyorId != 0)
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` = @SurveyorId AND MarkedDelete = 0;", new { processStep.SurveyorId }).FirstOrDefault();
//                if (cnt == 0)
//                {
//                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
//                }
//            }

//            if (processStep.DeviceId != 0)
//            {
//                var device =
//                    ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>("SELECT Id FROM `device_library` WHERE `Id` = @DeviceId AND MarkedDelete = 0;", new { processStep.DeviceId }).FirstOrDefault();
//                if (device == null)
//                {
//                    return Result.GenError<DataResult>(Error.DeviceNotExist);
//                }
//            }

//            processStep.Id = id;
//            processStep.CreateUserId = Request.GetIdentityInformation();
//            processStep.MarkedDateTime = DateTime.Now;
//            ServerConfig.FlowCardDb.Execute("UPDATE process_step_production_process SET `Id` = @Id, `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
//                                            "`ModifyId` = @ModifyId, `ProductionProcessId` = @ProductionProcessId, `ProcessStepOrder` = @ProcessStepOrder, `ProcessStepName` " +
//                                            "= @ProcessStepName, `ProcessStepRequirements` = @ProcessStepRequirements, `ProcessorId` = @ProcessorId, `ProcessTime` = " +
//                                            "@ProcessTime, `SurveyorId` = @SurveyorId, `SurveyTime` = @SurveyTime, `QualifiedNumber` = @QualifiedNumber, `UnqualifiedNumber` = @UnqualifiedNumber, " +
//                                            "`DeviceId` = @DeviceId WHERE `Id` = @Id;", processStep);

//            return Result.GenError<Result>(Error.Success);
//        }



//        // POST: api/ProcessStepProductionProcess
//        [HttpPost]
//        public Result PostProcessStepProductionProcess([FromBody] ProcessStepProductionProcess processStep)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id = @Id AND MarkedDelete = 0;", new
//                {
//                    Id = processStep.ProductionProcessId
//                }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
//            }
//            if (processStep.ProcessorId != 0)
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` = @ProcessorId AND MarkedDelete = 0;", new { processStep.ProcessorId }).FirstOrDefault();
//                if (cnt == 0)
//                {
//                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
//                }
//            }
//            if (processStep.SurveyorId != 0)
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` = @SurveyorId AND MarkedDelete = 0;", new { processStep.SurveyorId }).FirstOrDefault();
//                if (cnt == 0)
//                {
//                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
//                }
//            }

//            if (processStep.DeviceId != 0)
//            {
//                var device =
//                    ServerConfig.DeviceDb.Query<ProcessStepProductionProcessDetail>("SELECT Id FROM `device_library` WHERE `Id` = @DeviceId AND MarkedDelete = 0;", new { processStep.DeviceId }).FirstOrDefault();
//                if (device == null)
//                {
//                    return Result.GenError<DataResult>(Error.DeviceNotExist);
//                }
//            }
//            processStep.CreateUserId = Request.GetIdentityInformation();
//            processStep.MarkedDateTime = DateTime.Now;
//            ServerConfig.FlowCardDb.Execute(
//                "INSERT INTO process_step_production_process(`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, " +
//                "`ProcessStepName`, `ProcessStepRequirements`, `ProcessorId`, `ProcessTime`, `SurveyorId`, `SurveyTime`, `QualifiedNumber`, " +
//                "`UnqualifiedNumber`, `DeviceId`) " +
//                "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, " +
//                "@ProcessStepName, @ProcessStepRequirements, @ProcessorId, @ProcessTime, @SurveyorId, @SurveyTime, @QualifiedNumber, @UnqualifiedNumber," +
//                " @DeviceId); ",
//                processStep);

//            return Result.GenError<Result>(Error.Success);
//        }

//        /// <summary>
//        /// 批量添加
//        /// </summary>
//        /// <param name="processSteps"></param>
//        /// <returns></returns>
//        // POST: api/ProcessStepProductionProcess/ProcessSteps
//        [HttpPost("ProcessSteps")]
//        public Result PostProcessStepProductionProcess([FromBody] List<ProcessStepProductionProcess> processSteps)
//        {
//            var productionProcessIds = processSteps.GroupBy(x => x.ProductionProcessId);
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE Id IN @ProductionProcessIds AND MarkedDelete = 0;", new
//                {
//                    ProductionProcessIds = productionProcessIds.Select(x => x.Key)
//                }).FirstOrDefault();
//            if (cnt != productionProcessIds.Count())
//            {
//                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
//            }
//            var processorIds = processSteps.Where(x => x.ProcessorId != 0).GroupBy(x => x.ProcessorId).Select(x => x.Key);
//            if (processorIds.Any())
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` IN @ProcessorId AND MarkedDelete = 0;", new
//                    {
//                        ProcessorId = processorIds
//                    }).FirstOrDefault();
//                if (cnt != processorIds.Count())
//                {
//                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
//                }
//            }

//            var surveyorIds = processSteps.Where(x => x.SurveyorId != 0).GroupBy(x => x.SurveyorId).Select(x => x.Key);
//            if (surveyorIds.Any())
//            {
//                cnt =
//                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` IN @SurveyorId AND MarkedDelete = 0;", new
//                    {
//                        SurveyorId = surveyorIds
//                    }).FirstOrDefault();
//                if (cnt != surveyorIds.Count())
//                {
//                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
//                }
//            }

//            var deviceIds = processSteps.Where(x => x.DeviceId != 0).GroupBy(x => x.DeviceId).Select(x => x.Key);
//            if (deviceIds.Any())
//            {
//                cnt =
//                    ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Id` IN @DeviceId AND MarkedDelete = 0;", new
//                    {
//                        DeviceId = deviceIds
//                    }).FirstOrDefault();
//                if (cnt != deviceIds.Count())
//                {
//                    return Result.GenError<DataResult>(Error.DeviceNotExist);
//                }
//            }
//            foreach (var processStep in processSteps)
//            {
//                processStep.CreateUserId = Request.GetIdentityInformation();
//                processStep.MarkedDateTime = DateTime.Now;
//            }
//            ServerConfig.FlowCardDb.Execute(
//                "INSERT INTO process_step_production_process(`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, " +
//                "`ProcessStepName`, `ProcessStepRequirements`, `ProcessorId`, `ProcessTime`, `SurveyorId`, `SurveyTime`, `QualifiedNumber`, " +
//                "`UnqualifiedNumber`, `DeviceId`) " +
//                "VALUES(@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, " +
//                "@ProcessStepName, @ProcessStepRequirements, @ProcessorId, @ProcessTime, @SurveyorId, @SurveyTime, @QualifiedNumber, @UnqualifiedNumber," +
//                " @DeviceId); ",
//                processSteps.OrderBy(x => x.ProductionProcessId).ThenBy(y => y.ProcessStepOrder));

//            return Result.GenError<Result>(Error.Success);
//        }




//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        // DELETE: api/ProcessStepProductionProcess/Id/5
//        [HttpDelete("Id/{id}")]
//        public Result DeleteProcessStepProductionProcess([FromRoute] int id)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `process_step_production_process` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.ProcessStepNotExist);
//            }

//            ServerConfig.FlowCardDb.Execute(
//                "UPDATE `process_step_production_process` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
//                {
//                    MarkedDateTime = DateTime.Now,
//                    MarkedDelete = true,
//                    Id = id
//                });
//            return Result.GenError<Result>(Error.Success);
//        }

//        /// <summary>
//        /// 计划号
//        /// </summary>
//        /// <param name="productionProcessName">计划号</param>
//        /// <returns></returns>
//        // DELETE: api/ProcessStepProductionProcess/ProductionProcessName/5
//        [HttpDelete("ProductionProcessName/{productionProcessName}")]
//        public Result DeleteProcessStepProductionProcess([FromRoute] string productionProcessName)
//        {
//            var data =
//                ServerConfig.FlowCardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName AND MarkedDelete = 0;",
//                    new { productionProcessName }).FirstOrDefault();
//            if (data == null)
//            {
//                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
//            }

//            ServerConfig.FlowCardDb.Execute(
//                "UPDATE `process_step_production_process` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProductionProcessId`= @ProductionProcessId;", new
//                {
//                    MarkedDateTime = DateTime.Now,
//                    MarkedDelete = true,
//                    ProductionProcessId = data.Id
//                });
//            return Result.GenError<Result>(Error.Success);
//        }
//    }
//}