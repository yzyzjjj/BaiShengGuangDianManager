using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class FlowCardLibraryController : ControllerBase
    {
        // Post: api/FlowCardLibrary
        [HttpGet]
        public DataResult GetFlowCardLibrary([FromQuery]string flowCardName, DateTime startTime, DateTime endTime, string productionProcessName)
        {
            var result = new DataResult();
            var sql = "";
            var id = 0;
            if (!flowCardName.IsNullOrEmpty() && startTime != default(DateTime) && endTime != default(DateTime) && !productionProcessName.IsNullOrEmpty())
            {
                id =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
                if (id == 0)
                {
                    return Result.GenError<DataResult>(Error.ProductionLibraryNotExist);
                }

                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE FlowCardName = @FlowCardName AND ProductionProcessId = @ProductionProcessId AND CreateTime >= @StartTime AND CreateTime <= @EndTime ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (startTime != default(DateTime) && endTime != default(DateTime) && !productionProcessName.IsNullOrEmpty())
            {
                id =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
                if (id == 0)
                {
                    return Result.GenError<DataResult>(Error.ProductionLibraryNotExist);
                }

                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE ProductionProcessId = @ProductionProcessId AND CreateTime >= @StartTime AND CreateTime <= @EndTime ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (!flowCardName.IsNullOrEmpty() && startTime != default(DateTime) && endTime != default(DateTime))
            {
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, " +
                    "d.DeviceId, e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE FlowCardName = @FlowCardName AND CreateTime >= @StartTime AND CreateTime <= @EndTime ) a " +
                    "LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, " +
                    "ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, " +
                    "b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = " +
                    "0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN " +
                    "`flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (!flowCardName.IsNullOrEmpty() && !productionProcessName.IsNullOrEmpty())
            {
                id =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
                if (id == 0)
                {
                    return Result.GenError<DataResult>(Error.ProductionLibraryNotExist);
                }
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM " +
                    "( SELECT * FROM `flowcard_library` WHERE FlowCardName = @FlowCardName AND ProductionProcessId = @ProductionProcessId ) a LEFT JOIN `production_library` b ON a.ProductionProcessId " +
                    "= b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName," +
                    " QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` " +
                    "b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 " +
                    "00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (!flowCardName.IsNullOrEmpty())
            {
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM " +
                    "( SELECT * FROM `flowcard_library` WHERE FlowCardName = @FlowCardName) a LEFT JOIN `production_library` b " +
                    "ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, " +
                    "b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` " +
                    "a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || " +
                    "ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = " +
                    "e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (startTime != default(DateTime) && endTime != default(DateTime))
            {
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM " +
                    "( SELECT * FROM `flowcard_library` WHERE CreateTime >= @StartTime AND CreateTime <= @EndTime ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = " +
                    "b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, " +
                    "b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN " +
                    "`device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || " +
                    "ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON " +
                    "a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else if (!productionProcessName.IsNullOrEmpty())
            {
                id =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
                if (id == 0)
                {
                    return Result.GenError<DataResult>(Error.ProductionLibraryNotExist);
                }
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, " +
                    "e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE ProductionProcessId = @ProductionProcessId ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = " +
                    "b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, " +
                    "QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` " +
                    "b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 " +
                    "00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }
            else
            {
                sql =
                    "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM `flowcard_library` " +
                    "a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, " +
                    "ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, " +
                    "b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = " +
                    "0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` " +
                    "e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;";
            }

            var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(sql,
                new
                {
                    StartTime = startTime.DayBeginTime(),
                    EndTime = endTime.DayEndTime(),
                    ProductionProcessId = id,
                    FlowCardName = flowCardName
                }).OrderByDescending(x => x.CreateTime);

            var device = datas.Select(x => x.DeviceId);
            if (device.Any())
            {
                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = device });

                foreach (var data in datas)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                    if (code != null)
                    {
                        data.Code = code.Code;
                    }
                }
            }
            result.datas.AddRange(datas.OrderByDescending(x => x.Id));
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
                ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.RawMateriaName, c.ProductionProcessName FROM `flowcard_library` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id JOIN `production_library` c ON a.ProductionProcessId = c.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProductionLibraryNotExist;
                return result;
            }
            data.Specifications.AddRange(ServerConfig.ApiDb.Query<FlowCardSpecification>("SELECT * FROM `flowcard_specification` WHERE FlowCardId = @FlowCardId AND MarkedDelete = 0;", new
            {
                FlowCardId = data.Id
            }));
            data.ProcessSteps.AddRange(ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT a.*, IFNULL(b.ProcessorName, '') ProcessorName, IFNULL(c.SurveyorName, '') " +
                "SurveyorName FROM `flowcard_process_step` a LEFT JOIN `processor` b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id WHERE FlowCardId = @FlowCardId AND a.MarkedDelete = 0;", new
                {
                    FlowCardId = data.Id
                }));
            var deviceCodeList = data.ProcessSteps.Where(x => x.DeviceId != 0).Select(x => x.DeviceId);
            if (deviceCodeList.Any())
            {
                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = deviceCodeList });

                foreach (var processStep in data.ProcessSteps)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == processStep.DeviceId);
                    if (code != null)
                    {
                        processStep.Code = code.Code;
                    }
                }
            }
            result.datas.Add(data);
            return result;
        }


        /// <summary>
        /// 流程卡号
        /// </summary>
        /// <param name="id">流程卡号</param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/FlowCardName/5
        [HttpGet("FlowCardName/{id}")]
        public DataResult GetFlowCardLibraryByFlowCardName([FromRoute] string id)
        {
            var result = new DataResult();
            var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE FlowCardName = @FlowCardName ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;",
                new { FlowCardName = id });

            var device = datas.Select(x => x.DeviceId);
            if (device.Any())
            {
                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = device });

                foreach (var data in datas)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                    if (code != null)
                    {
                        data.Code = code.Code;
                    }
                }
            }
            result.datas.AddRange(datas.OrderByDescending(x => x.Id));
            return result;
        }

        public class QueryProcessData
        {
            public int Id;
            public string FlowCard;
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
            var id =
                ServerConfig.ApiDb.Query<int>("SELECT Id FROM `production_library` WHERE ProductionProcessName = @productionProcessName;", new { productionProcessName }).FirstOrDefault();
            if (id == 0)
            {
                return Result.GenError<DataResult>(Error.ProductionLibraryNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                "SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.CategoryName, d.StepName, d.ProcessTime, d.QualifiedNumber, d.DeviceId, e.TypeName FROM ( SELECT * FROM `flowcard_library` WHERE ProductionProcessId = @ProductionProcessId ) a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c ON a.RawMateriaId = c.Id LEFT JOIN ( SELECT * FROM ( SELECT FlowCardId, ProcessStepOrder, ProcessStepId, b.CategoryName, b.StepName, QualifiedNumber, ProcessTime, DeviceId FROM `flowcard_process_step` a JOIN ( SELECT a.Id, a.StepName, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) b ON a.ProcessStepId = b.Id WHERE a.MarkedDelete = 0 AND NOT ISNULL(ProcessTime) || ProcessTime = '0001-01-01 00:00:00' ORDER BY ProcessStepOrder DESC ) a GROUP BY a.FlowCardId ) d ON a.Id = d.FlowCardId LEFT JOIN `flowcard_type` e ON a.FlowCardTypeId = e.Id WHERE a.MarkedDelete = 0;",
                new { ProductionProcessId = id });

            var device = datas.Select(x => x.DeviceId);
            if (device.Any())
            {
                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
                    "SELECT Id, `Code` FROM `device_library` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = device });

                foreach (var data in datas)
                {
                    var code = deviceCodes.FirstOrDefault(x => x.Id == data.DeviceId);
                    if (code != null)
                    {
                        data.Code = code.Code;
                    }
                }
            }
            result.datas.AddRange(datas.OrderByDescending(x => x.Id));
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
            }

            var result = new DataResult();
            var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.QualifiedNumber, " +
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

            var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
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
                ServerConfig.ApiDb.Query<Processor>("SELECT Id FROM `processor` WHERE ProcessorName = @ProcessorName AND MarkedDelete = 0;", new { ProcessorName = processorName }).FirstOrDefault();
            if (processor == null)
            {
                return Result.GenError<DataResult>(Error.ProcessorNotExist);
            }

            var productionProcessIds = ServerConfig.ApiDb.Query<ProductionProcessStep>("SELECT ProductionProcessId FROM `production_process_step` WHERE ProcessorId = @ProcessorId AND MarkedDelete = 0; ", new
            {
                ProcessorId = processor.Id
            });

            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.QualifiedNumber, " +
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

                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
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
                ServerConfig.ApiDb.Query<Processor>("SELECT Id FROM `surveyor` WHERE SurveyorName = @SurveyorName AND MarkedDelete = 0;", new { SurveyorName = surveyorName }).FirstOrDefault();
            if (surveyor == null)
            {
                return Result.GenError<DataResult>(Error.SurveyorNotExist);
            }

            var productionProcessIds = ServerConfig.ApiDb.Query<ProductionProcessStep>(
                "SELECT ProductionProcessId FROM `production_process_step` WHERE SurveyorId = @SurveyorId; ", new
                {
                    SurveyorId = surveyor.Id
                });


            var result = new DataResult();
            if (productionProcessIds.Any())
            {
                var datas = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.ProductionProcessName, c.RawMateriaName, d.ProcessStepId, d.QualifiedNumber, " +
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

                var deviceCodes = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>(
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
        /// 加工获取 流程卡
        /// </summary>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/Detail
        [HttpPost("Detail")]
        public object GetFlowCardLibraryDetail([FromBody] FlowCardInfo flowCardInfo)
        {
            var device = ServerConfig.ApiDb.Query<DeviceLibraryDetail>(
                "SELECT a.Id, b.DeviceCategoryId FROM `device_library` a JOIN device_model b ON a.DeviceModelId = b.Id WHERE a.`Id` = @Id;", new { flowCardInfo.Id }).FirstOrDefault();

            if (device == null)
            {
                return Result.GenError<DataResult>(Error.DeviceNotExist);
            }

            var flowCard = ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.Id, a.ProductionProcessId, a.RawMateriaId, a.Priority, b.ProductionProcessName, c.RawMateriaName FROM `flowcard_library` " +
                                                                                "a LEFT JOIN `production_library` b ON a.ProductionProcessId = b.Id LEFT JOIN `raw_materia` c " +
                                                                                "ON a.RawMateriaId = c.Id WHERE a.FlowCardName = @flowCardName AND a.MarkedDelete = 0;",
                new { flowCardName = flowCardInfo.FlowCardName }).FirstOrDefault();
            if (flowCard == null)
            {
                return Result.GenError<DataResult>(Error.FlowCardLibraryNotExist);
            }

            var processNumber = ServerConfig.ApiDb.Query<dynamic>(
                "SELECT Id, ProcessNumber FROM `process_management` " +
                "WHERE FIND_IN_SET(@DeviceId, DeviceIds) AND FIND_IN_SET(@ProductModel, ProductModels) AND MarkedDelete = 0;", new
                {
                    DeviceId = device.Id,
                    ProductModel = flowCard.ProductionProcessId
                }).FirstOrDefault();
            if (processNumber == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            var rawMateriaSpecifications =
                ServerConfig.ApiDb.Query<dynamic>(
                    "SELECT SpecificationName, SpecificationValue FROM `raw_materia_specification` WHERE RawMateriaId = @RawMateriaId AND MarkedDelete = 0;", new { flowCard.RawMateriaId });
            var processData = ServerConfig.ApiDb.Query<ProcessData>(
                "SELECT * FROM `process_data` WHERE ProcessManagementId = @Id AND MarkedDelete = 0;", new { processNumber.Id });


            var processSteps = ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT a.*, IFNULL(b.StepName, '') StepName  FROM `flowcard_process_step` a LEFT JOIN `device_process_step` b ON a.ProcessStepId = b.Id WHERE FlowCardId = @FlowCardId AND a.MarkedDelete = 0;", new
            {
                FlowCardId = flowCard.Id
            }).OrderBy(x => x.ProcessStepOrder);
            var currentProcessSteps = new List<FlowCardProcessStepDetail>();
            var deviceProcessSteps =
                ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT * FROM `device_process_step` WHERE DeviceCategoryId = @DeviceCategoryId AND MarkedDelete = 0;",
                    new { device.DeviceCategoryId }).ToDictionary(x => x.Id);
            if (deviceProcessSteps.Any() && processSteps.Any())
            {
                var currentProcessStep = processSteps.FirstOrDefault(x => deviceProcessSteps.ContainsKey(x.ProcessStepId));
                if (currentProcessStep != null)
                {
                    currentProcessStep.ProcessStepOrderName = "加工工序";
                    var beforeOrder = currentProcessStep.ProcessStepOrder - 1;
                    var beforeProcessStep = processSteps.FirstOrDefault(x => x.ProcessStepOrder == beforeOrder);

                    if (beforeProcessStep != null)
                    {
                        beforeProcessStep.ProcessStepOrderName = "上道工序";
                        currentProcessSteps.Add(beforeProcessStep);
                    }

                    currentProcessSteps.Add(currentProcessStep);
                }
            }
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
                    processData,
                    processSteps = currentProcessSteps
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            flowCardLibrary.Id = id;
            flowCardLibrary.CreateUserId = createUserId;
            flowCardLibrary.MarkedDateTime = time;
            ServerConfig.ApiDb.Execute(
                "UPDATE flowcard_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, `InboundNum` = @InboundNum, " +
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

                var exist = ServerConfig.ApiDb.Query<FlowCardSpecification>("SELECT * FROM `flowcard_specification` " +
                                                                                 "WHERE MarkedDelete = 0 AND FlowCardId = @FlowCardId;", new { FlowCardId = id });
                ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Execute(
                    "UPDATE flowcard_specification SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
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

                var exist = ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT * FROM `flowcard_process_step` " +
                                                                                     "WHERE MarkedDelete = 0 AND FlowCardId = @FlowCardId;", new { FlowCardId = id });

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                    processSteps.Where(x => x.Id == 0).OrderBy(x => x.ProcessStepOrder));


                var update = processSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                                    && (y.ProcessStepOrder != x.ProcessStepOrder || y.ProcessStepId != x.ProcessStepId
                                        || y.ProcessStepRequirements != x.ProcessStepRequirements || y.ProcessStepRequirementMid != x.ProcessStepRequirementMid))).ToList();
                update.AddRange(exist.Where(x => processSteps.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));
                ServerConfig.ApiDb.Execute(
                    "UPDATE flowcard_process_step SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                    "`ProcessStepOrder` = @ProcessStepOrder, `ProcessStepId` = @ProcessStepId, `ProcessStepRequirements` = @ProcessStepRequirements, `ProcessStepRequirementMid` = @ProcessStepRequirementMid " +
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            var processorIds = flowCardProcessSteps.GroupBy(x => x.ProcessorId).Where(x => x.Key != 0).Select(x => x.Key);
            if (processorIds.Any())
            {
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `processor` WHERE `Id` in @ProcessorId AND MarkedDelete = 0;", new { ProcessorId = processorIds }).FirstOrDefault();
                if (cnt == 0 || cnt != processorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.ProcessorNotExist);
                }
            }
            var surveyorIds = flowCardProcessSteps.GroupBy(x => x.SurveyorId).Where(x => x.Key != 0).Select(x => x.Key);
            if (surveyorIds.Any())
            {
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE `Id` in @SurveyorId AND MarkedDelete = 0;", new { SurveyorId = surveyorIds }).FirstOrDefault();
                if (cnt == 0 || cnt != surveyorIds.Count())
                {
                    return Result.GenError<DataResult>(Error.SurveyorNotExist);
                }
            }
            var deviceIds = flowCardProcessSteps.GroupBy(x => x.DeviceId).Where(x => x.Key != 0).Select(x => x.Key);
            if (deviceIds.Any())
            {
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE `Id` in @DeviceId AND MarkedDelete = 0;", new { DeviceId = deviceIds }).FirstOrDefault();
                if (cnt == 0 || cnt != deviceIds.Count())
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
            }
            var exist = ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT * FROM `flowcard_process_step` " +
                                                                            "WHERE MarkedDelete = 0 AND FlowCardId = @FlowCardId;", new { FlowCardId = id });

            var update = flowCardProcessSteps.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id
                         && (y.ProcessorId != x.ProcessorId
                              || y.ProcessTime != x.ProcessTime
                              || y.SurveyorId != x.SurveyorId
                              || y.SurveyTime != x.SurveyTime
                              || y.QualifiedNumber != x.QualifiedNumber
                              || y.UnqualifiedNumber != x.UnqualifiedNumber
                              || y.DeviceId != x.DeviceId))).ToList();

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var processStep in update)
            {
                processStep.CreateUserId = createUserId;
                processStep.MarkedDateTime = time;
                processStep.IsReport = true;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE flowcard_process_step SET `MarkedDateTime` = @MarkedDateTime, `ProcessorId` = @ProcessorId, `ProcessTime` = @ProcessTime, " +
                "`SurveyorId` = @SurveyorId, `SurveyTime` = @SurveyTime, `QualifiedNumber` = @QualifiedNumber, `UnqualifiedNumber` = @UnqualifiedNumber, " +
                "`DeviceId` = @DeviceId, `IsReport` = @IsReport, `QualifiedRange` = @QualifiedRange, `QualifiedMode` = @QualifiedMode WHERE `Id` = @Id;", update);

            return Result.GenError<Result>(Error.Success);
        }
        public class FlowCardInfo
        {
            public int Id;
            public string FlowCardName;
        }

        /// <summary>
        /// 获取加工工序数据
        /// </summary>
        /// <param name="queryProcessData"></param>
        /// <returns></returns>
        // GET: api/FlowCardLibrary/ProcessData
        [HttpPost("ProcessData")]
        public object GetFlowCardLibraryProcessDataById([FromBody] QueryProcessData queryProcessData)
        {
            if (queryProcessData.Id != 0)
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE MarkedDelete = 0 AND Id = @Id;", new { queryProcessData.Id }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.FlowCardLibraryNotExist);
                }
            }
            else if (!queryProcessData.FlowCard.IsNullOrEmpty())
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT Id FROM `flowcard_library` WHERE MarkedDelete = 0 AND FlowCardName = @FlowCard;", new { queryProcessData.FlowCard }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.FlowCardLibraryNotExist);
                }

                queryProcessData.Id = cnt;
            }
            else
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }

            var processSteps =
            ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT a.*, d.CategoryName, d.StepName, d.IsSurvey, IFNULL(b.ProcessorName, '') ProcessorName, " +
                                                                "IFNULL(c.SurveyorName, '') SurveyorName, IFNULL(e.`Code`, '') `Code` FROM `flowcard_process_step` a LEFT JOIN `processor` " +
                                                                "b ON a.ProcessorId = b.Id LEFT JOIN `surveyor` c ON a.SurveyorId = c.Id LEFT JOIN ( SELECT a.Id, " +
                                                                "a.StepName, a.IsSurvey, b.CategoryName FROM `device_process_step` a JOIN `device_category` b " +
                                                                "ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ) d ON a.ProcessStepId = d.Id LEFT JOIN `device_library` " +
                                                                "e ON a.DeviceId = e.Id WHERE FlowCardId = @Id AND a.MarkedDelete = 0;", new { queryProcessData.Id }).OrderBy(x => x.ProcessStepOrder);

            var processors = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, `ProcessorName` FROM `processor` WHERE MarkedDelete = 0;");
            var surveyors = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, `SurveyorName` FROM `surveyor` WHERE MarkedDelete = 0;");
            var deviceIds = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, `Code` FROM `device_library` WHERE MarkedDelete = 0;");

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



        // POST: api/FlowCardLibrary
        [HttpPost]
        public Result PostFlowCardLibrary([FromBody] FlowCardLibrary flowCardLibrary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @FlowCardName AND MarkedDelete = 0;", new { flowCardLibrary.FlowCardName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryIsExist);
            }

            cnt =
              ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE `Id` = @ProductionProcessId AND MarkedDelete = 0;", new { flowCardLibrary.ProductionProcessId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` = @RawMateriaId AND MarkedDelete = 0;", new { flowCardLibrary.RawMateriaId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var productionProcessId = flowCardLibrary.ProductionProcessId;
            var processSteps = ServerConfig.ApiDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });


            var productionProcessSpecifications = ServerConfig.ApiDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            flowCardLibrary.CreateUserId = createUserId;
            flowCardLibrary.MarkedDateTime = time;
            flowCardLibrary.CreateTime = time;
            var index = ServerConfig.ApiDb.Query<int>(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `FlowCardTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @FlowCardTypeId);SELECT LAST_INSERT_ID();",
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
                    ProcessStepId = processStep.ProcessStepId,
                    ProcessStepRequirements = processStep.ProcessStepRequirements,
                    ProcessStepRequirementMid = processStep.ProcessStepRequirementMid,
                });
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `SpecificationName`, `SpecificationValue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @SpecificationName, @SpecificationValue);",
                flowCardLibrary.Specifications.OrderBy(x => x.FlowCardId));

            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                flowCardLibrary.ProcessSteps.OrderBy(x => x.ProcessStepOrder));

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FlowCardLibrary/FlowCardLibraries
        [HttpPost("FlowCardLibraries")]
        public Result PostFlowcardLibraries([FromBody] List<FlowCardLibrary> flowCardLibraries)
        {
            var flowCards = flowCardLibraries.GroupBy(x => x.FlowCardName);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName AND MarkedDelete = 0;", new
                {
                    FlowCardName = flowCards.Select(x => x.Key)
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryIsExist);
            }

            var productionProcessIds = flowCardLibraries.GroupBy(x => x.ProductionProcessId).Select(x => x.Key);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_library` WHERE `Id` IN @ProductionProcessId AND MarkedDelete = 0;", new
                {
                    ProductionProcessId = productionProcessIds
                }).FirstOrDefault();
            if (cnt != productionProcessIds.Count() || cnt != 1)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            var rawMateriaIds = flowCardLibraries.GroupBy(x => x.RawMateriaId).Select(x => x.Key);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE `Id` IN @RawMateriaId AND MarkedDelete = 0;", new
                {
                    RawMateriaId = rawMateriaIds
                }).FirstOrDefault();
            if (cnt != rawMateriaIds.Count())
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var productionProcessId = productionProcessIds.FirstOrDefault();
            var processSteps = ServerConfig.ApiDb.Query<ProductionProcessStep>("SELECT * FROM `production_process_step` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });


            var productionProcessSpecifications = ServerConfig.ApiDb.Query<ProductionSpecification>("SELECT * FROM `production_specification` WHERE ProductionProcessId = @ProductionProcessId AND MarkedDelete = 0;", new
            {
                ProductionProcessId = productionProcessId
            });

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var flowCardLibrary in flowCardLibraries)
            {
                flowCardLibrary.CreateUserId = createUserId;
                flowCardLibrary.MarkedDateTime = time;
                flowCardLibrary.CreateTime = time;
            }

            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `FlowCardTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @FlowCardTypeId);",
                flowCardLibraries.OrderBy(x => x.FlowCardName));

            var insertDatas =
                ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName AND MarkedDelete = 0;", new
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
                        ProcessStepId = processStep.ProcessStepId,
                        ProcessStepRequirements = processStep.ProcessStepRequirements,
                        ProcessStepRequirementMid = processStep.ProcessStepRequirementMid,
                    });
                }
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `SpecificationName`, `SpecificationValue`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @SpecificationName, @SpecificationValue);",
                flowCardLibraries.SelectMany(x => x.Specifications).OrderBy(x => x.FlowCardId));

            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_process_step` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_specification` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`= @Id;", new
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `flowcard_library` WHERE FlowCardName = @flowCardName AND MarkedDelete = 0;", new { flowCardName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE FlowCardName = @flowCardName AND MarkedDelete = 0;", new
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
                ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE ProductionProcessName = @productionProcessName AND MarkedDelete = 0;", new { productionProcessName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE ProductionProcessId = @productionProcessId AND MarkedDelete = 0;", new
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
                ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ProductionLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `flowcard_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE RawMateriaId = @rawMateriaId AND MarkedDelete = 0;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    rawMateriaId = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}