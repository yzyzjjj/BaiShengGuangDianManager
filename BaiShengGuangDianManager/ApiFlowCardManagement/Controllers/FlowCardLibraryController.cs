﻿using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.Device;

namespace ApiFlowCardManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class FlowCardLibraryController : ControllerBase
    {
        // GET: api/FlowCardLibrary
        [HttpGet]
        public DataResult GetFlowCardLibrary()
        {
            var result = new DataResult();
            var datas = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` WHERE MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId WHERE a.MarkedDelete = 0;");

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

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
        /// 自增ID
        /// </summary>
        /// <param name="id">自增ID</param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetFlowCardLibraryById([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.RawMateriaName, c.ProductionProcessName FROM `flowcard_library` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id JOIN `production_library` c ON a.ProductionProcessId = c.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionProcessLibraryNotExist;
                return result;
            }
            data.Specifications.AddRange(ServerConfig.FlowCardDb.Query<FlowCardSpecification>("SELECT * FROM `flowcard_specification` WHERE FlowCardId = @FlowCardId AND MarkedDelete = 0;", new
            {
                FlowCardId = data.Id
            }));
            data.ProcessSteps.AddRange(ServerConfig.FlowCardDb.Query<FlowCardProcessStepDetail>("SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') " +
                "SurveyorName FROM `flowcard_process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE FlowCardId = @FlowCardId AND a.MarkedDelete = 0;", new
                {
                    FlowCardId = data.Id
                }));
            var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = data.ProcessSteps.Select(x => x.DeviceId) });

            foreach (var processStep in data.ProcessSteps)
            {
                var code = deviceCodes.FirstOrDefault(x => x.Id == processStep.DeviceId);
                if (code != null)
                {
                    processStep.Code = code.Code;
                }
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 获取加工工序数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/ProcessData/5
        [HttpGet("ProcessData/{id}")]
        public object GetFlowCardLibraryProcessDataById([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE MarkedDelete = 0 AND Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            var processSteps =
            ServerConfig.FlowCardDb.Query<FlowCardProcessStepDetail>("SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') SurveyorName " +
                                               "FROM flowcard_process_step a LEFT JOIN processor b ON a.ProcessorId = b.Id LEFT JOIN surveyor c ON a.SurveyorId = c.Id WHERE a.FlowCardId = @id AND a.MarkedDelete = 0;", new { id });

            var processors = ServerConfig.FlowCardDb.Query<dynamic>("SELECT Id, `ProcessorName` FROM `processor` WHERE MarkedDelete = 0;");
            var surveyors = ServerConfig.FlowCardDb.Query<dynamic>("SELECT Id, `SurveyorName` FROM `surveyor` WHERE MarkedDelete = 0;");
            var deviceIds = ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, `Code` FROM `device_library` WHERE MarkedDelete = 0;");

            return new
            {
                errno = 0,
                errmsg = "成功",
                processSteps,
                processors,
                surveyors,
                deviceIds
            };
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/ProductionProcessName/5
        [HttpGet("ProductionProcessName/{productionProcessName}")]
        public DataResult GetFlowCardLibraryByProductionProcessName([FromRoute] string productionProcessName)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `production_process_step` WHERE ProcessTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE b.ProductionProcessName = @productionProcessName AND a.MarkedDelete = 0", new { productionProcessName });
            if (!datas.Any())
            {
                result.errno = Error.FlowCardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

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
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName">原料批号</param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/RawMateriaName/5
        [HttpGet("RawMateriaName/{rawMateriaName}")]
        public DataResult GetFlowCardLibraryByRawMateriaName([FromRoute] string rawMateriaName)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `production_process_step` WHERE ProcessTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE c.RawMateriaName = @rawMateriaName AND a.MarkedDelete = 0", new { rawMateriaName });
            if (!datas.Any())
            {
                result.errno = Error.FlowCardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

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
        // GET: api/FlowCardLibrary/ProcessorName/5
        [HttpGet("ProcessorName/{processorName}")]
        public DataResult GetFlowCardLibraryByProcessorName([FromRoute] string processorName)
        {
            if (processorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var processor =
                ServerConfig.FlowCardDb.Query<Processor>("SELECT Id FROM `processor` WHERE ProcessorName = @ProcessorName AND MarkedDelete = 0;", new { ProcessorName = processorName }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var productionProcessIds = ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT ProductionProcessId FROM `production_process_step` WHERE ProcessorId = @ProcessorId AND MarkedDelete = 0; ", new
            {
                ProcessorId = processor.Id
            });

            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                                 "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_library` " +
                                                                                 "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                                 "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `production_process_step` WHERE ProcessTime " +
                                                                                 "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                                 "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.ProductionProcessId IN @ProductionProcessId AND a.MarkedDelete = 0", new
                                                                                 {
                                                                                     ProductionProcessId = productionProcessIds.Select(x => x.ProductionProcessId)
                                                                                 });
                if (!datas.Any())
                {
                    result.errno = Error.FlowCardLibraryNotExist;
                    return result;
                }

                var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

                foreach (var data in datas)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                    if (code != null)
                    {
                        data.Code = code.Code;
                    }
                }
                result.datas.AddRange(datas);
            }
            return result;
        }

        /// <summary>
        /// 检验员
        /// </summary>
        /// <param name="surveyorName">检验员</param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/SurveyorName/5
        [HttpGet("SurveyorName/{surveyorName}")]
        public DataResult GetFlowCardLibraryBySurveyorName([FromRoute] string surveyorName)
        {
            if (surveyorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var surveyor =
                ServerConfig.FlowCardDb.Query<Processor>("SELECT Id FROM `surveyor` WHERE SurveyorName = @SurveyorName AND MarkedDelete = 0;", new { SurveyorName = surveyorName }).FirstOrDefault();
            if (surveyor == null)
            {
                return Result.GenError<DataResult>(Error.SurveyorNotExist);
            }

            var productionProcessIds = ServerConfig.FlowCardDb.Query<ProductionProcessStep>(
                "SELECT ProductionProcessId FROM `production_process_step` WHERE SurveyorId = @SurveyorId; ", new
                {
                    SurveyorId = surveyor.Id
                });


            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                                 "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_library` " +
                                                                                 "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                                 "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `production_process_step` WHERE ProcessTime " +
                                                                                 "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                                 "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.ProductionProcessId IN @ProductionProcessId AND a.MarkedDelete = 0", new
                                                                                 {
                                                                                     ProductionProcessId = productionProcessIds.Select(x => x.ProductionProcessId)
                                                                                 });
                if (!datas.Any())
                {
                    result.errno = Error.FlowCardLibraryNotExist;
                    return result;
                }

                var deviceCodes = ServerConfig.DeviceDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = datas.Select(x => x.DeviceId) });

                foreach (var data in datas)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                    if (code != null)
                    {
                        data.Code = code.Code;
                    }
                }
                result.datas.AddRange(datas);
            }
            return result;
        }

        public class FlowCardInfo
        {
            public int Id;
            public string FlowCardName;
        }

        /// <summary>
        /// 加工获取 流程卡
        /// </summary>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/Detail
        [HttpPost("Detail")]
        public object GetFlowCardLibraryDetail([FromBody] FlowCardInfo flowCardInfo)
        {
            var deviceId = ServerConfig.DeviceDb.Query<int>(
                "SELECT Id FROM `device_library` WHERE `Id` = @id;", new { flowCardInfo.Id }).FirstOrDefault();

            if (deviceId == 0)
            {
                return Result.GenError<DataResult>(Error.DeviceNotExist);
            }

            var flowCard = ServerConfig.FlowCardDb.Query<FlowCardLibraryDetail>("SELECT a.Id, a.ProductionProcessId, a.RawMateriaId, a.Priority, b.ProductionProcessName, c.RawMateriaName FROM `flowcard_library` " +
                                                                                "a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c " +
                                                                                "ON a.RawMateriaId = c.Id WHERE a.FlowCardName = @flowCardName AND a.MarkedDelete = 0;",
                new { flowCardName = flowCardInfo.FlowCardName }).FirstOrDefault();
            if (flowCard == null)
            {
                return Result.GenError<DataResult>(Error.FlowCardLibraryNotExist);
            }

            var processNumber = ServerConfig.ProcessDb.Query<dynamic>(
                "SELECT Id, ProcessNumber FROM `process_management` " +
                "WHERE FIND_IN_SET(@DeviceId, DeviceIds) AND FIND_IN_SET(@ProductModel, ProductModels) AND MarkedDelete = 0;", new
                {
                    DeviceId = deviceId,
                    ProductModel = flowCard.ProductionProcessId
                }).FirstOrDefault();
            if (processNumber == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            var rawMateriaSpecifications =
                ServerConfig.FlowCardDb.Query<dynamic>(
                    "SELECT SpecificationName, SpecificationValue FROM `raw_materia_specification` WHERE RawMateriaId = @RawMateriaId AND MarkedDelete = 0;", new { flowCard.RawMateriaId });

            return new
            {
                errno = 0,
                errmsg = "成功",
                flowCard = new
                {
                    flowCardId = flowCard.Id,
                    flowCard.ProductionProcessName,
                    flowCard.Priority,
                    RawMateriaSpecifications = rawMateriaSpecifications,
                    ProcessId = processNumber.Id,
                    processNumber.ProcessNumber,
                }
            };
        }




        /// <summary>
        /// 自增ID
        /// </summary>
        /// <param name="id">自增ID</param>
        /// <param name="flowCardLibrary"></param>
        /// <returns></returns>
        // PUT: api/FlowCardLibrary/5
        [HttpPut("{id}")]
        public Result PutFlowCardLibrary([FromRoute] int id, [FromBody] FlowCardLibrary flowCardLibrary)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            flowCardLibrary.Id = id;
            flowCardLibrary.CreateUserId = createUserId;
            flowCardLibrary.MarkedDateTime = time;
            ServerConfig.FlowCardDb.Execute(
                "UPDATE flowcard_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, `InboundNum` = @InboundNum, " +
                "`Remarks` = @Remarks, `Priority` = @Priority WHERE `Id` = @Id;", flowCardLibrary);


            var specifications = flowCardLibrary.Specifications;
            if (specifications.Any())
            {
                foreach (var specification in specifications)
                {
                    specification.FlowCardId = id;
                    specification.CreateUserId = createUserId;
                    specification.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<FlowCardSpecification>("SELECT * FROM `flowcard_specification` " +
                                                                                 "WHERE MarkedDelete = 0 AND FlowCardId = @FlowCardId;", new { FlowCardId = id });
                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO flowcard_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @SpecificationName, @SpecificationValue);",
                    specifications.Where(x => x.Id == 0));

                var update = specifications.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                update.AddRange(exist.Where(x => specifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE flowcard_specification SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                    "`ModifyId` = @ModifyId,  `SpecificationName` = @SpecificationName, `SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", update);
            }

            var processSteps = flowCardLibrary.ProcessSteps;
            if (processSteps.Any())
            {
                foreach (var processStep in processSteps)
                {
                    processStep.FlowCardId = id;
                    processStep.CreateUserId = createUserId;
                    processStep.MarkedDateTime = time;
                }

                var exist = ServerConfig.FlowCardDb.Query<FlowCardProcessStepDetail>("SELECT * FROM `flowcard_process_step` " +
                                                                                     "WHERE MarkedDelete = 0 AND FlowCardId = @FlowCardId;", new { FlowCardId = id });

                ServerConfig.FlowCardDb.Execute(
                    "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements);",
                    processSteps.Where(x => x.Id == 0).OrderBy(x => x.ProcessStepOrder));


                var update = processSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                                    && (y.ProcessStepOrder != x.ProcessStepOrder || y.ProcessStepName != x.ProcessStepName || y.ProcessStepRequirements != x.ProcessStepRequirements))).ToList();
                update.AddRange(exist.Where(x => processSteps.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.FlowCardDb.Execute(
                    "UPDATE flowcard_process_step SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                    "`ProcessStepOrder` = @ProcessStepOrder, `ProcessStepName` = @ProcessStepName, `ProcessStepRequirements` = @ProcessStepRequirements " +
                    "WHERE `Id` = @Id;", update);
            }
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 流程卡 更新加工数据
        /// </summary>
        /// <param name="flowCardProcessSteps"></param>
        /// <returns></returns>
        // PUT: api/FlowCardLibrary/ProcessData/5
        [HttpPut("ProcessData")]
        public Result PutFlowCardLibraryProcessData([FromBody] List<FlowCardProcessStep> flowCardProcessSteps)
        {
            var group = flowCardProcessSteps.GroupBy(x => x.FlowCardId);
            if (group.Count() != 1 || group.First().Key <= 0)
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var id = group.First().Key;
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            var processorIds = flowCardProcessSteps.Select(x => x.ProcessorId).Where(x => x != 0);
            if (processorIds.Any())
            {
                cnt =
                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` in @ProcessorId AND MarkedDelete = 0;", new { ProcessorId = processorIds }).FirstOrDefault();
                if (cnt == 0 || cnt != processorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
                }
            }
            var surveyorIds = flowCardProcessSteps.Select(x => x.SurveyorId).Where(x => x != 0);
            if (surveyorIds.Any())
            {
                cnt =
                    ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` in @SurveyorId AND MarkedDelete = 0;", new { SurveyorId = surveyorIds }).FirstOrDefault();
                if (cnt == 0 || cnt != surveyorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
                }
            }
            var deviceIds = flowCardProcessSteps.Select(x => x.DeviceId).Where(x => x != 0);
            if (deviceIds.Any())
            {
                cnt =
                    ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Id` in @DeviceId AND MarkedDelete = 0;", new { DeviceId = deviceIds }).FirstOrDefault();
                if (cnt == 0 || cnt != deviceIds.Count())
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var processStep in flowCardProcessSteps)
            {
                processStep.CreateUserId = createUserId;
                processStep.MarkedDateTime = time;
            }
            ServerConfig.FlowCardDb.Execute(
                "UPDATE flowcard_process_step SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `ProcessorId` = @ProcessorId, `ProcessTime` = @ProcessTime, " +
                "`SurveyorId` = @SurveyorId, `SurveyTime` = @SurveyTime, `QualifiedNumber` = @QualifiedNumber, `UnqualifiedNumber` = @UnqualifiedNumber, " +
                "`DeviceId` = @DeviceId WHERE `Id` = @Id;", flowCardProcessSteps);

            return Result.GenError<Result>(Error.Success);
        }





        // POST: api/FlowCardLibrary
        [HttpPost]
        public Result PostFlowCardLibrary([FromBody] FlowCardLibrary flowCardLibrary)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @FlowCardName AND MarkedDelete = 0;", new { flowCardLibrary.FlowCardName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryIsExist);
            }

            cnt =
              ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE `Id` = @ProductionProcessId AND MarkedDelete = 0;", new { flowCardLibrary.ProductionProcessId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` = @RawMateriaId AND MarkedDelete = 0;", new { flowCardLibrary.RawMateriaId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var productionProcessId = flowCardLibrary.ProductionProcessId;
            var processSteps = ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });


            var productionProcessSpecifications = ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            flowCardLibrary.CreateUserId = createUserId;
            flowCardLibrary.MarkedDateTime = time;
            var index = ServerConfig.FlowCardDb.Query<int>(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority);SELECT LAST_INSERT_ID();",
                    flowCardLibrary).FirstOrDefault();

            flowCardLibrary.Id = index;
            foreach (var productionSpecification in productionProcessSpecifications)
            {
                flowCardLibrary.Specifications.Add(new FlowCardSpecification
                {
                    CreateUserId = createUserId,
                    MarkedDateTime = time,
                    FlowCardId = flowCardLibrary.Id,
                    SpecificationName = productionSpecification.SpecificationName,
                    SpecificationValue = productionSpecification.SpecificationValue,
                });
            }
            foreach (var processStep in processSteps)
            {
                flowCardLibrary.ProcessSteps.Add(new FlowCardProcessStepDetail
                {
                    CreateUserId = createUserId,
                    MarkedDateTime = time,
                    FlowCardId = flowCardLibrary.Id,
                    ProcessStepOrder = processStep.ProcessStepOrder,
                    ProcessStepName = processStep.ProcessStepName,
                    ProcessStepRequirements = processStep.ProcessStepRequirements,
                });
            }
            ServerConfig.FlowCardDb.Execute(
                "INSERT INTO flowcard_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `SpecificationName`, `SpecificationValue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @SpecificationName, @SpecificationValue);",
                flowCardLibrary.Specifications.OrderBy(x => x.FlowCardId));

            ServerConfig.FlowCardDb.Execute(
                "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements);",
                flowCardLibrary.ProcessSteps.OrderBy(x => x.ProcessStepOrder));

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FlowCardLibrary/FlowCardLibraries
        [HttpPost("FlowCardLibraries")]
        public Result PostFlowcardLibraries([FromBody] List<FlowCardLibrary> flowCardLibraries)
        {
            var flowCards = flowCardLibraries.GroupBy(x => x.FlowCardName);
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName AND MarkedDelete = 0;", new
                {
                    FlowCardName = flowCards.Select(x => x.Key)
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryIsExist);
            }

            var productionProcessIds = flowCardLibraries.GroupBy(x => x.ProductionProcessId).Select(x => x.Key);
            cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE `Id` IN @ProductionProcessId AND MarkedDelete = 0;", new
                {
                    ProductionProcessId = productionProcessIds
                }).FirstOrDefault();
            if (cnt != productionProcessIds.Count() || cnt != 1)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var rawMateriaIds = flowCardLibraries.GroupBy(x => x.RawMateriaId).Select(x => x.Key);
            cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` IN @RawMateriaId AND MarkedDelete = 0;", new
                {
                    RawMateriaId = rawMateriaIds
                }).FirstOrDefault();
            if (cnt != rawMateriaIds.Count())
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var productionProcessId = productionProcessIds.FirstOrDefault();
            var processSteps = ServerConfig.FlowCardDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });


            var productionProcessSpecifications = ServerConfig.FlowCardDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var flowcardLibrary in flowCardLibraries)
            {
                flowcardLibrary.CreateUserId = createUserId;
                flowcardLibrary.MarkedDateTime = time;
            }

            ServerConfig.FlowCardDb.Execute(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority);",
                flowCardLibraries.OrderBy(x => x.FlowCardName));

            var insertDatas =
                ServerConfig.FlowCardDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName AND MarkedDelete = 0;", new
                {
                    FlowCardName = flowCardLibraries.Select(x => x.FlowCardName)
                }).ToDictionary(x => x.FlowCardName, x => x.Id);

            foreach (var flowCardLibrary in flowCardLibraries)
            {
                flowCardLibrary.Id = insertDatas[flowCardLibrary.FlowCardName];
                foreach (var productionSpecification in productionProcessSpecifications)
                {
                    flowCardLibrary.Specifications.Add(new FlowCardSpecification
                    {
                        CreateUserId = createUserId,
                        MarkedDateTime = time,
                        FlowCardId = flowCardLibrary.Id,
                        SpecificationName = productionSpecification.SpecificationName,
                        SpecificationValue = productionSpecification.SpecificationValue,
                    });
                }
                foreach (var processStep in processSteps)
                {
                    flowCardLibrary.ProcessSteps.Add(new FlowCardProcessStepDetail
                    {
                        CreateUserId = createUserId,
                        MarkedDateTime = time,
                        FlowCardId = flowCardLibrary.Id,
                        ProcessStepOrder = processStep.ProcessStepOrder,
                        ProcessStepName = processStep.ProcessStepName,
                        ProcessStepRequirements = processStep.ProcessStepRequirements,
                    });
                }
            }
            ServerConfig.FlowCardDb.Execute(
                "INSERT INTO flowcard_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `SpecificationName`, `SpecificationValue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @SpecificationName, @SpecificationValue);",
                flowCardLibraries.SelectMany(x => x.Specifications).OrderBy(x => x.FlowCardId));

            ServerConfig.FlowCardDb.Execute(
                "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepName`, `ProcessStepRequirements`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepName, @ProcessStepRequirements);",
                flowCardLibraries.SelectMany(x => x.ProcessSteps).OrderBy(x => x.FlowCardId).ThenBy(x => x.ProcessStepOrder));

            return Result.GenError<Result>(Error.Success);
        }




        /// <summary>
        /// 自增ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/FlowCardLibrary/5
        [HttpDelete("Id/{id}")]
        public Result DeleteFlowCardLibraryById([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_process_step` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_specification` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 流程卡号
        /// </summary>
        /// <param name="flowCardName"></param>
        /// <returns></returns>
        // DELETE: api/FlowCardLibrary/5
        [HttpDelete("FlowCardName/{flowCardName}")]
        public Result DeleteFlowCardLibraryByFlowCardName([FromRoute] string flowCardName)
        {
            var cnt =
                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @flowCardName AND MarkedDelete = 0;", new { flowCardName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE FlowCardName = @flowCardName AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    flowCardName
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName"></param>
        /// <returns></returns>
        // DELETE: api/FlowCardLibrary/ProductionProcessName/5
        [HttpDelete("ProductionProcessName/{productionProcessName}")]
        public Result DeleteFlowCardLibraryByProductionProcessName([FromRoute] string productionProcessName)
        {
            var data =
                ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE ProductionProcessName = @productionProcessName AND MarkedDelete = 0;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE ProductionProcessId = @productionProcessId AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    productionProcessId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName"></param>
        /// <returns></returns>
        // DELETE: api/FlowCardLibrary/RawMateriaName/5
        [HttpDelete("RawMateriaName/{rawMateriaName}")]
        public Result DeleteFlowCardLibraryByRawMateriaName([FromRoute] string rawMateriaName)
        {
            var data =
                ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowCardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE RawMateriaId = @rawMateriaId AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    rawMateriaId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}