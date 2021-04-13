using ApiManagement.Base.Helper;
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
using ModelBase.Models.Control;
using ModelBase.Models.Device;
using ModelBase.Models.Result;
using Newtonsoft.Json;using ModelBase.Models.BaseModel;
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
        public DataResult GetDeviceLibrary([FromQuery] bool detail, bool other, bool state, bool work, string ids, int scriptId, int categoryId)
        {
            var idList = !ids.IsNullOrEmpty() ? ids.Split(",").Select(int.Parse) : new int[0];
            if (!detail)
            {
                var models = new List<int>();
                if (categoryId != 0)
                {
                    models.AddRange(ServerConfig.ApiDb.Query<int>("SELECT Id FROM `device_model` WHERE MarkedDelete = 0 AND DeviceCategoryId = @categoryId;", new { categoryId }));
                }
                var result = new DataResult();
                if (categoryId != 0 && !models.Any())
                {
                    return result;
                }
                result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceLibrary>($"SELECT * FROM `device_library` WHERE MarkedDelete = 0" +
                                                                              $"{(idList.Any() ? " AND Id IN @idList" : "")}" +
                                                                              $"{(scriptId != 0 ? " AND ScriptId = @scriptId" : "")}" +
                                                                              $"{(categoryId != 0 ? " AND DeviceModelId IN @models" : "")};",
                    new { idList, scriptId, models }));
                return result;
            }
            else
            {
                var result = new DataResult();
                var sql = "";
                if (work)
                {
                    sql = "SELECT a.*, b.ModelName, b.DeviceCategoryId, b.CategoryName FROM device_library a " +
                            $"JOIN (SELECT a.*, b.CategoryName FROM device_model a JOIN device_category b ON a.DeviceCategoryId = b.Id " +
                            $"{(categoryId != 0 ? "WHERE b.Id = @categoryId" : "")}) b ON a.DeviceModelId = b.Id " +
                            $"WHERE" +
                            $"{(idList.Any() ? " a.Id IN @idList AND " : "")}" +
                            $"{(scriptId != 0 ? " a.ScriptId = @scriptId AND " : "")}" +
                            $" a.`MarkedDelete` = 0 ORDER BY a.Id;";
                }
                else
                {
                    sql = other ?
                            "SELECT a.*, b.ModelName, b.DeviceCategoryId, b.CategoryName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, f.RegionDescription, " +
                            "g.ScriptName, IFNULL(h.`Name`, '')  AdministratorName, i.`Class` FROM device_library a " +
                            $"JOIN (SELECT a.*, b.CategoryName FROM device_model a JOIN device_category b ON a.DeviceCategoryId = b.Id " +
                            $"{(categoryId != 0 ? "WHERE b.Id = @categoryId" : "")}) b ON a.DeviceModelId = b.Id " +
                            "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                            "JOIN application_library d ON a.ApplicationId = d.Id " +
                            "JOIN hardware_library e ON a.HardwareId = e.Id " +
                            "JOIN site f ON a.SiteId = f.Id " +
                            "JOIN script_version g ON a.ScriptId = g.Id " +
                            "JOIN device_class i ON a.ClassId = i.Id " +
                            "LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete)a GROUP BY a.Account) h ON a.Administrator = h.Account " +
                            $"WHERE" +
                            $"{(idList.Any() ? " a.Id IN @idList AND " : "")}" +
                            $"{(scriptId != 0 ? " a.ScriptId = @scriptId AND " : "")}" +
                            $" a.`MarkedDelete` = 0 ORDER BY a.Id;"
                            : "SELECT * FROM device_library " +
                              $"WHERE" +
                              $"{(idList.Any() ? " Id IN @idList AND" : "")}" +
                              $"{(scriptId != 0 ? " ScriptId = @scriptId AND" : "")}" +
                              $" `MarkedDelete` = 0 ORDER BY Id;";

                }
                var deviceLibraryDetails = ServerConfig.ApiDb.Query<DeviceLibraryDetail>(sql, new { idList, scriptId, categoryId }).ToDictionary(x => x.Id);
                if (state)
                {
                    var faultDevices = ServerConfig.ApiDb.Query<dynamic>(
                        $"SELECT * FROM (SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state{(idList.Any() ? " AND a.DeviceId IN @idList" : "")}{(scriptId != 0 ? " AND a.ScriptId = @scriptId" : "")} AND a.MarkedDelete = 0 ORDER BY a.DeviceId, a.State DESC ) a GROUP BY DeviceCode;",
                        new { state = RepairStateEnum.Complete, idList, scriptId });
                    foreach (var faultDevice in faultDevices)
                    {
                        var device = deviceLibraryDetails.Values.FirstOrDefault(x => x.Id == faultDevice.DeviceId);
                        if (device != null)
                        {
                            device.RepairState = faultDevice.State;
                        }
                    }

                    //ServerConfig.GateUrl = "http://192.168.1.142:61102";
                    var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.deviceListGate];
                    //向GateProxyLink请求数据
                    var resp = !idList.Any() ? HttpServer.Get(url) :
                        HttpServer.Get(url, new Dictionary<string, string>
                        {
                            {"ids", idList.Join()}
                        });
                    if (resp != "fail")
                    {
                        try
                        {
                            var scriptIds = deviceLibraryDetails.Values.Select(x => x.ScriptId);
                            var dataNameDictionaries = scriptIds.Any() ? DataNameDictionaryHelper.GetDataNameDictionaryDetails(scriptIds) : new List<DataNameDictionaryDetail>();
                            var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                            if (dataResult.errno == Error.Success)
                            {
                                foreach (DeviceInfo deviceInfo in dataResult.datas)
                                {
                                    var deviceId = deviceInfo.DeviceId;
                                    if (deviceLibraryDetails.ContainsKey(deviceId))
                                    {
                                        deviceInfo.ScriptId = deviceLibraryDetails[deviceId].ScriptId;
                                        deviceLibraryDetails[deviceId].State = deviceInfo.State;
                                        deviceLibraryDetails[deviceId].DeviceState = deviceInfo.DeviceState;

                                        //deviceLibraryDetails[deviceId].FlowCard = deviceInfo.FlowCard;
                                        //deviceLibraryDetails[deviceId].ProcessTime = deviceInfo.ProcessTime.IsNullOrEmpty() ? "0" : deviceInfo.ProcessTime;
                                        //deviceLibraryDetails[deviceId].LeftTime = deviceInfo.LeftTime.IsNullOrEmpty() ? "0" : deviceInfo.LeftTime;
                                        var deviceData = deviceInfo.DeviceData;
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.washFlagDId, out var v))
                                        {
                                            if (v > 0)
                                            {
                                                deviceLibraryDetails[deviceId].ProcessType = ProcessType.Wash;
                                            }
                                        }
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.repairFlagDId, out v))
                                        {
                                            if (v > 0)
                                            {
                                                deviceLibraryDetails[deviceId].ProcessType = ProcessType.Repair;
                                            }
                                        }
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.processFlagDId, out v))
                                        {
                                            if (v > 0)
                                            {
                                                deviceLibraryDetails[deviceId].ProcessType = ProcessType.Process;
                                            }
                                        }
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.flowCardDId, out v))
                                        {
                                            deviceLibraryDetails[deviceId].FlowCard = (v == -1 ? 0 : v).ToString();
                                        }
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.processedTimeDId, out v))
                                        {
                                            deviceLibraryDetails[deviceId].ProcessTime = (v == -1 ? 0 : v).ToString();
                                        }
                                        if (AnalysisHelper.GetValue(deviceData, dataNameDictionaries, deviceInfo.ScriptId, AnalysisHelper.leftProcessTimeDId, out v))
                                        {
                                            deviceLibraryDetails[deviceId].LeftTime = (v == -1 ? 0 : v).ToString();
                                        }
                                    }
                                }

                                var fcs = deviceLibraryDetails.Values.Where(x => int.TryParse(x.FlowCard, out var id) && id > 0).Select(x => int.Parse(x.FlowCard));
                                var flowCards = FlowCardHelper.Instance.GetAllByIds<FlowCard>(fcs);
                                foreach (var (deviceId, device) in deviceLibraryDetails)
                                {
                                    FlowCard fc = null;
                                    if (int.TryParse(device.FlowCard, out var id))
                                    {
                                        fc = flowCards.FirstOrDefault(x => x.Id == id);
                                    }

                                    deviceLibraryDetails[deviceId].FlowCard = fc?.FlowCardName ?? "";
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error($"{UrlMappings.Urls[UrlMappings.deviceListGate]},信息:{e}");
                        }
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
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, b.DeviceCategoryId, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, f.RegionDescription, " +
                                                              "g.ScriptName, i.`Class` FROM device_library a " +
                                                                 "JOIN device_model b ON a.DeviceModelId = b.Id " +
                                                                 "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                                                                 "JOIN application_library d ON a.ApplicationId = d.Id " +
                                                                 "JOIN hardware_library e ON a.HardwareId = e.Id " +
                                                                 "JOIN site f ON a.SiteId = f.Id " +
                                                                 "JOIN device_class i ON a.ClassId = i.Id " +
                                                                 "JOIN script_version g ON a.ScriptId = g.Id WHERE a.Id = @id AND a.`MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceNotExist;
                return result;
            }

            var faultDevice = ServerConfig.ApiDb.Query<dynamic>("SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state AND DeviceId = @DeviceId AND a.MarkedDelete = 0;",
                new { DeviceId = data.Id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (faultDevice != null)
            {
                data.RepairState = faultDevice.State;
            }
            var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.deviceSingleGate];
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
                    Log.Error($"{UrlMappings.Urls[UrlMappings.deviceSingleGate]},信息:{e}");
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
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, b.DeviceCategoryId, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, " +
                                                              "g.ScriptName, i.`Class` FROM device_library a " +
                                                                 "JOIN device_model b ON a.DeviceModelId = b.Id " +
                                                                 "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                                                                 "JOIN application_library d ON a.ApplicationId = d.Id " +
                                                                 "JOIN hardware_library e ON a.HardwareId = e.Id " +
                                                                 "JOIN site f ON a.SiteId = f.Id " +
                                                                 "JOIN device_class i ON a.ClassId = i.Id " +
                                                                 "JOIN script_version g ON a.ScriptId = g.Id WHERE a.Code = @code AND a.`MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceNotExist;
                return result;
            }
            var faultDevice = ServerConfig.ApiDb.Query<dynamic>("SELECT a.* FROM `fault_device_repair` a JOIN `device_library` b ON a.DeviceId = b.Id WHERE a.`State` != @state AND DeviceId = @DeviceId AND a.MarkedDelete = 0;",
                new { DeviceId = data.Id, state = RepairStateEnum.Complete }).FirstOrDefault();
            if (faultDevice != null)
            {
                data.RepairState = faultDevice.State;
            }

            var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.deviceSingleGate];
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
                    Log.Error($"{UrlMappings.Urls[UrlMappings.deviceSingleGate]},信息:{e}");
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
            result.classes.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, `Class` FROM `device_class` WHERE `MarkedDelete` = 0 ORDER BY Id DESC;"));

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
        [HttpGet("State")]
        public DataResult GetDeviceLibraryState([FromQuery] int qId, bool all)
        {
            var device =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, ScriptId FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = qId }).FirstOrDefault();
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
            var result = new DataResult();

            var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.batchSendBackGate];
            //向GateProxyLink请求数据
            var resp = HttpServer.Post(url, new Dictionary<string, string>{
                {"devicesList",(new List<DeviceInfo>
                    {
                        new DeviceInfo
                        {
                             DeviceId = qId,
                            Instruction = scriptVersion.HeartPacket
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
                                if (!all)
                                {

                                    var usuallyDictionaries =
                                        ServerConfig.ApiDb.Query<UsuallyDictionaryPrecision>("SELECT a.*, b.`Precision` FROM `usually_dictionary` a right JOIN `data_name_dictionary` b ON a.ScriptId = b.ScriptId AND a.DictionaryId = b.PointerAddress WHERE a.ScriptId = @ScriptId AND a.MarkedDelete = 0;", new { device.ScriptId });
                                    if (!usuallyDictionaries.Any())
                                    {
                                        return Result.GenError<DataResult>(Error.UsuallyDictionaryNotExist);
                                    }

                                    var usuallyDictionaryTypes = ServerConfig.ApiDb.Query<UsuallyDictionaryType>("SELECT `Id` FROM `usually_dictionary_type` WHERE MarkedDelete = 0 AND IsDetail = 1;");
                                    if (!usuallyDictionaryTypes.Any())
                                    {
                                        return Result.GenError<DataResult>(Error.UsuallyDictionaryTypeNotExist);
                                    }

                                    foreach (var usuallyDictionaryType in usuallyDictionaryTypes)
                                    {
                                        var usuallyDictionary =
                                            usuallyDictionaries.FirstOrDefault(
                                                x => x.VariableNameId == usuallyDictionaryType.Id);
                                        if (usuallyDictionary != null)
                                        {
                                            var v = 0;
                                            var dId = usuallyDictionary.DictionaryId - 1;
                                            var chu = Math.Pow(10, usuallyDictionary.Precision);
                                            switch (usuallyDictionary.VariableTypeId)
                                            {
                                                case 1:
                                                    if (res.vals.Count >= usuallyDictionary.DictionaryId)
                                                    {
                                                        v = res.vals[dId];
                                                        if (usuallyDictionary.VariableNameId == 6)
                                                        {
                                                            var flowCard = ServerConfig.ApiDb.Query<dynamic>("SELECT FlowCardName, ProductionProcessId FROM `flowcard_library` WHERE Id = @id AND MarkedDelete = 0;",
                                                                new { id = v }).FirstOrDefault();
                                                            if (flowCard != null)
                                                            {
                                                                if (flowCard.FlowCardName != null)
                                                                {
                                                                    result.datas.Add(new Tuple<int, string>(usuallyDictionaryType.Id, flowCard.FlowCardName));
                                                                }
                                                                var processNumber = ServerConfig.ApiDb.Query<dynamic>(
                                                                    "SELECT Id, ProcessNumber FROM `process_management` WHERE FIND_IN_SET(@DeviceId, DeviceIds) AND FIND_IN_SET(@ProductModel, ProductModels) AND MarkedDelete = 0;", new
                                                                    {
                                                                        DeviceId = qId,
                                                                        ProductModel = flowCard.ProductionProcessId
                                                                    }).FirstOrDefault();
                                                                if (processNumber != null)
                                                                {
                                                                    result.datas.Add(new Tuple<int, string>(-1, processNumber.ProcessNumber));
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result.datas.Add(new Tuple<int, string>(usuallyDictionaryType.Id, (v / chu).ToRound(usuallyDictionary.Precision).ToString()));
                                                        }
                                                    }
                                                    break;
                                                case 2:
                                                    if (res.ins.Count >= usuallyDictionary.DictionaryId)
                                                    {
                                                        v = res.ins[dId];
                                                        result.datas.Add(new Tuple<int, string>(usuallyDictionaryType.Id, (v / chu).ToRound(usuallyDictionary.Precision).ToString()));
                                                    }
                                                    break;
                                                case 3:
                                                    if (res.outs.Count >= usuallyDictionary.DictionaryId)
                                                    {
                                                        v = res.outs[dId];
                                                        result.datas.Add(new Tuple<int, string>(usuallyDictionaryType.Id, (v / chu).ToRound(usuallyDictionary.Precision).ToString()));
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var usuallyDictionaries =
                                        ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE ScriptId = @ScriptId AND MarkedDelete = 0;", new { device.ScriptId });
                                    if (!usuallyDictionaries.Any())
                                    {
                                        return Result.GenError<DataResult>(Error.UsuallyDictionaryNotExist);
                                    }
                                    var deviceTrueData = new DeviceTrueData();
                                    for (var i = 0; i < res.vals.Count; i++)
                                    {
                                        var value = res.vals[i];
                                        var usuallyDictionary = usuallyDictionaries.FirstOrDefault(x =>
                                            x.VariableTypeId == 1 && x.PointerAddress == (i + 1));
                                        if (usuallyDictionary != null)
                                        {
                                            var chu = Math.Pow(10, usuallyDictionary.Precision);
                                            deviceTrueData.vals.Add((decimal)(value / chu));
                                        }
                                    }

                                    for (var i = 0; i < res.ins.Count; i++)
                                    {
                                        var value = res.ins[i];
                                        var usuallyDictionary = usuallyDictionaries.FirstOrDefault(x =>
                                            x.VariableTypeId == 2 && x.PointerAddress == (i + 1));
                                        if (usuallyDictionary != null)
                                        {
                                            var chu = Math.Pow(10, usuallyDictionary.Precision);
                                            deviceTrueData.ins.Add((decimal)(value / chu));
                                        }
                                    }

                                    for (var i = 0; i < res.outs.Count; i++)
                                    {
                                        var value = res.outs[i];
                                        var usuallyDictionary = usuallyDictionaries.FirstOrDefault(x =>
                                            x.VariableTypeId == 3 && x.PointerAddress == (i + 1));
                                        if (usuallyDictionary != null)
                                        {
                                            var chu = Math.Pow(10, usuallyDictionary.Precision);
                                            deviceTrueData.outs.Add((decimal)(value / chu));
                                        }
                                    }
                                    result.datas.Add(deviceTrueData);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{UrlMappings.Urls[UrlMappings.batchSendBackGate]},信息:{e}");
                }
            }

            return result;
        }

        // PUT: api/DeviceLibrary
        [HttpPut]
        public object PutDeviceLibrary([FromBody] IEnumerable<DeviceLibrary> deviceLibraries)
        {
            if (deviceLibraries == null || !deviceLibraries.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (deviceLibraries.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.DeviceCodeNotEmpty);
            }

            if (deviceLibraries.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.DeviceIsExist);
            }

            if (deviceLibraries.Any(x => !IPAddress.TryParse(x.Ip, out _)))
            {
                return Result.GenError<Result>(Error.IpInvalid);
            }
            if (deviceLibraries.Any(x => x.Port < 0 || x.Port > 65535))
            {
                return Result.GenError<Result>(Error.PortInvalid);
            }

            if (deviceLibraries.GroupBy(x => new { x.Ip, x.Port }).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            var deviceIds = deviceLibraries.Select(x => x.Id);
            var data =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE Id IN @deviceIds AND `MarkedDelete` = 0;", new { deviceIds });
            if (data.Count() != deviceLibraries.Count())
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }
            var result = new DataResult();
            var codes = deviceLibraries.Select(x => x.Code);
            var ips = deviceLibraries.Select(x => x.Ip);
            var ports = deviceLibraries.Select(x => x.Port);

            var sames = DeviceLibraryHelper.GetHaveSameCode(0, codes, deviceIds);
            if (sames.Any())
            {
                result.datas.AddRange(sames.Select(x => x.Code));
                return Result.GenError<Result>(Error.IpPortIsExist);
            }
            sames = DeviceLibraryHelper.GetHaveSameIpPort(0, ips, ports, deviceIds);
            if (sames.Any())
            {
                result.datas.AddRange(sames.Select(x => $"{x.Ip} - {x.Port}"));
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            var deviceModelIds = deviceLibraries.Select(x => x.DeviceModelId);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id IN @deviceModelIds AND `MarkedDelete` = 0;", new { deviceModelIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }
            var firmwareIds = deviceLibraries.Select(x => x.FirmwareId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id IN @firmwareIds AND `MarkedDelete` = 0;", new { firmwareIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }
            var hardwareIds = deviceLibraries.Select(x => x.HardwareId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id IN @hardwareIds AND `MarkedDelete` = 0;", new { hardwareIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            var applicationIds = deviceLibraries.Select(x => x.ApplicationId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id IN @applicationIds AND `MarkedDelete` = 0;", new { applicationIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            var siteIds = deviceLibraries.Select(x => x.SiteId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id IN @siteIds AND `MarkedDelete` = 0;", new { siteIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            var scriptIds = deviceLibraries.Select(x => x.ScriptId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id IN @scriptIds AND `MarkedDelete` = 0;", new { scriptIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var deviceLibrary in deviceLibraries)
            {
                deviceLibrary.MarkedDateTime = markedDateTime;
            }
            DeviceLibraryHelper.Instance.Update(deviceLibraries);

            var updates = deviceLibraries.Where(x => data.Any(od => od.Id == x.Id) && data.First().Ip != x.Ip || data.First().Port != x.Port);
            if (updates.Any())
            {
                BatchHttpResponseErrAsync(updates.Select(x => new DeviceInfo
                {
                    DeviceId = x.Id,
                    Ip = x.Ip,
                    Port = x.Port,
                }), "batchUpdateDeviceGate", "PutDeviceLibrary");
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
                "UPDATE device_library SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, " +
                "`DeviceName` = @DeviceName, `MacAddress` = @MacAddress, `Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `ClassId` = @ClassId, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `Administrator` = @Administrator, " +
                "`Remark` = @Remark, `Icon` = @Icon WHERE `Id` = @Id;", deviceLibrary);

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
        public Result PostDeviceLibrary([FromBody]  IEnumerable<DeviceLibrary> deviceLibraries)
        {
            if (deviceLibraries == null || !deviceLibraries.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (deviceLibraries.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.DeviceCodeNotEmpty);
            }

            if (deviceLibraries.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.DeviceIsExist);
            }

            if (deviceLibraries.Any(x => !IPAddress.TryParse(x.Ip, out _)))
            {
                return Result.GenError<Result>(Error.IpInvalid);
            }
            if (deviceLibraries.Any(x => x.Port < 0 || x.Port > 65535))
            {
                return Result.GenError<Result>(Error.PortInvalid);
            }

            if (deviceLibraries.GroupBy(x => new { x.Ip, x.Port }).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            var result = new DataResult();
            var codes = deviceLibraries.Select(x => x.Code);
            var ips = deviceLibraries.Select(x => x.Ip);
            var ports = deviceLibraries.Select(x => x.Port);

            var sames = DeviceLibraryHelper.GetHaveSameCode(0, codes);
            if (sames.Any())
            {
                result.datas.AddRange(sames.Select(x => x.Code));
                return Result.GenError<Result>(Error.IpPortIsExist);
            }
            sames = DeviceLibraryHelper.GetHaveSameIpPort(0, ips, ports);
            if (sames.Any())
            {
                result.datas.AddRange(sames.Select(x => $"{x.Ip} - {x.Port}"));
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            var deviceModelIds = deviceLibraries.Select(x => x.DeviceModelId);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id IN @id AND `MarkedDelete` = 0;", new { deviceModelIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }
            var firmwareIds = deviceLibraries.Select(x => x.FirmwareId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id IN @firmwareId AND `MarkedDelete` = 0;", new { FirmwareIds = firmwareIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }
            var hardwareIds = deviceLibraries.Select(x => x.HardwareId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id IN @hardwareIds AND `MarkedDelete` = 0;", new { hardwareIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            var applicationIds = deviceLibraries.Select(x => x.ApplicationId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id IN @applicationIds AND `MarkedDelete` = 0;", new { applicationIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            var siteIds = deviceLibraries.Select(x => x.SiteId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id IN @siteIds AND `MarkedDelete` = 0;", new { siteIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            var scriptIds = deviceLibraries.Select(x => x.ScriptId);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id IN @scriptIds AND `MarkedDelete` = 0;", new { scriptIds }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var deviceLibrary in deviceLibraries)
            {
                deviceLibrary.CreateUserId = createUserId;
                deviceLibrary.MarkedDateTime = markedDateTime;
            }
            DeviceLibraryHelper.Instance.Add(deviceLibraries);
            var data = DeviceLibraryHelper.GetDetail(0, codes);

            ServerConfig.ApiDb.Execute("INSERT INTO npc_proxy_link (`DeviceId`) VALUES (@DeviceId);", data.Select(x => new { DeviceId = x.Id }));

            BatchHttpResponseErrAsync(data.Select(x => new DeviceInfo
            {
                DeviceId = x.Id,
                Ip = x.Ip,
                Port = x.Port,
            }), "batchAddDeviceGate", "PostDeviceLibrary");
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
                ServerConfig.ApiDb.Query<DeviceLibraryDetail>("SELECT a.*, IFNULL(b.DeviceCategoryId, 0) DeviceCategoryId FROM `device_library` a " +
                                                              "JOIN `device_model` b ON a.DeviceModelId = b.Id " +
                                                              "WHERE a.Id = @DeviceId AND a.`MarkedDelete` = 0;", new { processInfo.DeviceId }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            //var flowCard =
            //    ServerConfig.ApiDb.Query<FlowCardLibraryDetail>("SELECT a.*, b.RawMateriaName, c.ProductionProcessName FROM `flowcard_library` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id " +
            //                                                    "JOIN `production_library` c ON a.ProductionProcessId = c.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id = processInfo.FlowCardId }).FirstOrDefault();
            var flowCard = FlowCardHelper.Instance.Get<FlowCard>(processInfo.FlowCardId);
            if (flowCard == null)
            {
                return Result.GenError<Result>(Error.FlowCardLibraryNotExist);
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

            //当前配方
            //messagePacket.Vals.Add(98, flowCard.Id);
            var isSetProcessData = RedisHelper.Get<int>(ServerConfig.IsSetProcessDataKey) == 1;
            IEnumerable<dynamic> processDatas = null;
            var dictionaryIds =
                ServerConfig.ApiDb.Query<UsuallyDictionaryPrecision>("SELECT a.Id, VariableName, DictionaryId, b.`Precision` FROM `usually_dictionary_type` a  " +
                                                                     "JOIN (SELECT a.*, b.`Precision` FROM `usually_dictionary` a " +
                                                                     "JOIN `data_name_dictionary` b ON a.ScriptId = b.ScriptId AND a.DictionaryId = b.PointerAddress AND a.VariableTypeId = b.VariableTypeId " +
                                                                     "WHERE a.ScriptId = @ScriptId AND a.MarkedDelete = 0) b ON a.Id = b.VariableNameId  " +
                                                                     "WHERE b.ScriptId = @ScriptId AND a.MarkedDelete = 0 ORDER BY a.Id;", new
                                                                     {
                                                                         device.ScriptId,
                                                                     });

            if (!dictionaryIds.Any())
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryTypeNotExist);
            }
            if (isSetProcessData)
            {
                processDatas = ServerConfig.ApiDb.Query<dynamic>("SELECT `ProcessOrder`, `PressurizeMinute`, `PressurizeSecond`, `ProcessMinute`, `ProcessSecond`, " +
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


                var i = 1;
                foreach (var processData in processDatas)
                {
                    var j = 0;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.PressurizeMinute * chu);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.PressurizeSecond * chu);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.ProcessMinute * chu);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.ProcessSecond * chu);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.Pressure * chu);
                    }
                    j++;
                    if (dictionaryIds.Any(x => x.VariableName == key[j] + i))
                    {
                        var dd = dictionaryIds.First(x => x.VariableName == key[j] + i);
                        var chu = (int)Math.Pow(10, dd.Precision);
                        messagePacket.Vals.Add(dd.DictionaryId - 1, processData.Speed * chu);
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
            //下次加工流程卡号
            var dictionaryId = 113;
            if (dictionaryIds.Any(x => x.Id == dictionaryId))
            {
                messagePacket.Vals.Add(dictionaryIds.First(x => x.Id == dictionaryId).DictionaryId - 1, processInfo.FlowCardId);
            }

            //下次加工流程卡号
            if (processInfo.FlowCardId != 0)
            {
                dictionaryId = 119;
                if (dictionaryIds.Any(x => x.Id == dictionaryId))
                {
                    messagePacket.Vals.Add(dictionaryIds.First(x => x.Id == dictionaryId).DictionaryId - 1, flowCard.ProductionProcessId);
                }
            }

            var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.deviceSingleGate];
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
                        //if (deviceInfo.DeviceState != DeviceState.Waiting)
                        //{
                        //    return Result.GenError<Result>(deviceInfo.DeviceState == DeviceState.Processing ? Error.ProcessingNotSet : Error.DeviceStateErrorNotSet);
                        //}
                        if (deviceInfo.DeviceState != DeviceState.Waiting && deviceInfo.DeviceState != DeviceState.Processing)
                        {
                            return Result.GenError<Result>(Error.DeviceStateErrorNotSet);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{UrlMappings.Urls[UrlMappings.deviceSingleGate]},信息:{e}");
                return Result.GenError<Result>(Error.AnalysisFail);
            }

            var msg = messagePacket.Serialize();
            url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.batchSendBackGate];
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
                    ProcessData = processDatas?.ToJSON() ?? "[]"
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

            var url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.deviceSingleGate];
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
                Log.Error($"{UrlMappings.Urls[UrlMappings.deviceSingleGate]},信息:{e}");
                return Result.GenError<Result>(Error.AnalysisFail);
            }

            var dictionaryId =
                ServerConfig.ApiDb.Query<UsuallyDictionaryPrecision>("SELECT a.*, b.`Precision` FROM `usually_dictionary` a JOIN `data_name_dictionary` b ON a.DictionaryId = b.PointerAddress WHERE a.ScriptId = @ScriptId AND VariableNameId = @VariableNameId AND a.MarkedDelete = 0;",
                new
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
                var chu = (int)Math.Pow(10, dictionaryId.Precision);
                messagePacket.Vals.Add(dictionaryId.DictionaryId - 1, dataInfo.Value * chu);
            }
            else
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var msg = messagePacket.Serialize();
            url = ServerConfig.GateUrl + UrlMappings.Urls[UrlMappings.batchSendBackGate];
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
        // POST: api/DeviceLibrary/Upgrade
        [HttpPost("Upgrade")]
        public UpgradeResult Upgrade([FromBody] UpgradeInfos upgradeInfos)
        {
            if (upgradeInfos == null || !upgradeInfos.Infos.Any())
            {
                return Result.GenError<UpgradeResult>(Error.ParamError);
            }

            if (upgradeInfos.Infos.GroupBy(x => x.DeviceId).Any(y => y.Count() > 1))
            {
                return Result.GenError<UpgradeResult>(Error.UpgradeDeviceDuplicate);
            }
            var result = new UpgradeResult { Type = upgradeInfos.Type };
            var groups = upgradeInfos.Infos.GroupBy(x => new { x.Type, x.FileUrl }).Select(y => y.Key);
            foreach (var group in groups)
            {
                //脚本位置 0 本地  1 网络
                IEnumerable<string> file = null;
                switch (group.Type)
                {
                    case 0:
                        file = FileHelper.LocalFileTo16String(@group.FileUrl);
                        break;
                    case 1:
                        file = FileHelper.RemoteFileToBytes(@group.FileUrl);
                        break;
                }

                if (file == null || !file.Any())
                {
                    result.datas.AddRange(upgradeInfos.Infos.Where(x => x.Type == @group.Type && x.FileUrl == @group.FileUrl).Select(y =>
                    {
                        y.ErrNo = Error.FileNotExist;
                        return new DeviceErr(y.DeviceId, Error.FileNotExist);
                    }));
                    continue;
                }

                foreach (var info in upgradeInfos.Infos.Where(x => x.Type == @group.Type && x.FileUrl == @group.FileUrl))
                {
                    info.UpgradeFile = file.Join(",");
                }
            }

            var leftInfos = upgradeInfos.Infos.Where(x => x.ErrNo == Error.Success);
            if (!leftInfos.Any())
            {
                result.datas = result.datas.OrderBy(x => x.DeviceId).ToList();
                return result;
            }
            try
            {
                var mapKey = UrlMappings.deviceListGate;
                var url = ServerConfig.GateUrl + UrlMappings.Urls[mapKey];
                //向GateProxyLink请求数据
                var resp = HttpServer.Get(url, new Dictionary<string, string>{
                    { "ids", leftInfos.Select(x=>x.DeviceId).Join(",")}
                });
                if (resp != "fail")
                {
                    try
                    {
                        var dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                        if (dataResult.errno == Error.Success)
                        {
                            foreach (var info in leftInfos)
                            {
                                var device = dataResult.datas.FirstOrDefault(x => x.DeviceId == info.DeviceId);
                                if (device != null)
                                {
                                    if (device.DeviceState != DeviceState.Waiting)
                                    {
                                        info.ErrNo = Error.UpgradeDeviceStateError;
                                        result.datas.Add(new DeviceErr(info.DeviceId, Error.UpgradeDeviceStateError));
                                    }
                                }
                                else
                                {
                                    info.ErrNo = Error.DeviceException;
                                    result.datas.Add(new DeviceErr(info.DeviceId, Error.DeviceException));
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"{UrlMappings.Urls[mapKey]},信息:{e}");
                    }
                }
                if (!leftInfos.Any())
                {
                    result.datas = result.datas.OrderBy(x => x.DeviceId).ToList();
                    return result;
                }
                mapKey = UrlMappings.batchUpgradeGate;
                url = ServerConfig.GateUrl + UrlMappings.Urls[mapKey];
                //向GateProxyLink请求数据
                resp = HttpServer.Post(url, new UpgradeInfos { Type = upgradeInfos.Type, Infos = leftInfos.ToList() }.ToJSON());
                var fRes = resp != "fail";
                if (fRes)
                {
                    result.datas.AddRange(JsonConvert.DeserializeObject<UpgradeResult>(resp).datas);
                    result.datas = result.datas.OrderBy(x => x.DeviceId).ToList();
                    if (result.datas.Any(x => x.errno == Error.Success))
                    {
                        var f = false;
                        var sql = "";
                        object data = null;
                        //0  默认  1 升级流程脚本  2 升级固件  3 升级应用层
                        switch (upgradeInfos.Type)
                        {
                            case 1:
                                f = true;
                                sql =
                                    "UPDATE `device_library` SET `ScriptId` = @ScriptId WHERE `Id` = @Id;";
                                data = result.datas.Where(x => x.errno == Error.Success).Select(
                                    y => new
                                    {
                                        Id = y.DeviceId,
                                        ScriptId = upgradeInfos.Infos.FirstOrDefault(x => x.DeviceId == y.DeviceId)?.FileId ?? 0
                                    });
                                break;
                            case 2:
                                f = true;
                                sql =
                                    "UPDATE `device_library` SET `FirmwareId` = @FirmwareId WHERE `Id` = @Id;";
                                data = result.datas.Where(x => x.errno == Error.Success).Select(
                                    y => new
                                    {
                                        Id = y.DeviceId,
                                        FirmwareId = upgradeInfos.Infos.FirstOrDefault(x => x.DeviceId == y.DeviceId)?.FileId ?? 0
                                    });
                                break;
                            case 3:
                                f = true;
                                sql =
                                    "UPDATE `device_library` SET `HardwareId` = @HardwareId WHERE `Id` = @Id;";
                                data = result.datas.Where(x => x.errno == Error.Success).Select(
                                    y => new
                                    {
                                        Id = y.DeviceId,
                                        HardwareId = upgradeInfos.Infos.FirstOrDefault(x => x.DeviceId == y.DeviceId)?.FileId ?? 0
                                    });
                                break;
                            default: break;
                        }
                        if (f)
                        {
                            ServerConfig.ApiDb.Execute(sql, data);
                            RedisHelper.PublishToTable();
                        }
                    }
                    return result;
                }
                return Result.GenError<UpgradeResult>(Error.GateExceptionHappen);
            }
            catch (Exception e)
            {
                Log.Error(e);
                return Result.GenError<UpgradeResult>(Error.ExceptionHappen);
            }
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
        private static void BatchHttpResponseErrAsync(IEnumerable<DeviceInfo> deviceInfos, string urlKey, string funName, Action callback = null)
        {
            var url = ServerConfig.GateUrl + UrlMappings.Urls[urlKey];
            //向NpcProxyLink请求数据
            HttpServer.PostAsync(url, new Dictionary<string, string>
            {
                { "devicesList", deviceInfos.ToJSON()
                }
            }, (resp, exp) =>
            {
                Log.DebugFormat("{0} Res:{1}", funName, resp);
                callback?.Invoke();
            });
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