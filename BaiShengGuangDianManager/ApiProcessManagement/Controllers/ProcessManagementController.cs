using ApiProcessManagement.Base.Server;
using ApiProcessManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.ServerConfig.Enum;
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
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management`;");
            if (processManagements.Any())
            {
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library`;").ToDictionary(x => x.Id);
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model`;").ToDictionary(x => x.Id);
                var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id`, ProductionProcessName FROM `production_process_library`;").ToDictionary(x => x.Id);

                foreach (var processManagement in processManagements)
                {
                    if (!processManagement.DeviceModels.IsNullOrEmpty())
                    {
                        var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                        processManagement.ModelName = deviceModelList
                            .Where(x => deviceModels.ContainsKey(x)).Select(x => deviceModels[x].ModelName).Join(",");
                    }
                    if (!processManagement.ProductModels.IsNullOrEmpty())
                    {
                        var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                        processManagement.ProductionProcessName = productModelList
                            .Where(x => productionProcessLibraries.ContainsKey(x)).Select(x => productionProcessLibraries[x].ProductionProcessName).Join(",");
                    }
                    if (!processManagement.DeviceIds.IsNullOrEmpty())
                    {
                        var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                        processManagement.Code = deviceIdList
                            .Where(x => deviceLibraries.ContainsKey(x)).Select(x => deviceLibraries[x].Code).Join(",");
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
        [HttpGet("ProductionProcessName/{productionProcessId}")]
        public DataResult GetProcessManagementByProductionProcessName([FromRoute] int productionProcessId = 0)
        {
            var result = new DataResult();
            var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProcessManagementDetail>("SELECT * FROM `production_process_library`"
                                                                                                    + (productionProcessId == 0 ? ";" : " WHERE `Id` = @productionProcessId"), new { productionProcessId });
            var res = new List<ProcessManagementDetail>();
            if (productionProcessLibraries.Any())
            {
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT * FROM `device_model`;").ToDictionary(x => x.Id);
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library`;").ToDictionary(x => x.Id);
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management`;");

                foreach (var productionProcessLibrary in productionProcessLibraries)
                {
                    var processNumberList = processManagements.Where(x => x.ProductModels.Split(',').Select(int.Parse).Contains(productionProcessLibrary.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }
                    productionProcessLibrary.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");

                    //设备型号
                    IEnumerable<int> deviceModelList = null;
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.DeviceModels.IsNullOrEmpty())
                        {
                            var tdeviceModelList = processNumber.DeviceModels.Split(',').Select(int.Parse)
                                .Where(x => deviceModels.ContainsKey(x)).Select(x => deviceModels[x].Id);
                            deviceModelList = (deviceModelList ?? tdeviceModelList).Union(tdeviceModelList);
                        }
                    }

                    if (deviceModelList != null && deviceModelList.Any())
                    {
                        productionProcessLibrary.ModelName = deviceModelList
                            .Where(x => deviceModels.ContainsKey(x)).Select(x => deviceModels[x].ModelName).Join(",");
                    }

                    //机台号
                    IEnumerable<int> codes = null;
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.DeviceIds.IsNullOrEmpty())
                        {
                            var codeList = processNumber.DeviceIds.Split(',').Select(int.Parse)
                                .Where(x => deviceLibraries.ContainsKey(x)).Select(x => deviceLibraries[x].Id);
                            codes = (codes ?? codeList).Union(codeList);
                        }
                    }

                    if (codes != null && codes.Any())
                    {
                        productionProcessLibrary.Code = codes
                            .Where(x => deviceLibraries.ContainsKey(x)).Select(x => deviceLibraries[x].Code).Join(",");
                    }
                    res.Add(productionProcessLibrary);
                }
            }

            result.datas.AddRange(res);
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
            var deviceModels = ServerConfig.DeviceDb.Query<ProcessManagementDetail>("SELECT * FROM `device_model`;");
            var res = new List<ProcessManagementDetail>();
            if (deviceModels.Any())
            {
                var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library`;");
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management`;");
                var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id`, ProductionProcessName FROM `production_process_library`;").ToDictionary(x => x.Id);

                foreach (var deviceModel in deviceModels)
                {
                    var processNumberList = processManagements.Where(x => x.DeviceModels.Split(',').Select(int.Parse).Contains(deviceModel.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }

                    deviceModel.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");

                    IEnumerable<int> productModels = null;
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.ProductModels.IsNullOrEmpty())
                        {
                            var productModelList = processNumber.ProductModels.Split(',').Select(int.Parse)
                                .Where(x => productionProcessLibraries.ContainsKey(x)).Select(x => productionProcessLibraries[x].Id);
                            productModels = (productModels ?? productModelList).Union(productModelList);
                        }
                    }

                    if (productModels != null && productModels.Any())
                    {
                        deviceModel.ProductionProcessName = productModels
                            .Where(x => productionProcessLibraries.ContainsKey(x)).Select(x => productionProcessLibraries[x].ProductionProcessName).Join(",");
                    }

                    deviceModel.Code = deviceLibraries.Where(x => processNumberList.Any(y => y.DeviceIds.Split(",").Select(int.Parse).Contains(x.Id))).Select(x => x.Code).Join(",");
                    res.Add(deviceModel);
                }
            }

            result.datas.AddRange(res);
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
            var deviceLibraries = ServerConfig.DeviceDb.Query<ProcessManagementDetail>("SELECT * FROM `device_library`;");
            var res = new List<ProcessManagementDetail>();
            if (deviceLibraries.Any())
            {
                var processManagements = ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management`;");
                var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model`;").ToDictionary(x => x.Id);
                var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id`, ProductionProcessName FROM `production_process_library`;").ToDictionary(x => x.Id);

                foreach (var deviceLibrary in deviceLibraries)
                {
                    var processNumberList = processManagements.Where(x => x.DeviceIds.Split(',').Select(int.Parse).Contains(deviceLibrary.Id));
                    if (!processNumberList.Any())
                    {
                        continue;
                    }
                    deviceLibrary.ProcessNumber = processNumberList.Select(x => x.ProcessNumber).Join(",");
                    deviceLibrary.ModelName =
                        deviceModels.ContainsKey(deviceLibrary.Id) ? deviceModels[deviceLibrary.Id].ModelName : "";

                    IEnumerable<int> productModels = null;
                    foreach (var processNumber in processNumberList)
                    {
                        if (!processNumber.ProductModels.IsNullOrEmpty())
                        {
                            var productModelList = processNumber.ProductModels.Split(',').Select(int.Parse)
                                .Where(x => productionProcessLibraries.ContainsKey(x)).Select(x => productionProcessLibraries[x].Id);
                            productModels = (productModels ?? productModelList).Union(productModelList);
                        }
                    }

                    if (productModels != null && productModels.Any())
                    {
                        deviceLibrary.ProductionProcessName = productModels
                            .Where(x => productionProcessLibraries.ContainsKey(x)).Select(x => productionProcessLibraries[x].ProductionProcessName).Join(",");
                    }
                    res.Add(deviceLibrary);
                }
            }

            result.datas.AddRange(res);
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
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE Id = @id;", new { id });
            if (!processManagements.Any())
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var deviceLibraries = ServerConfig.DeviceDb
                .Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library`;").ToDictionary(x => x.Id);
            var deviceModels = ServerConfig.DeviceDb
                .Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model`;").ToDictionary(x => x.Id);
            var productionProcessLibraries = ServerConfig.FlowcardDb
                .Query<ProductionProcessLibrary>(
                    "SELECT `Id`, ProductionProcessName FROM `production_process_library`;")
                .ToDictionary(x => x.Id);

            foreach (var processManagement in processManagements)
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    processManagement.ModelName = deviceModelList
                        .Where(x => deviceModels.ContainsKey(x)).Select(x => deviceModels[x].ModelName).Join(",");
                }

                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    processManagement.ProductionProcessName = productModelList
                        .Where(x => productionProcessLibraries.ContainsKey(x))
                        .Select(x => productionProcessLibraries[x].ProductionProcessName).Join(",");
                }

                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    processManagement.Code = deviceIdList
                        .Where(x => deviceLibraries.ContainsKey(x)).Select(x => deviceLibraries[x].Code).Join(",");
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
            var processManagements = ServerConfig.ProcessDb.Query<ProcessManagementDetail>("SELECT * FROM `process_management` WHERE ProcessNumber = @processNumber;", new { processNumber });
            if (!processManagements.Any())
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var deviceLibraries = ServerConfig.DeviceDb
                .Query<DeviceLibrary>("SELECT `Id`, Code FROM `device_library`;").ToDictionary(x => x.Id);
            var deviceModels = ServerConfig.DeviceDb
                .Query<DeviceModel>("SELECT `Id`, ModelName FROM `device_model`;").ToDictionary(x => x.Id);
            var productionProcessLibraries = ServerConfig.FlowcardDb
                .Query<ProductionProcessLibrary>(
                    "SELECT `Id`, ProductionProcessName FROM `production_process_library`;")
                .ToDictionary(x => x.Id);

            foreach (var processManagement in processManagements)
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    processManagement.ModelName = deviceModelList
                        .Where(x => deviceModels.ContainsKey(x)).Select(x => deviceModels[x].ModelName).Join(",");
                }

                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    processManagement.ProductionProcessName = productModelList
                        .Where(x => productionProcessLibraries.ContainsKey(x))
                        .Select(x => productionProcessLibraries[x].ProductionProcessName).Join(",");
                }

                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceIdList = processManagement.DeviceIds.Split(',').Select(int.Parse);
                    processManagement.Code = deviceIdList
                        .Where(x => deviceLibraries.ContainsKey(x)).Select(x => deviceLibraries[x].Code).Join(",");
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
        // PUT: api/ProcessManagement/Id/5
        [HttpPut("Id/{id}")]
        public Result PutProcessManagement([FromRoute] int id, [FromBody] ProcessManagement processManagement)
        {

            var data =
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `Id` = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `ProcessNumber` = @ProcessNumber;", new { processManagement.ProcessNumber }).FirstOrDefault();
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
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model`;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library`;").ToDictionary(x => x.Id);
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
                    }
                }
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library`;").ToDictionary(x => x.Id);
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

            processManagement.Id = id;
            processManagement.CreateUserId = Request.GetIdentityInformation();
            processManagement.MarkedDateTime = DateTime.Now;
            ServerConfig.ProcessDb.Execute(
                "UPDATE process_management SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `ProcessNumber` = @ProcessNumber, `DeviceModels` = @DeviceModels, `ProductModels` = @ProductModels, " +
                "`DeviceIds` = @DeviceIds WHERE `Id` = @Id;", processManagement);

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
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `ProcessNumber` = @processNumber;", new { processNumber }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }

            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `ProcessNumber` = @ProcessNumber;", new { processManagement.ProcessNumber }).FirstOrDefault();
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
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model`;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library`;").ToDictionary(x => x.Id);
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
                    }
                }
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library`;").ToDictionary(x => x.Id);
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
                ServerConfig.ProcessDb.Query<ProcessManagement>("SELECT * FROM `process_management` WHERE `Id` IN @Id;", new { Id = processManagements.Select(x => x.Id) });
            if (!datas.Any() || datas.Count() != processManagements.Count)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementNotExist);
            }
            var deviceLibraries = ServerConfig.DeviceDb.Query<string>("SELECT Id FROM `device_library`;");
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


        // POST: api/ProcessManagement
        [HttpPost]
        public Result PostProcessManagement([FromBody] ProcessManagement processManagement)
        {
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `ProcessNumber` = @ProcessNumber;", new { processManagement.ProcessNumber }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error.ProcessManagementIsExist);
            }

            try
            {
                if (!processManagement.DeviceModels.IsNullOrEmpty())
                {
                    var deviceModels = ServerConfig.DeviceDb.Query<DeviceModel>("SELECT `Id` FROM `device_model`;").ToDictionary(x => x.Id);
                    var deviceModelList = processManagement.DeviceModels.Split(',').Select(int.Parse);
                    if (deviceModelList.Any(x => !deviceModels.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.DeviceModelNotExist);
                    }
                }
                if (!processManagement.ProductModels.IsNullOrEmpty())
                {
                    var productionProcessLibraries = ServerConfig.FlowcardDb.Query<ProductionProcessLibrary>("SELECT `Id` FROM `production_process_library`;").ToDictionary(x => x.Id);
                    var productModelList = processManagement.ProductModels.Split(',').Select(int.Parse);
                    if (productModelList.Any(x => !productionProcessLibraries.ContainsKey(x)))
                    {
                        return Result.GenError<Result>(Error.ProductionProcessLibraryNotExist);
                    }
                }
                if (!processManagement.DeviceIds.IsNullOrEmpty())
                {
                    var deviceLibraries = ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library`;").ToDictionary(x => x.Id);
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

            processManagement.CreateUserId = Request.GetIdentityInformation();
            processManagement.MarkedDateTime = DateTime.Now;
            ServerConfig.ProcessDb.Execute(
                "INSERT INTO process_management (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessNumber`, `DeviceModels`, `ProductModels`, `DeviceIds`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessNumber, @DeviceModels, @ProductModels, @DeviceIds);",
                processManagement);

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
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE Id = @id;", new { id }).FirstOrDefault();
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
            var cnt =
                ServerConfig.ProcessDb.Query<int>("SELECT COUNT(1) FROM `process_management` WHERE `ProcessNumber` = @processNumber;", new { processNumber }).FirstOrDefault();
            if (cnt == 0)
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
            return Result.GenError<Result>(Error.Success);
        }
    }
}