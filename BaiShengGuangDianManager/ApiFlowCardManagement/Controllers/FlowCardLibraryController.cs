using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiFlowCardManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class FlowCardLibraryController : ControllerBase
    {
        // GET: api/FlowcardLibrary
        [HttpGet]
        public DataResult GetFlowcardLibrary()
        {
            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                         "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                         "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                         "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                         "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                         "d ON a.ProductionProcessId = d.ProductionProcessId;");


            var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
        /// 自增ID
        /// </summary>
        /// <param name="id">自增ID</param>
        /// <returns></returns>
        // GET: api/FlowcardLibrary/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetFlowcardLibraryById([FromRoute] int id)
        {
            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.Id = @id;", new { id });
            if (!datas.Any())
            {
                result.errno = Error.FlowcardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
        /// 流程卡号
        /// </summary>
        /// <param name="flowCardName">流程卡号</param>
        /// <returns></returns>
        // GET: api/FlowcardLibrary/FlowCardName/5
        [HttpGet("FlowCardName/{flowCardName}")]
        public DataResult GetFlowcardLibraryByFlowCardName([FromRoute] string flowCardName)
        {
            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.FlowCardName = @flowCardName;", new { flowCardName });
            if (!datas.Any())
            {
                result.errno = Error.FlowcardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
        /// 计划号
        /// </summary>
        /// <param name="productionProcessName">计划号</param>
        /// <returns></returns>
        // GET: api/FlowcardLibrary/ProductionProcessName/5
        [HttpGet("ProductionProcessName/{productionProcessName}")]
        public DataResult GetFlowcardLibraryByProductionProcessName([FromRoute] string productionProcessName)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE b.ProductionProcessName = @productionProcessName", new { productionProcessName });
            if (!datas.Any())
            {
                result.errno = Error.FlowcardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName">原料批号</param>
        /// <returns></returns>
        // GET: api/FlowcardLibrary/RawMateriaName/5
        [HttpGet("RawMateriaName/{rawMateriaName}")]
        public DataResult GetFlowcardLibraryByRawMateriaName([FromRoute] string rawMateriaName)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                             "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                             "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                             "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                             "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                             "d ON a.ProductionProcessId = d.ProductionProcessId WHERE c.RawMateriaName = @rawMateriaName", new { rawMateriaName });
            if (!datas.Any())
            {
                result.errno = Error.FlowcardLibraryNotExist;
                return result;
            }

            var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
        // GET: api/FlowcardLibrary/ProcessorName/5
        [HttpGet("ProcessorName/{processorName}")]
        public DataResult GetFlowcardLibraryByProcessorName([FromRoute] string processorName)
        {
            if (processorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var processor =
                ServerConfig.FlowcardDb.Query<Processor>("SELECT Id FROM `processor` WHERE ProcessorName = @ProcessorName;", new { ProcessorName = processorName }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var productionProcessIds = ServerConfig.FlowcardDb.Query<ProcessStep>("SELECT ProductionProcessId FROM `process_step` WHERE ProcessorId = @ProcessorId; ", new
            {
                ProcessorId = processor.Id
            });

            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                                 "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                                 "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                                 "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                                 "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                                 "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.ProductionProcessId IN @ProductionProcessId", new
                                                                                 {
                                                                                     ProductionProcessId = productionProcessIds.Select(x => x.ProductionProcessId)
                                                                                 });
                if (!datas.Any())
                {
                    result.errno = Error.FlowcardLibraryNotExist;
                    return result;
                }

                var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
            }
            return result;
        }

        /// <summary>
        /// 检验员
        /// </summary>
        /// <param name="surveyorName">检验员</param>
        /// <returns></returns>
        // GET: api/FlowcardLibrary/SurveyorName/5
        [HttpGet("SurveyorName/{surveyorName}")]
        public DataResult GetFlowcardLibraryBySurveyorName([FromRoute] string surveyorName)
        {
            if (surveyorName.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var surveyor =
                ServerConfig.FlowcardDb.Query<Processor>("SELECT Id FROM `surveyor` WHERE SurveyorName = @SurveyorName;", new { SurveyorName = surveyorName }).FirstOrDefault();
            if (surveyor == null)
            {
                return Result.GenError<DataResult>(Error.SurveyorNotExist);
            }

            var productionProcessIds = ServerConfig.FlowcardDb.Query<ProcessStep>(
                "SELECT ProductionProcessId FROM `process_step` WHERE SurveyorId = @SurveyorId; ", new
                {
                    SurveyorId = surveyor.Id
                });


            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.FlowcardDb.Query<FlowcardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepName, d.QualifiedNumber, " +
                                                                                 "d.DeviceId FROM `flowcard_library` a LEFT JOIN `production_process_library` " +
                                                                                 "b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = " +
                                                                                 "c.Id LEFT JOIN ( SELECT * FROM ( SELECT * FROM `process_step` WHERE ProcessorTime " +
                                                                                 "IS NOT NULL ORDER BY ProcessStepOrder DESC ) a GROUP BY a.ProductionProcessId ) " +
                                                                                 "d ON a.ProductionProcessId = d.ProductionProcessId WHERE a.ProductionProcessId IN @ProductionProcessId", new
                                                                                 {
                                                                                     ProductionProcessId = productionProcessIds.Select(x => x.ProductionProcessId)
                                                                                 });
                if (!datas.Any())
                {
                    result.errno = Error.FlowcardLibraryNotExist;
                    return result;
                }

                var deviceCodes = ServerConfig.DeviceDb.Query<FlowcardLibraryDetail>(
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
            }
            return result;
        }


        /// <summary>
        /// 自增ID
        /// </summary>
        /// <param name="id">自增ID</param>
        /// <param name="flowcardLibrary"></param>
        /// <returns></returns>
        // PUT: api/FlowcardLibrary/Id/5
        [HttpPut("Id/{id}")]
        public Result PutFlowcardLibrary([FromRoute] int id, [FromBody] FlowcardLibrary flowcardLibrary)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryNotExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @FlowCardName;", new { flowcardLibrary.FlowCardName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryIsExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE `Id` = @ProductionProcessId;", new { flowcardLibrary.ProductionProcessId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` = @RawMateriaId;", new { flowcardLibrary.RawMateriaId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
            }

            flowcardLibrary.Id = id;
            flowcardLibrary.CreateUserId = Request.GetIdentityInformation();
            flowcardLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE flowcard_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `FlowCardName` = @FlowCardName, " +
                "`ProductionProcessId` = @ProductionProcessId, `RawMateriaId` = @RawMateriaId, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, `InboundNum` = @InboundNum, " +
                "`Remarks` = @Remarks WHERE `Id` = @Id;", flowcardLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 流程卡号
        /// </summary>
        /// <param name="flowCardName">流程卡号</param>
        /// <param name="flowcardLibrary"></param>
        /// <returns></returns>
        // PUT: api/FlowcardLibrary/FlowCardName/5
        [HttpPut("FlowCardName/{flowCardName}")]
        public Result PutFlowcardLibrary([FromRoute] string flowCardName, [FromBody] FlowcardLibrary flowcardLibrary)
        {
            var data =
                ServerConfig.FlowcardDb.Query<FlowcardLibrary>("SELECT `Id` FROM `flowcard_library` WHERE FlowCardName = @flowCardName;", new { flowCardName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @FlowCardName;", new { flowcardLibrary.FlowCardName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (flowcardLibrary.FlowCardName.IsNullOrEmpty() && data.FlowCardName != flowcardLibrary.FlowCardName)
                {
                    return Result.GenError<Result>(Error.FlowcardLibraryIsExist);
                }
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE `Id` = @ProductionProcessId;", new { flowcardLibrary.ProductionProcessId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionProcessLibraryNotExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` = @RawMateriaId;", new { flowcardLibrary.RawMateriaId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
            }

            flowcardLibrary.Id = data.Id;
            flowcardLibrary.CreateUserId = Request.GetIdentityInformation();
            flowcardLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE flowcard_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `FlowCardName` = @FlowCardName, " +
                "`ProductionProcessId` = @ProductionProcessId, `RawMateriaId` = @RawMateriaId, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, `InboundNum` = @InboundNum, " +
                "`Remarks` = @Remarks WHERE `Id` = @Id;", flowcardLibrary);

            return Result.GenError<Result>(Error.Success);
        }






        // POST: api/FlowcardLibrary
        [HttpPost]
        public Result PostFlowcardLibrary([FromBody] FlowcardLibrary flowcardLibrary)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @FlowCardName;", new { flowcardLibrary.FlowCardName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryIsExist);
            }

            cnt =
              ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE `Id` = @ProductionProcessId;", new { flowcardLibrary.ProductionProcessId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` = @RawMateriaId;", new { flowcardLibrary.RawMateriaId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            flowcardLibrary.CreateUserId = Request.GetIdentityInformation();
            flowcardLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks);",
                flowcardLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FlowcardLibrary/FlowcardLibraries
        [HttpPost("FlowcardLibraries")]
        public Result PostFlowcardLibraries([FromBody] List<FlowcardLibrary> flowcardLibraries)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName;", new
                {
                    FlowCardName = flowcardLibraries.GroupBy(x => x.FlowCardName).Select(x => x.Key)
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryIsExist);
            }

            var productionProcessIds = flowcardLibraries.GroupBy(x => x.ProductionProcessId).Select(x => x.Key);
            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `production_process_library` WHERE `Id` IN @ProductionProcessId;", new
                {
                    ProductionProcessId = productionProcessIds
                }).FirstOrDefault();
            if (cnt != productionProcessIds.Count())
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            var rawMateriaIds = flowcardLibraries.GroupBy(x => x.RawMateriaId).Select(x => x.Key);
            cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` IN @RawMateriaId;", new
                {
                    RawMateriaId = rawMateriaIds
                }).FirstOrDefault();
            if (cnt != rawMateriaIds.Count())
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            foreach (var flowcardLibrary in flowcardLibraries)
            {
                flowcardLibrary.CreateUserId = Request.GetIdentityInformation();
                flowcardLibrary.MarkedDateTime = DateTime.Now;
            }

            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks);",
                flowcardLibraries.OrderBy(x => x.FlowCardName));

            return Result.GenError<Result>(Error.Success);
        }






        /// <summary>
        /// 自增ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/FlowcardLibrary/5
        [HttpDelete("Id/{id}")]
        public Result DeleteFlowcardLibraryById([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
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
        // DELETE: api/FlowcardLibrary/5
        [HttpDelete("FlowCardName/{flowCardName}")]
        public Result DeleteFlowcardLibraryByFlowCardName([FromRoute] string flowCardName)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @flowCardName;", new { flowCardName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowcardLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE FlowCardName = @flowCardName;", new
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
        // DELETE: api/FlowcardLibrary/ProductionProcessName/5
        [HttpDelete("ProductionProcessName/{productionProcessName}")]
        public Result DeleteFlowcardLibraryByProductionProcessName([FromRoute] string productionProcessName)
        {
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE ProductionProcessId = @productionProcessId;", new
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
        // DELETE: api/FlowcardLibrary/RawMateriaName/5
        [HttpDelete("RawMateriaName/{rawMateriaName}")]
        public Result DeleteFlowcardLibraryByRawMateriaName([FromRoute] string rawMateriaName)
        {
            var data =
                ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `flowcard_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE RawMateriaId = @rawMateriaId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    rawMateriaId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}