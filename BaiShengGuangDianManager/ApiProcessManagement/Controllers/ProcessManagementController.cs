using ApiProcessManagement.Base.Server;
using ApiProcessManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiProcessManagement.Controllers
{
    /// <summary>
    /// 工艺编号
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ProcessManagementController : ControllerBase
    {

        /// <summary>
        /// 按工艺编号显示
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/ProcessNumber
        [HttpGet("ProcessNumber")]
        public DataResult GetProcessManagementByProcessNumber()
        {
            var result = new DataResult();
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE MarkedDelete = 0;");
            if (processManagements.Any())
            {
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);

                foreach (var processManagement in processManagements)
                {
                    if (!processManagement.DeviceModels.IsNullOrEmpty())
                    {
                        var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                        processManagement.ModelName = deviceModels.Where(x => deviceModelList.Contains(x.Key)).Select(x => x.Value.ModelName).Join(",");
                    }
                    if (!processManagement.ProductModels.IsNullOrEmpty())
                    {
                        var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                        processManagement.ProductionProcessName = productionProcessLibraries.Where(x => productModelList.Contains(x.Key)).Select(x => x.Value.ProductionProcessName).Join(",");
                    }
                    if (!processManagement.DeviceIds.IsNullOrEmpty())
                    {
                        var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                        processManagement.Code = deviceLibraries.Where(x => deviceIdList.Contains(x.Key)).Select(x => x.Value.Code).Join(",");
                    }
                }
            }

            result.datas.AddRange(processManagements);
            return result;
        }

        /// <summary>
        /// 按产品型号显示
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/ProductionProcessName
        [HttpGet("ProductionProcessName")]
        public DataResult GetProcessManagementByProductionProcessName()
        {
            var result = new DataResult();
            var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProcessManagementDetail>("SELECT * FROM `production_library` WHERE MarkedDelete = 0;");
            var res = new List<ProcessManagementDetail>();
            if (productionProcessLibraries.Any())
            {
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT * FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND ProductModels != '';");

                foreach (var productionProcessLibrary in productionProcessLibraries)
                {
                    var processNumberList = processManagements.Where(x => x.ProductModels.Split(',').Select(int.Parse).Contains(productionProcessLibrary.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }
                    productionProcessLibrary.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");

                    //设备型号
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.DeviceModels.IsNullOrEmpty())
                        {
                            var tdeviceModelList = processNumber.DeviceModels.Split(',').Select(int.Parse);
                            productionProcessLibrary.ModelName = deviceModels.Where(x => tdeviceModelList.Contains(x.Key)).Select(x => x.Value.ModelName).Join(",");
                        }
                    }

                    //机台号
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.DeviceIds.IsNullOrEmpty())
                        {
                            var codeList = processNumber.DeviceIds.Split(',').Select(int.Parse);
                            productionProcessLibrary.Code = deviceLibraries.Where(x => codeList.Contains(x.Key)).Select(x => x.Value.Code).Join(",");
                        }
                    }
                    res.Add(productionProcessLibrary);
                }
            }

            result.datas.AddRange(res);
            return result;
        }

        /// <summary>
        /// 指定产品型号的工艺列表
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/ProductionProcessName
        [HttpGet("ProductionProcessName/{id}")]
        public DataResult GetProcessManagementByProductionProcessName([FromRoute] int id)
        {
            var result = new DataResult();
            var productionProcessLibrary = ServerConfig.FlowCardDb.Query<dynamic>("SELECT * FROM `production_library` WHERE `Id` = @id AND MarkedDelete = 0", new { id }).FirstOrDefault();
            if (productionProcessLibrary != null)
            {
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND FIND_IN_SET(@id, ProductModels);", new { id });
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);

                foreach (var processManagement in processManagements)
                {
                    //机台号
                    IEnumerable<int> codes = null;
                    if (!processManagement.DeviceIds.IsNullOrEmpty())
                    {
                        var codeList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                        processManagement.Code = deviceLibraries.Where(x => codeList.Contains(x.Key)).Select(x => x.Value.Code).Join(",");
                    }

                }
                result.datas.AddRange(processManagements);
            }

            return result;
        }

        /// <summary>
        /// 按设备型号显示
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/ModelName
        [HttpGet("ModelName")]
        public DataResult GetProcessManagementByModelName()
        {
            var result = new DataResult();
            var deviceModels = ServerConfig.DeviceDb.Query<ProcessManagementDetail>("SELECT * FROM `device_model` WHERE MarkedDelete = 0;");
            var res = new List<ProcessManagementDetail>();
            if (deviceModels.Any())
            {
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;");
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND DeviceModels != '';");
                var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);

                foreach (var deviceModel in deviceModels)
                {
                    var processNumberList = processManagements.Where(x => x.DeviceModels.Split(',').Select(int.Parse).Contains(deviceModel.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }

                    deviceModel.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");

                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.ProductModels.IsNullOrEmpty())
                        {
                            var productModelList = processNumber.ProductModels.Split(',').Select(int.Parse);
                            deviceModel.ProductionProcessName = productionProcessLibraries.Where(x => productModelList.Contains(x.Key)).Select(x => x.Value.ProductionProcessName).Join(",");
                        }
                    }

                    deviceModel.Code = deviceLibraries.Where(x => processNumberList.Any(y => y.DeviceIds.Split(",").Select(int.Parse).Contains(x.Id))).Select(x => x.Code).Join(",");
                    res.Add(deviceModel);
                }
            }

            result.datas.AddRange(res);
            return result;
        }

        /// <summary>
        /// 指定设备型号的工艺列表
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/ModelName
        [HttpGet("ModelName/{id}")]
        public DataResult GetProcessManagementByModelName([FromRoute] int id)
        {
            var result = new DataResult();
            var deviceModel = ServerConfig.DeviceDb.Query<dynamic>("SELECT * FROM `device_model` WHERE `Id` = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (deviceModel != null)
            {
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND FIND_IN_SET(@id, DeviceModels);", new { id });
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var res = new Dictionary<int, List<string>>();
                foreach (var processManagement in processManagements)
                {
                    //机台号
                    if (!processManagement.DeviceIds.IsNullOrEmpty())
                    {
                        var codeList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                        var deviceLibrariesList = deviceLibraries.Where(x => codeList.Contains(x.Key));
                        foreach (var pair in deviceLibrariesList)
                        {
                            var key = pair.Key;
                            if (!res.ContainsKey(key))
                            {
                                res.Add(key, new List<string>());
                            }
                            res[key].Add(processManagement.ProcessNumber);
                        }
                    }
                }
                result.datas.AddRange(res.OrderBy(x => x.Key).Select(x => new ProcessManagementDetail
                {
                    Id = x.Key,
                    Code = deviceLibraries[x.Key].Code,
                    ProcessNumber = x.Value.Distinct().Join(",")
                }));
            }

            return result;
        }

        /// <summary>
        /// 按机台号显示
        /// </summary>
        /// <returns></returns>
        // GET: api/ProcessManagement/Code
        [HttpGet("Code")]
        public DataResult GetProcessManagementByCode()
        {
            var result = new DataResult();
            var deviceLibraries = ServerConfig.DeviceDb.Query<ProcessManagementDetail>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;");
            var res = new List<ProcessManagementDetail>();
            if (deviceLibraries.Any())
            {
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND DeviceIds != '';");
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);

                foreach (var deviceLibrary in deviceLibraries)
                {
                    var processNumberList = processManagements.Where(x => x.DeviceIds.Split(',').Select(int.Parse).Contains(deviceLibrary.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }
                    deviceLibrary.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");
                    deviceLibrary.ModelName = deviceModels.ContainsKey(deviceLibrary.Id) ? deviceModels[deviceLibrary.Id].ModelName : "";

                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.ProductModels.IsNullOrEmpty())
                        {
                            var productModelList = processNumber.ProductModels.Split(',').Select(int.Parse);
                            deviceLibrary.ProductionProcessName = productionProcessLibraries.Where(x => productModelList.Contains(x.Key)).Select(x => x.Value.ProductionProcessName).Join(",");
                        }
                    }
                    res.Add(deviceLibrary);
                }
            }

            result.datas.AddRange(res);
            return result;
        }

        /// <summary>
        /// 指定机台号的工艺列表
        /// </summary>
        /// <returns></returns>
            // GET: api/ProcessManagement/Code
        [HttpGet("Code/{id}")]
        public DataResult GetProcessManagementByCode([FromRoute] int id)
        {
            var result = new DataResult();
            var deviceLibrary = ServerConfig.DeviceDb.Query<dynamic>("SELECT a.*, IFNULL(b.ModelName, '') ModelName FROM `device_library` a JOIN `device_model` b ON a.DeviceModelId = b.Id WHERE a.`Id` = @id AND a.MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (deviceLibrary != null)
            {
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE MarkedDelete = 0 AND FIND_IN_SET(@id, DeviceIds);", new { id });
                var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                var res = new Dictionary<int, List<string>>();
                foreach (var processManagement in processManagements)
                {
                    if (!processManagement.ProductModels.IsNullOrEmpty())
                    {
                        var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                        var productionProcessLibrariesList = productionProcessLibraries.Where(x => productModelList.Contains(x.Key));
                        foreach (var pair in productionProcessLibrariesList)
                        {
                            var key = pair.Key;
                            if (!res.ContainsKey(key))
                            {
                                res.Add(key, new List<string>());
                            }
                            res[key].Add(processManagement.ProcessNumber);
                        }
                    }
                }
                result.datas.AddRange(res.Select(x => new ProcessManagementDetail
                {
                    Id = x.Key,
                    ModelName = deviceLibrary.ModelName,
                    ProductionProcessName = productionProcessLibraries[x.Key].ProductionProcessName,
                    ProcessNumber = x.Value.Distinct().Join(",")
                }));
            }

            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/ProcessManagement/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetProcessManagement([FromRoute] int id)
        {
            var result = new DataResult();
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE Id = @id AND MarkedDelete = 0;", new { id });
            if (!processManagements.Any())
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var deviceLibraries = ServerConfig.DeviceDb
                .Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
            var deviceModels = ServerConfig.DeviceDb
                .Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
            var productionProcessLibraries = ServerConfig.FlowCardDb
                .Query<ProductionLibrary>(
                    "SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;")
                .ToDictionary(x => x.Id);

            foreach (var processManagement in processManagements)
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    processManagement.ModelName = deviceModels.Where(x => deviceModelList.Contains(x.Key)).Select(x => x.Value.ModelName).Join(",");
                }

                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    processManagement.ProductionProcessName = productionProcessLibraries.Where(x => productModelList.Contains(x.Key)).Select(x => x.Value.ProductionProcessName).Join(",");
                }

                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    processManagement.Code = deviceLibraries.Where(x => deviceIdList.Contains(x.Key)).Select(x => x.Value.Code).Join(",");
                }
            }

            result.datas.AddRange(processManagements);
            return result;
        }

        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="processNumber">工艺编号</param>
        /// <returns></returns>
        // GET: api/ProcessManagement/ProcessNumber/5
        [HttpGet("ProcessNumber/{processNumber}")]
        public DataResult GetProcessManagement([FromRoute] string processNumber)
        {
            var result = new DataResult();
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE ProcessNumber = @processNumber AND MarkedDelete = 0;", new { processNumber });
            if (!processManagements.Any())
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var deviceLibraries = ServerConfig.DeviceDb
                .Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
            var deviceModels = ServerConfig.DeviceDb
                .Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
            var productionProcessLibraries = ServerConfig.FlowCardDb
                .Query<ProductionLibrary>(
                    "SELECT `Id`, ProductionProcessName FROM `production_library` WHERE MarkedDelete = 0;")
                .ToDictionary(x => x.Id);

            foreach (var processManagement in processManagements)
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    processManagement.ModelName = deviceModels.Where(x => deviceModelList.Contains(x.Key)).Select(x => x.Value.ModelName).Join(",");
                }

                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    processManagement.ProductionProcessName = productionProcessLibraries.Where(x => productModelList.Contains(x.Key)).Select(x => x.Value.ProductionProcessName).Join(",");
                }

                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    processManagement.Code = deviceLibraries.Where(x => deviceIdList.Contains(x.Key)).Select(x => x.Value.Code).Join(",");
                }
            }

            result.datas.AddRange(processManagements);
            return result;
        }




        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="processManagement"></param>
        /// <returns></returns>
        // PUT: api/ProcessManagement/Id
        [HttpPut("Id")]
        public Result PutProcessManagement([FromBody] ProcessManagement processManagement)
        {
            var datas =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE MarkedDelete = 0;");
            if (datas.All(x => x.Id != processManagement.Id))
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            if (datas.Any(x => x.ProcessNumber == processManagement.ProcessNumber && x.Id != processManagement.Id))
            {
                return Result.GenError<DataResult>(Error.ProcessManagementIsExist);
            }

            datas = datas.Where(x => x.Id != processManagement.Id);
            try
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                else
                {
                    processManagement.DeviceModels = "";
                }

                Dictionary<int, ProductionLibrary> productionProcessLibraries = new Dictionary<int, ProductionLibrary>();
                IEnumerable<int> productModelList = null;
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionLibraryNotExist);
                    }
                }
                else
                {
                    processManagement.ProductModels = "";
                }
                Dictionary<int, DeviceLibrary> deviceLibraries = new Dictionary<int, DeviceLibrary>();
                IEnumerable<int> deviceIdList = null;
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    if (deviceIdList.Any(x => !deviceLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceNotExist);
                    }
                }
                else
                {
                    processManagement.DeviceIds = "";
                }

                if (!processManagement.ProductModels.IsNullOrEmpty() && !processManagement.DeviceIds.IsNullOrEmpty())
                {
                    foreach (var productModel in productModelList)
                    {
                        var processManagements = datas.Where(x => x.ProductModelList.Contains(productModel));
                        if (processManagements.Any(x => x.DeviceIdList.Any(y => deviceIdList.Contains(y))))
                        {
                            return Result.GenError<Result>(Error.ProcessManagementAddError);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Result.GenError<Result>(Error.Fail);

            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            processManagement.CreateUserId = createUserId;
            processManagement.MarkedDateTime = time;
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_management SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `ProcessNumber` = @ProcessNumber, `DeviceModels` = @DeviceModels, `ProductModels` = @ProductModels, " +
                "`DeviceIds` = @DeviceIds WHERE `Id` = @Id;", processManagement);

            var processDatas = processManagement.ProcessDatas;
            foreach (var processData in processDatas)
            {
                processData.ProcessManagementId = processManagement.Id;
                processData.CreateUserId = createUserId;
                processData.MarkedDateTime = time;
            }
            var exist = ServerConfig.ProcessDb.Query<ProcessData>("SELECT * FROM `process_data` WHERE MarkedDelete = 0 AND ProcessManagementId = @ProcessManagementId;",
                new { ProcessManagementId = processManagement.Id });
            ServerConfig.ProcessDb.Execute(
                "INSERT INTO process_data (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessManagementId`, `ProcessOrder`, `PressurizeMinute`, `PressurizeSecond`, `Pressure`, `ProcessMinute`, `ProcessSecond`, `Speed`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessManagementId, @ProcessOrder, @PressurizeMinute, @PressurizeSecond, @Pressure, @ProcessMinute, @ProcessSecond, @Speed);",
                    processDatas.Where(x => x.Id == 0));

            var update = processDatas.Where(x => x.Id != 0 && exist.Any(y => y.Id == x.Id 
                                                                             && (y.ProcessOrder != x.ProcessOrder || 
                                                                                 y.PressurizeMinute != x.PressurizeMinute || 
                                                                                 y.PressurizeSecond != x.PressurizeSecond || 
                                                                                 y.ProcessMinute != x.ProcessMinute || 
                                                                                 y.ProcessSecond != x.ProcessSecond || 
                                                                                 y.Pressure != x.Pressure ||
                                                                                 y.Speed != x.Speed))).ToList();
            update.AddRange(exist.Where(x => processDatas.All(y => x.Id != y.Id)).Select(x =>
            {
                x.MarkedDateTime = DateTime.Now;
                x.MarkedDelete = true;
                return x;
            }));
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_data SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ProcessManagementId` = @ProcessManagementId, `ProcessOrder` = @ProcessOrder, `PressurizeMinute` = @PressurizeMinute, `PressurizeSecond` = @PressurizeSecond, " +
                "`Pressure` = @Pressure, `ProcessMinute` = @ProcessMinute, `ProcessSecond` = @ProcessSecond, `Speed` = @Speed WHERE `Id` = @Id;", update);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="processNumber">工艺编号</param>
        /// <param name="processManagement"></param>
        /// <returns></returns>
        // PUT: api/ProcessManagement/ProcessNumber/5
        [HttpPut("ProcessNumber/{processNumber}")]
        public Result PutProcessManagement([FromRoute] string processNumber, [FromBody] ProcessManagement processManagement)
        {
            var data =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `ProcessNumber` = @processNumber AND MarkedDelete = 0;", new { processNumber }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `ProcessNumber` = @ProcessNumber AND MarkedDelete = 0;", new { processManagement.ProcessNumber }).FirstOrDefault();
            if (cnt == 0)
            {
                if (processManagement.ProcessNumber.IsNullOrEmpty() && data.ProcessNumber != processManagement.ProcessNumber)
                {
                    return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
                }
            }

            try
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionLibraryNotExist);
                    }
                }
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    if (deviceIdList.Any(x => !deviceLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceNotExist);
                    }
                }
            }
            catch (Exception)
            {
                return Result.GenError<Result>(Error.Fail);

            }

            processManagement.Id = data.Id;
            processManagement.CreateUserId = Request.GetIdentityInformation();
            processManagement.MarkedDateTime = DateTime.Now;
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_management SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `ProcessNumber` = @ProcessNumber, `DeviceModels` = @DeviceModels, `ProductModels` = @ProductModels, " +
                "`DeviceIds` = @DeviceIds WHERE `Id` = @Id;", processManagement);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 指定机台号添加工艺编号
        /// </summary>
        /// <param name="processManagements">指定机台号ID添加工艺编号</param>
        /// <returns></returns>
        // PUT: api/ProcessManagement/DeviceId
        [HttpPut("DeviceId")]
        public Result PutProcessManagementByDeviceId([FromBody] List<ProcessManagement> processManagements)
        {
            var datas =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `Id` IN @Id AND MarkedDelete = 0;", new { Id = processManagements.Select(x => x.Id) });
            if (!datas.Any() || datas.Count() != processManagements.Count)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            var deviceLibraries = ServerConfig.DeviceDb.Query<string>("SELECT Id FROM `device_library` WHERE MarkedDelete = 0;");
            var processManagementsDict = processManagements.ToDictionary(x => x.Id);
            foreach (var data in datas)
            {
                data.CreateUserId = Request.GetIdentityInformation();
                data.MarkedDateTime = DateTime.Now;
                var deviceId = processManagementsDict[data.Id].DeviceIds;
                if (!deviceLibraries.Contains(deviceId))
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }
                if (!data.DeviceIds.IsNullOrEmpty())
                {
                    var deviceIds = data.DeviceIds.Split(',').ToList();
                    if (deviceIds.Contains(deviceId))
                    {
                        return Result.GenError<DataResult>(Error.ProcessManagementIsExist);
                    }

                    deviceIds.Add(deviceId);
                    data.DeviceIds = deviceIds.Distinct().OrderBy(x => x).ToJSON();
                }
                else
                {
                    data.DeviceIds = deviceId;
                }
            }
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_management SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `DeviceIds` = @DeviceIds WHERE `Id` = @Id;", datas);

            return Result.GenError<Result>(Error.Success);
        }



        public class Request1
        {
            public int Pid = 0;
            public string DeviceModelIds;
            public string ProductionProcessIds;
        }
        /// <summary>
        /// 添加工艺编号时根据产品型号获取可添加设备
        /// </summary>
        /// <returns></returns>
        // Post: api/ProcessManagement/ProductionProcessName/DeviceId
        [HttpPost("ProductionProcessName/DeviceId")]
        public DataResult GetProcessManagementByProductionProcessNameToDeviceId([FromBody] Request1 request1)
        {
            var result = new DataResult();
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>(
                "SELECT * FROM `process_management` WHERE MarkedDelete = 0" + (request1.Pid == 0 ? ";" : " AND Id != @id;"), new { id = request1.Pid });
            if (!processManagements.Any())
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            try
            {
                var productionProcessIdList = request1.ProductionProcessIds.Split(",").Select(int.Parse);
                var deviceModelIdList = request1.DeviceModelIds.Split(",").Select(int.Parse);
                var existIds = processManagements.Where(x => x.ProductModels.Split(",").Select(int.Parse).Any(y => productionProcessIdList.Contains(y)));
                var existId = existIds.Select(x => x.DeviceIds.Split(",")).SelectMany(x => x).Distinct();
                var deviceIds = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM device_library WHERE Id NOT IN @id AND MarkedDelete = 0;", new { id = existId });
                result.datas.AddRange(deviceIds.Where(x => deviceModelIdList.Contains(x.DeviceModelId)));
            }
            catch (Exception)
            {
                // ignored
            }

            return result;
        }

        //public class Request2
        //{
        //    public string DeviceModelIds;
        //    public string ProductionProcessIds;
        //}
        ///// <summary>
        ///// 添加工艺编号时根据设备获取可添加产品型号
        ///// </summary>
        ///// <returns></returns>
        //// GET: api/ProcessManagement/DeviceId/ProductionProcessName
        //[HttpGet("DeviceId/ProductionProcessName/{deviceId}")]
        //public DataResult GetProcessManagementByDeviceIdToProductionProcessName([FromRoute] int deviceId)
        //{
        //    var result = new DataResult();
        //    var productModels = ServerConfig.ProcessDb.Query<string>(
        //        "SELECT IFNULL(GROUP_CONCAT(ProductModels), '') FROM `process_management` WHERE FIND_IN_SET(@deviceId, DeviceIds) AND MarkedDelete = 0;", new { deviceId });

        //    result.datas.AddRange(ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT * FROM production_library WHERE Id NOT IN @id AND MarkedDelete = 0;", new { id = productModels }));
        //    return result;
        //}

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="processManagement"></param>
        /// <returns></returns>
        // POST: api/ProcessManagement
        [HttpPost]
        public Result PostProcessManagement([FromBody] ProcessManagement processManagement)
        {
            var datas =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE MarkedDelete = 0;");
            if (datas.Any(x => x.ProcessNumber == processManagement.ProcessNumber))
            {
                return Result.GenError<DataResult>(Error.ProcessManagementIsExist);
            }

            try
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                else
                {
                    processManagement.DeviceModels = "";
                }

                Dictionary<int, ProductionLibrary> productionProcessLibraries = new Dictionary<int, ProductionLibrary>();
                IEnumerable<int> productModelList = null;
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    productionProcessLibraries = ServerConfig.FlowCardDb.Query<ProductionLibrary>("SELECT `Id` FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionLibraryNotExist);
                    }
                }
                else
                {
                    processManagement.ProductModels = "";
                }
                Dictionary<int, DeviceLibrary> deviceLibraries = new Dictionary<int, DeviceLibrary>();
                IEnumerable<int> deviceIdList = null;
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.Id);
                    deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    if (deviceIdList.Any(x => !deviceLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceNotExist);
                    }
                }
                else
                {
                    processManagement.DeviceIds = "";
                }

                if (!processManagement.ProductModels.IsNullOrEmpty() && !processManagement.DeviceIds.IsNullOrEmpty())
                {
                    foreach (var productModel in productModelList)
                    {
                        var processManagements = datas.Where(x => x.ProductModelList.Contains(productModel));
                        if (processManagements.Any(x => x.DeviceIdList.Any(y => deviceIdList.Contains(y))))
                        {
                            return Result.GenError<Result>(Error.ProcessManagementAddError);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Result.GenError<Result>(Error.Fail);

            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            processManagement.CreateUserId = createUserId;
            processManagement.MarkedDateTime = time;
            var index = ServerConfig.ProcessDb.Query<int>(
                "INSERT INTO process_management (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessNumber`, `DeviceModels`, `ProductModels`, `DeviceIds`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessNumber, @DeviceModels, @ProductModels, @DeviceIds);SELECT LAST_INSERT_ID();",
                processManagement).FirstOrDefault();

            var processDatas = processManagement.ProcessDatas;
            foreach (var processData in processDatas)
            {
                processData.ProcessManagementId = index;
                processData.CreateUserId = createUserId;
                processData.MarkedDateTime = time;
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
        // DELETE: api/ProcessManagement/Id/5
        [HttpDelete("Id/{id}")]
        public Result DeleteProcessManagement([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessManagementNotExist);
            }

            ServerConfig.ProcessDb.Execute(
                "UPDATE `process_management` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });


            ServerConfig.ProcessDb.Execute(
                "UPDATE `process_data` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProcessManagementId`= @ProcessManagementId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    ProcessManagementId = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 工艺编号
        /// </summary>
        /// <param name="processNumber"></param>
        /// <returns></returns>
        // DELETE: api/ProcessManagement/ProcessNumber/5
        [HttpDelete("ProcessNumber/{processNumber}")]
        public Result DeleteProcessManagement([FromRoute] string processNumber)
        {
            var data =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `ProcessNumber` = @processNumber AND MarkedDelete = 0;", new { processNumber }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            ServerConfig.ProcessDb.Execute(
                "UPDATE `process_management` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ProcessNumber` = @processNumber;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    processNumber
                });

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