using ApiDeviceManagement.Base.Server;
using ApiDeviceManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.ServerConfig.Enum;
using ModelBase.Base.UrlMappings;
using ModelBase.Base.Utils;
using ModelBase.Models.Device;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack;

namespace ApiDeviceManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeviceLibraryController : ControllerBase
    {
        // GET: api/DeviceLibrary
        [HttpGet]
        public DataResult GetDeviceLibrary()
        {
            var result = new DataResult();
            var deviceLibraryDetails = ServerConfig.DeviceDb.Query<DeviceLibraryDetail>(
                "SELECT a.*, b.ModelName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, g.ScriptName FROM device_library a " +
                "JOIN device_model b ON a.DeviceModelId = b.Id " +
                "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                "JOIN application_library d ON a.ApplicationId = d.Id " +
                "JOIN hardware_library e ON a.HardwareId = e.Id " +
                "JOIN site f ON a.SiteId = f.Id " +
                "JOIN script_version g ON a.ScriptId = g.Id WHERE a.`MarkedDelete` = 0;").ToDictionary(x => x.Id);

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
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat($"{UrlMappings.Urls["deviceListGate"]} 返回：{resp},信息:{e.Message}");
                }
            }

            result.datas.AddRange(deviceLibraryDetails.Values);

            return result;
        }

        // GET: api/DeviceLibrary/5
        [HttpGet("{id}")]
        public DataResult GetDeviceLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, g.ScriptName FROM device_library a " +
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
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
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
                ServerConfig.DeviceDb.Query<DeviceLibraryDetail>("SELECT a.*, b.ModelName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, g.ScriptName FROM device_library a " +
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
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat($"{UrlMappings.Urls["deviceSingleGate"]} 返回：{resp},信息:{e.Message}");
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
            var deviceModels =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, ModelName FROM `device_model` WHERE `MarkedDelete` = 0;");
            result.deviceModels.AddRange(deviceModels);

            var firmwareLibraries =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, FirmwareName FROM `firmware_library` WHERE `MarkedDelete` = 0;");
            result.firmwareLibraries.AddRange(firmwareLibraries);

            var hardwareLibraries =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, HardwareName FROM `hardware_library` WHERE `MarkedDelete` = 0;");
            result.hardwareLibraries.AddRange(hardwareLibraries);

            var applicationLibraries =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, ApplicationName FROM `application_library` WHERE `MarkedDelete` = 0;");
            result.applicationLibraries.AddRange(applicationLibraries);

            var sites =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, SiteName FROM `site` WHERE `MarkedDelete` = 0;");
            result.sites.AddRange(sites);

            var scriptVersions =
                ServerConfig.DeviceDb.Query<dynamic>("SELECT Id, ScriptName FROM `script_version` WHERE `MarkedDelete` = 0;");
            result.scriptVersions.AddRange(scriptVersions);
            return result;
        }


        // PUT: api/DeviceLibrary/5
        [HttpPut("{id}")]
        public Result PutDeviceLibrary([FromRoute] int id, [FromBody] DeviceLibrary deviceLibrary)
        {
            var data =
                ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
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
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND `MarkedDelete` = 0;", new
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
            ServerConfig.DeviceDb.Execute(
                "UPDATE device_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `Code` = @Code, " +
                "`DeviceName` = @DeviceName, `MacAddress` = @MacAddress, `Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `AdministratorUser` = @AdministratorUser, " +
                "`Remark` = @Remark WHERE `Id` = @Id;", deviceLibrary);

            if (deviceLibrary.Ip != data.Ip || deviceLibrary.Port != data.Port)
            {
                HttpResponseErrAsync(new DeviceInfo
                {
                    DeviceId = deviceLibrary.Id,
                }, "delDeviceGate", "PutDeviceLibrary", () =>
                {
                    HttpResponseErrAsync(new DeviceInfo
                    {
                        DeviceId = deviceLibrary.Id,
                        Ip = deviceLibrary.Ip,
                        Port = deviceLibrary.Port,
                    }, "addDeviceGate", "PutDeviceLibrary");
                });
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
                ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE Code = @code AND `MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
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
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND Id != @Port;", new
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
            ServerConfig.DeviceDb.Execute(
                "UPDATE device_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `Code` = @Code, " +
                "`DeviceName` = @DeviceName, `MacAddress` = @MacAddress, `Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `AdministratorUser` = @AdministratorUser, " +
                "`Remark` = @Remark WHERE `Id` = @Id;", deviceLibrary);

            if (deviceLibrary.Id != data.Id || deviceLibrary.Ip != data.Ip || deviceLibrary.Port != data.Port)
            {
                HttpResponseErrAsync(new DeviceInfo
                {
                    DeviceId = deviceLibrary.Id,
                }, "delDeviceGate", "PutDeviceLibrary", () =>
                {
                    HttpResponseErrAsync(new DeviceInfo
                    {
                        DeviceId = deviceLibrary.Id,
                        Ip = deviceLibrary.Ip,
                        Port = deviceLibrary.Port,
                    }, "addDeviceGate", "PutDeviceLibrary");
                });
            }

            return Result.GenError<Result>(Error.Success);
        }



        // POST: api/DeviceLibrary
        [HttpPost]
        public Result PostDeviceLibrary([FromBody] DeviceLibrary deviceLibrary)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Ip = @Ip AND Port = @Port AND `MarkedDelete` = 0;", new
                {
                    deviceLibrary.Ip,
                    deviceLibrary.Port
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.IpPortIsExist);
            }

            cnt =
               ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.FirmwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.HardwareId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ApplicationId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.SiteId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }
            cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceLibrary.ScriptId }).FirstOrDefault();
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

            deviceLibrary.CreateUserId = Request.GetIdentityInformation();
            deviceLibrary.MarkedDateTime = DateTime.Now;
            var lastInsertId = ServerConfig.DeviceDb.Query<int>(
              "INSERT INTO device_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `Code`, `DeviceName`, `MacAddress`, `Ip`, `Port`, `Identifier`, `DeviceModelId`, " +
              "`ScriptId`, `FirmwareId`, `HardwareId`, `ApplicationId`, `SiteId`, `AdministratorUser`, `Remark`) VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, " +
              "@ModifyId, @Code, @DeviceName, @MacAddress, @Ip, @Port, @Identifier, @DeviceModelId, @ScriptId, @FirmwareId, @HardwareId, @ApplicationId, @SiteId, @AdministratorUser, " +
              "@Remark);SELECT LAST_INSERT_ID();",
              deviceLibrary).FirstOrDefault();

            ServerConfig.DeviceDb.Execute("INSERT INTO npc_proxy_link (`DeviceId`) VALUES (@DeviceId);", new { DeviceId = lastInsertId, });

            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = lastInsertId,
                Ip = deviceLibrary.Ip,
                Port = deviceLibrary.Port,
            }, "addDeviceGate", "PostDeviceLibrary");
            return Result.GenError<Result>(Error.Success);
        }






        // DELETE: api/DeviceLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `device_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = id,
            }, "delDeviceGate", "DeleteDeviceLibrary");
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
                ServerConfig.DeviceDb.Query<DeviceLibrary>("SELECT `Id` FROM `device_library` WHERE Code = @code AND `MarkedDelete` = 0;", new { code }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `device_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    data.Id
                });
            HttpResponseErrAsync(new DeviceInfo
            {
                DeviceId = data.Id,
            }, "delDeviceGate", "DeleteDeviceLibrary");
            return Result.GenError<Result>(Error.Success);
        }
        private static void HttpResponseErrAsync(DeviceInfo deviceInfo, string urlKey, string funName, Action callback = null)
        {
            var url = ServerConfig.GateUrl + UrlMappings.Urls[urlKey];
            //向NpcProxyLink请求数据
            HttpServer.PostAsync(url, new Dictionary<string, string>
            {
                {"deviceInfo", deviceInfo.ToJSON()}
            }, (resp, exp) =>
            {
                Log.DebugFormat("{0} Res:{1}", funName, resp);
                callback?.Invoke();
            });
        }
    }
}