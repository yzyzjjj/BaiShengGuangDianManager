using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.ProcessManagementModel;
using ApiManagement.Models.RepairManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.UrlMappings;
using ModelBase.Base.Utils;
using ModelBase.Models.Device;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ApiManagement.Controllers.DeviceManagementController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeviceLibraryController : ControllerBase
    {
        // GET: api/DeviceLibrary
        [HttpGet]
        public DataResult GetDeviceLibrary([FromQuery] bool hard, bool work)
        {
            if (!hard)
            {
                var result = new DataResult();
                result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0;"));
                return result;
            }
            else
            {
                var result = new DataResult();
                var deviceLibraryDetails = ServerConfig.ApiDb.Query<DeviceLibraryDetail>(
                        "SELECT a.*, b.ModelName, b.DeviceCategoryId, b.CategoryName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, f.RegionDescription, g.ScriptName, IFNULL(h.`Name`, '')  AdministratorName FROM device_library a " +
                        "JOIN (SELECT a.*, b.CategoryName FROM device_model a JOIN device_category b ON a.DeviceCategoryId = b.Id) b ON a.DeviceModelId = b.Id " +
                        "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                        "JOIN application_library d ON a.ApplicationId = d.Id " +
                        "JOIN hardware_library e ON a.HardwareId = e.Id " +
                        "JOIN site f ON a.SiteId = f.Id " +
                        "JOIN script_version g ON a.ScriptId = g.Id " +
                        "LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete)a GROUP BY a.Account) h ON a.Administrator = h.Account " +
                        "WHERE a.`MarkedDelete` = 0 ORDER BY a.Id;")
                    .ToDictionary(x => x.Id);

                var faultDevices = ServerConfig.ApiDb.Query<dynamic>(
                    "SELECT * FROM (SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state AND a.MarkedDelete = 0 ORDER BY a.DeviceId, a.State DESC ) a GROUP BY DeviceCode;",
                    new { state = RepairStateEnum.Complete });
                foreach (var faultDevice in faultDevices)
                {
                    var device = deviceLibraryDetails.Values.FirstOrDefault(x => x.Id == faultDevice.DeviceId);
                    if (device != null)
                    {
                        device.RepairState = faultDevice.State;
                    }
                }

                var url = ServerConfig.GateUrl + UrlMappings.Urls["deviceListGate"];
                //向GateProxyLink请求数据
                var resp = HttpServer.Get(url);
                if (resp != "fail")
                {
                    try
                    {
                        var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                        if (dataResult.errno == Error.Success)
                        {
                            foreach (DeviceInfo deviceInfo in dataResult.datas)
                            {
                                var deviceId = deviceInfo.DeviceId;
                                if (deviceLibraryDetails.ContainsKey(deviceId))
                                {
                                    deviceLibraryDetails[deviceId].State = deviceInfo.State;
                                    deviceLibraryDetails[deviceId].DeviceState = deviceInfo.DeviceState;
                                    deviceLibraryDetails[deviceId].FlowCard = deviceInfo.FlowCard;
                                    deviceLibraryDetails[deviceId].ProcessTime = deviceInfo.ProcessTime.IsNullOrEmpty() ? "0" : deviceInfo.ProcessTime;
                                    deviceLibraryDetails[deviceId].LeftTime = deviceInfo.LeftTime.IsNullOrEmpty() ? "0" : deviceInfo.LeftTime;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"{UrlMappings.Urls["deviceListGate"]} 返回：{resp},信息:{e.Message}");
                    }
                }

                var data = deviceLibraryDetails.Values.All(x => int.TryParse(x.Code, out _))
                    ? deviceLibraryDetails.Values.OrderByDescending(x => x.DeviceState).ThenByDescending(x => x.DeviceStateStr).ThenBy(x => int.Parse(x.Code))
                    : deviceLibraryDetails.Values.OrderByDescending(x => x.DeviceState).ThenByDescending(x => x.DeviceStateStr).ThenBy(x => x.Code);

                if (work)
                {
                    result.datas.AddRange(data.Select(x => new
                    {
                        x.Id,
                        x.Code,
                        x.CategoryName,
                        x.DeviceStateStr,
                        x.FlowCard,
                        x.ProcessTime,
                        x.LeftTime,
                        x.Administrator,
                        x.SiteName,
                    }));
                }
                else
                {
                    result.datas.AddRange(data);
                }

                return result;
            }
        }

        // GET: api/DeviceLibrary/5
        [HttpGet("{id}")]
        public DataResult GetDeviceLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, b.DeviceCategoryId, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, f.RegionDescription, g.ScriptName FROM device_library a " +
                                                                 "JOIN device_model b ON a.DeviceModelId = b.Id " +
                                                                 "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                                                                 "JOIN application_library d ON a.ApplicationId = d.Id " +
                                                                 "JOIN hardware_library e ON a.HardwareId = e.Id " +
                                                                 "JOIN site f ON a.SiteId = f.Id " +
                                                                 "JOIN script_version g ON a.ScriptId = g.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceNotExist;
                return result;
            }

            var faultDevice = ServerConfig.ApiDb.Query<dynamic>("SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state AND a.MarkedDelete = 0 AND DeviceId = @DeviceId;",
                new { DeviceId = data.Id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (faultDevice != null)
            {
                data.RepairState = faultDevice.State;
            }
            var url = ServerConfig.GateUrl + UrlMappings.Urls["deviceSingleGate"];
            //向GateProxyLink请求数据
            var resp = HttpServer.Get(url, new Dictionary<string, string>
            {
                { "id", data.Id.ToString()}
            });
            if (resp != "fail")
            {
                try
                {
                    var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                    if (dataResult.errno == Error.Success)
                    {
                        if (dataResult.datas.Any())
                        {
                            var deviceInfo = dataResult.datas.First();
                            data.State = deviceInfo.State;
                            data.DeviceState = deviceInfo.DeviceState;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
                }
            }

            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 根据机台号
        /// </summary>
        /// <param name="code">机台号</param>
        /// <returns></returns>
        // GET: api/DeviceLibrary/Code/5
        [HttpGet("Code/{code}")]
        public DataResult GetDeviceLibrary([FromRoute] string code)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, b.DeviceCategoryId, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, g.ScriptName FROM device_library a " +
                                                                 "JOIN device_model b ON a.DeviceModelId = b.Id " +
                                                                 "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                                                                 "JOIN application_library d ON a.ApplicationId = d.Id " +
                                                                 "JOIN hardware_library e ON a.HardwareId = e.Id " +
                                                                 "JOIN site f ON a.SiteId = f.Id " +
                                                                 "JOIN script_version g ON a.ScriptId = g.Id WHERE a.Code = @code AND a.`MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceNotExist;
                return result;
            }
            var faultDevice = ServerConfig.ApiDb.Query<dynamic>("SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state AND a.MarkedDelete = 0 AND DeviceId = @DeviceId;",
                new { DeviceId = data.Id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (faultDevice != null)
            {
                data.RepairState = faultDevice.State;
            }

            var url = ServerConfig.GateUrl + UrlMappings.Urls["deviceSingleGate"];
            //向GateProxyLink请求数据
            var resp = HttpServer.Get(url, new Dictionary<string, string>
            {
                { "id", data.Id.ToString()}
            });
            if (resp != "fail")
            {
                try
                {
                    var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                    if (dataResult.errno == Error.Success)
                    {
                        if (dataResult.datas.Any())
                        {
                            var deviceInfo = dataResult.datas.First();
                            data.State = deviceInfo.State;
                            data.DeviceState = deviceInfo.DeviceState;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
                }
            }

            result.datas.Add(data);
            return result;
        }

        // GET: api/DeviceLibrary/Info
        [HttpGet("Info")]
        public DeviceUpdateResult GetDeviceLibraryInfo()
        {
            var result = new DeviceUpdateResult();
            var deviceCategories =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryName FROM `device_category` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.deviceCategories.AddRange(deviceCategories);

            var deviceModels =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, DeviceCategoryId, ModelName FROM `device_model` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.deviceModels.AddRange(deviceModels);

            var firmwareLibraries =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, FirmwareName FROM `firmware_library` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.firmwareLibraries.AddRange(firmwareLibraries);

            var hardwareLibraries =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, HardwareName FROM `hardware_library` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.hardwareLibraries.AddRange(hardwareLibraries);

            var applicationLibraries =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, ApplicationName FROM `application_library` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.applicationLibraries.AddRange(applicationLibraries);

            var sites =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, SiteName, RegionDescription FROM `site` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.sites.AddRange(sites);

            var scriptVersions =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, DeviceModelId, ScriptName FROM `script_version` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.scriptVersions.AddRange(scriptVersions);

            var maintainers =
                ServerConfig.ApiDb.Query<dynamic>("SELECT Id, `Name`, `Account` FROM `maintainer` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;");
            result.maintainers.AddRange(maintainers);
            return result;
        }

        // GET: api/DeviceLibrary/State
        [HttpGet("State/{id}")]
        public DataResult GetDeviceLibraryState([FromRoute] int id)
        {
            var device =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, ScriptId FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<DataResult>(Error.DeviceNotExist);
            }

            var scriptVersion =
                 ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = device.ScriptId }).FirstOrDefault();
            if (scriptVersion == null)
            {
                return Result.GenError<DataResult>(Error.ScriptVersionNotExist);
            }

            var usuallyDictionaries =
                ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE ScriptId = @ScriptId AND MarkedDelete = 0;", new { device.ScriptId });
            if (!usuallyDictionaries.Any())
            {
                return Result.GenError<DataResult>(Error.UsuallyDictionaryNotExist);
            }

            var usuallyDictionaryTypes = ServerConfig.ApiDb.Query<UsuallyDictionaryType>("SELECT `Id` FROM `usually_dictionary_type` WHERE MarkedDelete = 0 AND IsDetail = 1;");
            if (!usuallyDictionaryTypes.Any())
            {
                return Result.GenError<DataResult>(Error.UsuallyDictionaryTypeNotExist);
            }

            var result = new DataResult();

            var url = ServerConfig.GateUrl + UrlMappings.Urls["batchSendBackGate"];
            var msg = new DeviceInfoMessagePacket(scriptVersion.MaxValuePointerAddress, scriptVersion.MaxInputPointerAddress,
                scriptVersion.MaxOutputPointerAddress);
            //向GateProxyLink请求数据
            var resp = HttpServer.Post(url, new Dictionary<string, string>{
                {"devicesList",(new List<DeviceInfo>
                    {
                        new DeviceInfo
                        {
                             DeviceId = id,
                            Instruction = msg.Serialize()
                        }
                    }).ToJSON()
                }
            });

            if (resp != "fail")
            {
                try
                {
                    var dataResult = JsonConvert.DeserializeObject<MessageResult>(resp);
                    if (dataResult.errno == Error.Success)
                    {
                        if (dataResult.messages.Any())
                        {
                            var data = dataResult.messages.First().Item2;
                            var res = JsonConvert.DeserializeObject<DeviceData>(data);
                            if (res != null)
                            {
                                foreach (var usuallyDictionaryType in usuallyDictionaryTypes)
                                {
                                    var usuallyDictionary =
                                        usuallyDictionaries.FirstOrDefault(
                                            x => x.VariableNameId == usuallyDictionaryType.Id);
                                    if (usuallyDictionary != null)
                                    {
                                        var v = string.Empty;
                                        var dId = usuallyDictionary.DictionaryId - 1;
                                        switch (usuallyDictionary.VariableTypeId)
                                        {
                                            case 1:
                                                if (res.vals.Count >= usuallyDictionary.DictionaryId)
                                                {
                                                    v = res.vals[dId].ToString();
                                                    if (usuallyDictionary.VariableNameId == 6)
                                                    {
                                                        var flowCard = ServerConfig.ApiDb.Query<dynamic>("SELECT FlowCardName, ProductionProcessId FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;",
                                                            new { id = v }).FirstOrDefault();
                                                        if (flowCard != null)
                                                        {
                                                            v = flowCard.FlowCardName;
                                                            var processNumber = ServerConfig.ApiDb.Query<dynamic>(
                                                                "SELECT Id, ProcessNumber FROM `process_management` WHERE FIND_IN_SET(@DeviceId, DeviceIds) AND FIND_IN_SET(@ProductModel, ProductModels) AND MarkedDelete = 0;", new
                                                                {
                                                                    DeviceId = id,
                                                                    ProductModel = flowCard.ProductionProcessId
                                                                }).FirstOrDefault();
                                                            if (processNumber != null)
                                                            {
                                                                result.datas.Add(new Tuple<int, string>(-1, processNumber.ProcessNumber));
                                                            }
                                                        }
                                                    }
                                                }
                                                break;
                                            case 2:
                                                if (res.ins.Count >= usuallyDictionary.DictionaryId)
                                                {
                                                    v = res.ins[dId].ToString();
                                                }
                                                break;
                                            case 3:
                                                if (res.outs.Count >= usuallyDictionary.DictionaryId)
                                                {
                                                    v = res.outs[dId].ToString();
                                                }
                                                break;
                                        }
                                        result.datas.Add(new Tuple<int, string>(usuallyDictionaryType.Id, v));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{UrlMappings.Urls["sendBackGate"]} 返回：{resp},信息:{e.Message}");
                }
            }

            return result;
        }

        // PUT: api/DeviceLibrary/5
        [HttpPut("{id}")]
        public Result PutDeviceLibrary([FromRoute] int id, [FromBody] DeviceLibrary deviceLibrary)
        {
            var data =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            if (!IPAddress.TryParse(deviceLibrary.Ip, out _))
            {
                return Result.GenError<Result>(Error.IpInvalid);
            }

            if (deviceLibrary.Port < 0 || deviceLibrary.Port > 65535)
            {
                return Result.GenError<Result>(Error.PortInvalid);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND `MarkedDelete` = 0;", new
                {
                    deviceLibrary.Ip,
                    deviceLibrary.Port
                }).FirstOrDefault();
            if (cnt > 0)
            {
                if (data.Ip != deviceLibrary.Ip || data.Port != deviceLibrary.Port)
                {
                    return Result.GenError<Result>(Error.IpPortIsExist);
                }
            }
            deviceLibrary.Id = id;
            deviceLibrary.CreateUserId = Request.GetIdentityInformation();
            deviceLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE device_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `Code` = @Code, " +
                "`DeviceName` = @DeviceName, `MacAddress` = @MacAddress, `Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `Administrator` = @Administrator, " +
                "`Remark` = @Remark WHERE `Id` = @Id;", deviceLibrary);

            if (deviceLibrary.Ip != data.Ip || deviceLibrary.Port != data.Port)
            {
                HttpResponseErrAsync(new DeviceInfo
                {
                    DeviceId = deviceLibrary.Id,
                    Ip = deviceLibrary.Ip,
                    Port = deviceLibrary.Port,
                }, "batchUpdateDeviceGate", "PutDeviceLibrary");
            }

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 根据机台号
        /// </summary>
        /// <param name="code">机台号</param>
        /// <param name="deviceLibrary">更新信息</param>
        /// <returns></returns>
        // PUT: api/DeviceLibrary/Code/5
        [HttpPut("Code/{code}")]
        public Result PutDeviceLibrary([FromRoute] string code, [FromBody] DeviceLibrary deviceLibrary)
        {
            var data =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE Code = @code AND `MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }

            if (!IPAddress.TryParse(deviceLibrary.Ip, out _))
            {
                return Result.GenError<Result>(Error.IpInvalid);
            }

            if (deviceLibrary.Port < 0 || deviceLibrary.Port > 65535)
            {
                return Result.GenError<Result>(Error.PortInvalid);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND Id != @Port;", new
                {
                    deviceLibrary.Ip,
                    deviceLibrary.Port
                }).FirstOrDefault();
            if (cnt > 0)
            {
                if (data.Ip != deviceLibrary.Ip || data.Port != deviceLibrary.Port)
                {
                    return Result.GenError<Result>(Error.IpPortIsExist);
                }
            }
            deviceLibrary.Code = code;
            deviceLibrary.CreateUserId = Request.GetIdentityInformation();
            deviceLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE device_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `Code` = @Code, " +
                "`DeviceName` = @DeviceName, `MacAddress` = @MacAddress, `Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `Administrator` = @Administrator, " +
                "`Remark` = @Remark WHERE `Id` = @Id;", deviceLibrary);

            if (deviceLibrary.Id != data.Id || deviceLibrary.Ip != data.Ip || deviceLibrary.Port != data.Port)
            {
                HttpResponseErrAsync(new DeviceInfo
                {
                    DeviceId = deviceLibrary.Id,
                }, "batchDelDeviceGate", "PutDeviceLibrary", () =>
                {
                    HttpResponseErrAsync(new DeviceInfo
                    {
                        DeviceId = deviceLibrary.Id,
                        Ip = deviceLibrary.Ip,
                        Port = deviceLibrary.Port,
                    }, "batchAddDeviceGate", "PutDeviceLibrary");
                });
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceLibrary
        [HttpPost]
        public Result PostDeviceLibrary([FromBody] DeviceLibrary deviceLibrary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND `MarkedDelete` = 0;", new
                {
                    deviceLibrary.Ip,
                    deviceLibrary.Port
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            var scriptVersion =
                ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
            if (scriptVersion == null)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            if (!IPAddress.TryParse(deviceLibrary.Ip, out _))
            {
                return Result.GenError<Result>(Error.IpInvalid);
            }

            if (deviceLibrary.Port < 0 || deviceLibrary.Port > 65535)
            {
                return Result.GenError<Result>(Error.PortInvalid);
            }

            deviceLibrary.CreateUserId = Request.GetIdentityInformation();
            deviceLibrary.MarkedDateTime = DateTime.Now;
            var lastInsertId = ServerConfig.ApiDb.Query<int>(
              "INSERT INTO device_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `Code`, `DeviceName`, `MacAddress`, `Ip`, `Port`, `Identifier`, `DeviceModelId`, " +
              "`ScriptId`, `FirmwareId`, `HardwareId`, `ApplicationId`, `SiteId`, `Administrator`, `Remark`) VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, " +
              "@ModifyId, @Code, @DeviceName, @MacAddress, @Ip, @Port, @Identifier, @DeviceModelId, @ScriptId, @FirmwareId, @HardwareId, @ApplicationId, @SiteId, @Administrator, " +
              "@Remark);SELECT LAST_INSERT_ID();",
              deviceLibrary).FirstOrDefault();

            ServerConfig.ApiDb.Execute("INSERT INTO npc_proxy_link (`DeviceId`, `Instruction`) VALUES (@DeviceId, @Instruction);",
                new { DeviceId = lastInsertId, Instruction = scriptVersion.HeartPacket, });

            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = lastInsertId,
                Ip = deviceLibrary.Ip,
                Port = deviceLibrary.Port,
            }, "batchAddDeviceGate", "PostDeviceLibrary");
            return Result.GenError<Result>(Error.Success);
        }

        public class ProcessInfo
        {
            /// <summary>
            /// 设备id
            /// </summary>
            public int DeviceId;
            /// <summary>
            /// 工艺编号id
            /// </summary>
            public int ProcessId;
            /// <summary>
            /// 流程卡id
            /// </summary>
            public int FlowCardId;

            public List<ProcessDataSimple> ProcessDatas;
        }
        // POST: api/DeviceLibrary/SetProcessStep
        [HttpPost("SetProcessStep")]
        public Result PostDeviceLibrarySetProcessStep([FromBody] ProcessInfo processInfo)
        {
            var device =
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, IFNULL(b.DeviceCategoryId, 0) DeviceCategoryId FROM `device_library` a JOIN `device_model` b ON a.DeviceModelId = b.Id " +
                                                              "WHERE a.Id = @DeviceId AND a.`MarkedDelete` = 0 AND b.`MarkedDelete` = 0;", new { processInfo.DeviceId }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var url = ServerConfig.GateUrl + UrlMappings.Urls["deviceSingleGate"];
            //向GateProxyLink请求数据
            var resp = HttpServer.Get(url, new Dictionary<string, string>
            {
                { "id", processInfo.DeviceId.ToString()}
            });
            if (resp == "fail")
            {
                return Result.GenError<Result>(Error.ExceptionHappen);
            }

            try
            {
                var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                if (dataResult.errno == Error.Success)
                {
                    if (dataResult.datas.Any())
                    {
                        var deviceInfo = dataResult.datas.First();
                        if (deviceInfo.DeviceState != DeviceState.Waiting)
                        {
                            return Result.GenError<Result>(deviceInfo.DeviceState == DeviceState.Processing ? Error.ProcessingNotSet : Error.DeviceStateErrorNotSet);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
                return Result.GenError<Result>(Error.AnalysisFail);
            }

            var flowCard =
                ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.RawMateriaName, c.ProductionProcessName FROM `flowcard_library` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id " +
                                                                "JOIN `production_library` c ON a.ProductionProcessId = c.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id = processInfo.FlowCardId }).FirstOrDefault();
            if (flowCard == null)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
            }


            var processDatas =
                ServerConfig.ApiDb.Query<dynamic>("SELECT `ProcessOrder`, `PressurizeMinute`, `PressurizeSecond`, `ProcessMinute`, `ProcessSecond`, " +
                                                  "`Pressure`, `Speed` FROM `process_data` WHERE ProcessManagementId = @ProcessId AND MarkedDelete = 0 ORDER BY ProcessOrder;", new
                                                  {
                                                      processInfo.ProcessId,
                                                  });
            if (!processDatas.Any())
            {
                return Result.GenError<Result>(Error.ProcessDataNotExist);
            }

            if (processInfo.ProcessDatas != null)
            {
                if (processInfo.ProcessDatas.Any())
                {
                    processDatas = processInfo.ProcessDatas;
                }
                else
                {
                    return Result.GenError<Result>(Error.ProcessDataNotExist);
                }
            }

            var dictionaryIds =
                ServerConfig.ApiDb.Query<UsuallyDictionaryDetail>("SELECT a.Id, VariableName, DictionaryId FROM `usually_dictionary_type` a JOIN `usually_dictionary` b " +
                                                                  "ON a.Id = b.VariableNameId WHERE b.ScriptId = @ScriptId AND a.MarkedDelete = 0 ORDER BY a.Id;", new
                                                                  {
                                                                      device.ScriptId,
                                                                  });
            if (!dictionaryIds.Any())
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryTypeNotExist);
            }
            var messagePacket = new SetValMessagePacket();
            var key = new[]
            {
                "加压时间分",
                "加压时间秒",
                "工艺时间分",
                "工艺时间秒",
                "设定压力",
                "下盘速度",
            };

            var isSetProcessData = ServerConfig.RedisHelper.Get<int>(ServerConfig.IsSetProcessDataKey) == 1;
            if (isSetProcessData)
            {
                //当前配方
                messagePacket.Vals.Add(98, 0);
                var i = 1;
                foreach (var processData in processDatas)
                {
                    var j = 0;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.PressurizeMinute);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.PressurizeSecond);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.ProcessMinute);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.ProcessSecond);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.Pressure);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, processData.Speed * 100);
                    }

                    i++;
                }

                for (; i <= 8; i++)
                {
                    var j = 0;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        messagePacket.Vals.Add(dictionaryIds.First(x => x.VariableName == key[j] + i).DictionaryId - 1, 0);
                    }
                }

            }
            if (dictionaryIds.Any(x => x.Id == 6))
            {
                messagePacket.Vals.Add(dictionaryIds.First(x => x.Id == 6).DictionaryId - 1, processInfo.FlowCardId);
            }
            var msg = messagePacket.Serialize();
            url = ServerConfig.GateUrl + UrlMappings.Urls["batchSendBackGate"];
            //向GateProxyLink请求数据
            resp = HttpServer.Post(url, new Dictionary<string, string>{
                { "devicesList", (new List<DeviceInfo>
                    {
                        new DeviceInfo
                        {
                            DeviceId = processInfo.DeviceId,
                            Instruction = msg
                        }
                    }).ToJSON()
                }
            });
            var fRes = false;
            if (resp != "fail")
            {
                var dataResult = JsonConvert.DeserializeObject<MessageResult>(resp);
                if (dataResult.errno == Error.Success)
                {

                    if (dataResult.messages.Any())
                    {
                        var data = dataResult.messages.First().Item2;
                        var res = messagePacket.Deserialize(data);
                        fRes = res == 0;
                    }
                }
            }
            var account = Request.GetIdentityInformation();
            var time = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO process_log (`AccountName`, `OperateTime`, `DeviceId`, `DeviceCode`, `ProcessId`, `FlowCardId`, `Result`, `ProcessData`) " +
                "VALUES (@AccountName, @OperateTime, @DeviceId, @DeviceCode, @ProcessId, @FlowCardId, @Result, @ProcessData);",
                new
                {
                    AccountName = account,
                    OperateTime = time,
                    DeviceId = processInfo.DeviceId,
                    DeviceCode = device.Code,
                    ProcessId = processInfo.ProcessId,
                    FlowCardId = processInfo.FlowCardId,
                    Result = fRes,
                    ProcessData = processDatas.ToJSON()
                });

            if (fRes)
            {
                var deviceProcessStep =
                    ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT * FROM `device_process_step` WHERE DeviceCategoryId = @DeviceCategoryId AND MarkedDelete = 0 AND IsSurvey = 0;",
                        new { device.DeviceCategoryId }).ToDictionary(x => x.Id);
                if (deviceProcessStep.Any())
                {
                    var processorId = ServerConfig.ApiDb
                        .Query<int>("SELECT Id FROM `processor` WHERE Account = @Account AND MarkedDelete = 0;", new { Account = account })
                        .FirstOrDefault();

                    var flowCardProcessStepDetails = ServerConfig.ApiDb.Query<FlowCardProcessStepDetail>("SELECT * FROM `flowcard_process_step` WHERE FlowCardId = @FlowCardId AND MarkedDelete = 0;", new
                    {
                        FlowCardId = flowCard.Id
                    });
                    var flowCardProcessStepDetail =
                        flowCardProcessStepDetails.FirstOrDefault(x => deviceProcessStep.ContainsKey(x.ProcessStepId) && x.ProcessTime == default(DateTime));
                    if (flowCardProcessStepDetail != null)
                    {
                        flowCardProcessStepDetail.MarkedDateTime = time;
                        flowCardProcessStepDetail.ProcessorId = processorId;
                        flowCardProcessStepDetail.DeviceId = device.Id;

                        ServerConfig.ApiDb.Execute(
                            "UPDATE flowcard_process_step SET `MarkedDateTime` = @MarkedDateTime, `ProcessorId` = @ProcessorId, `DeviceId` = @DeviceId WHERE `Id` = @Id;", flowCardProcessStepDetail);
                    }
                }
            }

            return Result.GenError<Result>(fRes ? Error.Success : Error.Fail);
        }


        public class DataSet
        {
            //设备id
            public int DeviceId;
            //变量类型
            public int UsuallyDictionaryId;
            //设置的值
            public int Value;
        }

        // POST: api/DeviceLibrary/DataSet
        [HttpPost("DataSet")]
        public Result PostDeviceLibraryDataSet([FromBody] DataSet dataInfo)
        {
            var device =
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, IFNULL(b.DeviceCategoryId, 0) DeviceCategoryId FROM `device_library` a JOIN `device_model` b ON a.DeviceModelId = b.Id " +
                                                              "WHERE a.Id = @DeviceId AND a.`MarkedDelete` = 0;", new { dataInfo.DeviceId }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var url = ServerConfig.GateUrl + UrlMappings.Urls["deviceSingleGate"];
            //向GateProxyLink请求数据
            var resp = HttpServer.Get(url, new Dictionary<string, string>
            {
                { "id", dataInfo.DeviceId.ToString()}
            });
            if (resp == "fail")
            {
                return Result.GenError<Result>(Error.ExceptionHappen);
            }

            try
            {
                var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                if (dataResult.errno == Error.Success)
                {
                    if (dataResult.datas.Any())
                    {
                        var deviceInfo = dataResult.datas.First();
                        if (deviceInfo.DeviceState != DeviceState.Waiting)
                        {
                            return Result.GenError<Result>(deviceInfo.DeviceState == DeviceState.Processing ? Error.ProcessingNotSet : Error.DeviceStateErrorNotSet);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
                return Result.GenError<Result>(Error.AnalysisFail);
            }

            var dictionaryId =
                ServerConfig.ApiDb.Query<UsuallyDictionaryDetail>("SELECT * FROM `usually_dictionary` WHERE ScriptId = @ScriptId AND VariableNameId = @VariableNameId AND MarkedDelete = 0;", new
                {
                    device.ScriptId,
                    VariableNameId = dataInfo.UsuallyDictionaryId
                }).FirstOrDefault();
            if (dictionaryId == null)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryTypeNotExist);
            }
            var messagePacket = new SetValMessagePacket();
            if (dictionaryId.VariableTypeId == 1)
            {
                messagePacket.Vals.Add(dictionaryId.DictionaryId - 1, dataInfo.Value);
            }
            else
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var msg = messagePacket.Serialize();
            url = ServerConfig.GateUrl + UrlMappings.Urls["batchSendBackGate"];
            //向GateProxyLink请求数据
            resp = HttpServer.Post(url, new Dictionary<string, string>{
                { "devicesList",(new List<DeviceInfo>
                    {
                        new DeviceInfo
                        {
                            DeviceId = dataInfo.DeviceId,
                            Instruction = msg
                        }
                    }).ToJSON()
                }
            });
            var fRes = false;
            if (resp != "fail")
            {
                var dataResult = JsonConvert.DeserializeObject<MessageResult>(resp);
                if (dataResult.errno == Error.Success)
                {

                    if (dataResult.messages.Any())
                    {
                        var data = dataResult.messages.First().Item2;
                        var res = messagePacket.Deserialize(data);
                        fRes = res == 0;
                    }
                }
            }

            return Result.GenError<Result>(fRes ? Error.Success : Error.Fail);
        }

        public class DeviceOperate
        {
            //设备id
            public int DeviceId;
            //操作时间
            public DateTime Time;
            //操作名称
            public string OpName;
            //操作参数
            public decimal Thickness;
        }

        // POST: api/DeviceLibrary/DeviceOperate
        [HttpPost("DeviceOperate")]
        public Result PostDeviceLibraryDeviceOperate([FromBody] DeviceOperate deviceOperate)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @Id AND `MarkedDelete` = 0;", new
                {
                    Id = deviceOperate.DeviceId
                }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }
            var account = Request.GetIdentityInformation();
            var processorId = ServerConfig.ApiDb
                .Query<int>("SELECT Id FROM `processor` WHERE Account = @Account AND MarkedDelete = 0;", new { Account = account }).FirstOrDefault();

            ServerConfig.ApiDb.Execute(
                "INSERT INTO npc_monitoring_process_log (`OpName`, `DeviceId`, `StartTime`, `EndTime`, `ProcessorId`, `ActualThickness`) VALUES (@OpName, @DeviceId, @StartTime, @EndTime, @ProcessorId, @ActualThickness);",
                    new MonitoringProcessLog
                    {
                        OpName = deviceOperate.OpName,
                        DeviceId = deviceOperate.DeviceId,
                        StartTime = deviceOperate.Time,
                        EndTime = deviceOperate.Time,
                        ProcessorId = processorId,
                        ActualThickness = deviceOperate.Thickness,
                    });

            return Result.GenError<Result>(Error.Success);
        }

        public class UpgradeInfo
        {
            //设备id
            public int DeviceId;
            //固件ID
            public int FirmwareId;
            //固件bin文件
            public string FirmwareFile;
        }
        // POST: api/DeviceLibrary/Upgrade
        [HttpPost("Upgrade")]
        public Result Upgrade([FromBody] UpgradeInfo upgradeInfo)
        {

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceLibrary/ReStart
        [HttpPost("ReStart")]
        public Result ReStart([FromRoute] int id)
        {

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `device_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = id,
            }, "batchDelDeviceGate", "DeleteDeviceLibrary");
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 根据机台号
        /// </summary>
        /// <returns></returns>
        // DELETE: api/DeviceLibrary/5
        [HttpDelete("Code/{code}")]
        public Result DeleteDeviceLibrary([FromRoute] string code)
        {
            var data =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library` WHERE Code = @code AND `MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `device_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    data.Id
                });
            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = data.Id,
            }, "batchDelDeviceGate", "DeleteDeviceLibrary");
            return Result.GenError<Result>(Error.Success);
        }
        private static void HttpResponseErrAsync(DeviceInfo deviceInfo, string urlKey, string funName, Action callback = null)
        {
            var url = ServerConfig.GateUrl + UrlMappings.Urls[urlKey];
            //向NpcProxyLink请求数据
            HttpServer.PostAsync(url, new Dictionary<string, string>
            {
                { "devicesList", new List<DeviceInfo>
                    {
                        deviceInfo
                    }.ToJSON()
                }
            }, (resp, exp) =>
            {
                Log.DebugFormat("{0} Res:{1}", funName, resp);
                callback?.Invoke();
            });
        }
    }
}