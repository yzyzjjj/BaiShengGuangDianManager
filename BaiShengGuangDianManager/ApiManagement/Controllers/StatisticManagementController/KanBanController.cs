using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.RepairManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.UrlMappings;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.Device;

namespace ApiManagement.Controllers.StatisticManagementController
{
    /// <summary>
    /// 数据统计
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class KanBanController : ControllerBase
    {
        /// <summary>
        /// 看板数据
        /// </summary>
        /// <returns></returns>
        // POST: api/KanBan
        [HttpGet]
        public object GetSet([FromQuery] bool init = false, int qId = 0, int page = 0)
        {
            if (init)
            {
                var data = MonitoringKanBanSetHelper.GetDetail().ToList();
                data.Insert(0, new MonitoringKanBanSet
                {
                    Id = 0,
                    Name = "所有设备",
                    Type = MonitoringKanBanEnum.设备详情看板,
                    Order = -1,
                    IsShow = true
                });
                return new
                {
                    errno = 0,
                    errmsg = "成功",
                    menu = EnumHelper.EnumToList<MonitoringKanBanEnum>().Select(x => new { Id = x.EnumValue, Type = x.EnumName }),
                    data
                };
            }

            var set = qId != 0 ? MonitoringKanBanSetHelper.Instance.Get<MonitoringKanBanSet>(qId) : new MonitoringKanBanSet
            {
                Type = MonitoringKanBanEnum.设备详情看板
            };
            switch (set.Type)
            {
                case MonitoringKanBanEnum.设备详情看板:
                    return new
                    {
                        errno = 0,
                        errmsg = "成功",
                        time = DateTime.Now,
                        data = AnalysisHelper.MonitoringKanBanDic.ContainsKey(qId) ? AnalysisHelper.MonitoringKanBanDic[qId] : new MonitoringKanBan()
                    };
                case MonitoringKanBanEnum.设备状态看板:
                    {
                        var ret = new MonitoringKanBan();
                        page = page < 0 ? 0 : page;
                        var idList = set.DeviceIdList;
                        var deviceLibraryDetails = ServerConfig.ApiDb.Query<DeviceLibraryDetail>(
                            "SELECT a.*, b.ModelName, b.DeviceCategoryId, b.CategoryName, c.FirmwareName, d.ApplicationName, e.HardwareName, f.SiteName, f.RegionDescription, " +
                            "g.ScriptName, IFNULL(h.`Name`, '')  AdministratorName, i.`Class` FROM device_library a " +
                            $"JOIN (SELECT a.*, b.CategoryName FROM device_model a JOIN device_category b ON a.DeviceCategoryId = b.Id) b ON a.DeviceModelId = b.Id " +
                            "JOIN firmware_library c ON a.FirmwareId = c.Id " +
                            "JOIN application_library d ON a.ApplicationId = d.Id " +
                            "JOIN hardware_library e ON a.HardwareId = e.Id " +
                            "JOIN site f ON a.SiteId = f.Id " +
                            "JOIN script_version g ON a.ScriptId = g.Id " +
                            "JOIN device_class i ON a.ClassId = i.Id " +
                            "LEFT JOIN (SELECT * FROM(SELECT * FROM maintainer ORDER BY MarkedDelete)a GROUP BY a.Account) h ON a.Administrator = h.Account " +
                            $"WHERE a.`MarkedDelete` = 0" +
                            $"{(idList.Any() ? " AND a.Id IN @idList" : "")}" +
                            $" ORDER BY a.Id;", new { idList }).ToDictionary(x => x.Id);

                        var faultDevices = ServerConfig.ApiDb.Query<dynamic>(
                            $"SELECT * FROM (SELECT a.* FROM `fault_device_repair` a " +
                            $"JOIN `device_library` b ON a.DeviceId = b.Id " +
                            $"WHERE a.`State` != @state" +
                            $"{(idList.Any() ? " AND a.DeviceId IN @idList" : "")}" +
                            $" AND a.MarkedDelete = 0 ORDER BY a.DeviceId, a.State DESC ) a GROUP BY DeviceCode;",
                            new { state = RepairStateEnum.Complete, idList });
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
                        var resp = !idList.Any() ? HttpServer.Get(url) :
                            HttpServer.Get(url, new Dictionary<string, string>
                            {
                            {"ids", idList.Join()}
                            });
                        DeviceResult dataResult = null;
                        if (resp != "fail")
                        {
                            try
                            {
                                dataResult = JsonConvert.DeserializeObject<DeviceResult>(resp);
                                if (dataResult.errno == Error.Success)
                                {
                                    foreach (var deviceInfo in dataResult.datas)
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

                        //var data = deviceLibraryDetails.Values.All(x => int.TryParse(x.Code, out _))
                        //    ? deviceLibraryDetails.Values.OrderByDescending(x => x.DeviceState).ThenByDescending(x => x.DeviceStateStr).ThenBy(x => int.Parse(x.Code))
                        //    : deviceLibraryDetails.Values.OrderByDescending(x => x.DeviceState).ThenByDescending(x => x.DeviceStateStr).ThenBy(x => x.Code);

                        var allDevices = deviceLibraryDetails.Values;
                        var sum = allDevices.Count;
                        var gz = allDevices.Count(x => x.RepairState != -1);
                        var other = allDevices.Where(x => x.RepairState == -1);
                        var zc = other.Count(x => x.DeviceState == DeviceState.Processing)
                                 + other.Count(x => x.DeviceState == DeviceState.Restart)
                                 + other.Count(x => x.DeviceState == DeviceState.UpgradeFirmware)
                                 + other.Count(x => x.DeviceState == DeviceState.UpgradeScript)
                                 + other.Count(x => x.DeviceState == DeviceState.Waiting);
                        var jg = other.Count(x => x.DeviceState == DeviceState.Processing);
                        var xz = other.Count(x => x.DeviceState == DeviceState.Waiting);
                        var wlj = sum - zc;
                        var tp = (int)Math.Ceiling((decimal)sum / (set.Length <= 0 ? 30 : set.Length));
                        if (page >= tp)
                        {
                            page = 0;
                        }
                        var devices = allDevices.Skip(set.Length * page).Take(set.Length);
                        var scriptIds = set.VariableList.Select(y => y.ScriptId);
                        var dataNameDictionaries = scriptIds.Any() ? DataNameDictionaryHelper.GetDataNameDictionaryDetails(scriptIds) : new List<DataNameDictionaryDetail>();
                        ret.Id = qId;
                        foreach (var device in devices)
                        {
                            var t = ClassExtension.ParentCopyToChild<DeviceLibraryDetail, MonitoringSetData>(device);
                            var vs = set.VariableList.Where(v => v.ScriptId == device.ScriptId).OrderBy(x => x.Order);

                            DeviceData deviceData = null;
                            if (dataResult != null
                                && dataResult.datas.Any(d => d.Id == device.Id)
                                && dataResult.datas.First(d => d.Id == device.Id).DeviceData != null)
                            {
                                deviceData = dataResult.datas.First(d => d.Id == device.Id).DeviceData;
                            }

                            foreach (var x in vs)
                            {
                                var dn = dataNameDictionaries.FirstOrDefault(d =>
                                    d.VariableTypeId == x.VariableTypeId && d.PointerAddress == x.PointerAddress);

                                ////设备状态
                                //var stateDId = 1;
                                ////总加工次数
                                //var processCountDId = 63;
                                ////总加工时间
                                //var processTimeDId = 64;
                                ////当前加工流程卡号
                                //var currentFlowCardDId = 6;
                                ////累积运行总时间
                                //var runTimeDId = 5;

                                if (dn == null)
                                {
                                    continue;
                                }

                                var r = new MonitoringSetSingleDataDetail
                                {
                                    Sid = x.ScriptId,
                                    Type = x.VariableTypeId,
                                    Add = x.PointerAddress,
                                    VName = x.VariableName.IsNullOrEmpty() ? dn.VariableName ?? "" : x.VariableName,
                                };

                                if (dn.VariableTypeId == 1 && dn.VariableNameId == AnalysisHelper.stateDId)
                                {
                                    r.V = device.DeviceStateStr;
                                }
                                else if (dn.VariableTypeId == 1 && dn.VariableNameId == AnalysisHelper.currentFlowCardDId)
                                {
                                    r.V = device.FlowCard ?? "";
                                }
                                else if (deviceData != null)
                                {
                                    List<int> bl = null;
                                    switch (x.VariableTypeId)
                                    {
                                        case 1:
                                            bl = deviceData.vals;
                                            break;
                                        case 2:
                                            bl = deviceData.ins;
                                            break;
                                        case 3:
                                            bl = deviceData.outs;
                                            break;
                                    }
                                    if (bl != null)
                                    {
                                        if (bl.Count > x.PointerAddress - 1)
                                        {
                                            var chu = Math.Pow(10, dn.Precision);
                                            var v = (decimal)(bl.ElementAt(x.PointerAddress - 1) / chu);
                                            r.V = v.ToString();
                                        }
                                    }
                                }
                                t.Data.Add(r);
                            }
                            ret.MSetData.Add(t);
                        }
                        return new
                        {
                            errno = 0,
                            errmsg = "成功",
                            time = DateTime.Now,
                            zc,
                            jg,
                            xz,
                            gz,
                            wlj,
                            sum,
                            row = set.Row,
                            col = set.Col,
                            len = set.Length,
                            cp = page,
                            tp,
                            data = ret.MSetData
                        };
                    }
                default:
                    return new
                    {
                        errno = 0,
                        errmsg = "成功",
                        time = DateTime.Now,
                    };
            }
        }

        // PUT: api/KanBan/5
        [HttpPut]
        public Result PutSet([FromBody] MonitoringKanBanSet set)
        {
            if (set.Type == 0)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var sames = new List<string> { set.Name };
            var ids = new List<int> { set.Id };
            if (MonitoringKanBanSetHelper.GetHaveSame((int)set.Type, sames, ids))
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetIsExist);
            }
            var cnt = MonitoringKanBanSetHelper.Instance.GetCountById(set.Id);
            if (cnt != 1)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetNotExist);
            }

            if (set.Second <= 0)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetSecondError);
            }

            if (set.Type == MonitoringKanBanEnum.设备状态看板)
            {
                if (set.Row <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetRowError);
                }
                if (set.Col <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetColError);
                }

                if (set.Length <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetLengthError);
                }

                if (set.VariableList.Count == 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetVariableError);
                }
            }

            set.MarkedDateTime = DateTime.Now;
            MonitoringKanBanSetHelper.Instance.Update(set);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/KanBan
        [HttpPost]
        public Result PostSet([FromBody] MonitoringKanBanSet set)
        {
            if (set.Second <= 0)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetSecondError);
            }
            if (set.Type == MonitoringKanBanEnum.设备状态看板)
            {
                if (set.Row <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetRowError);
                }

                if (set.Col <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetColError);
                }

                if (set.Length <= 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetLengthError);
                }

                if (set.VariableList.Count == 0)
                {
                    return Result.GenError<Result>(Error.MonitoringKanBanSetVariableError);
                }
            }

            var sames = new List<string> { set.Name };
            if (MonitoringKanBanSetHelper.GetHaveSame((int)set.Type, sames))
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetIsExist);
            }
            set.CreateUserId = Request.GetIdentityInformation();
            set.MarkedDateTime = DateTime.Now;
            MonitoringKanBanSetHelper.Instance.Add(set);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/KanBan/5
        [HttpDelete("{id}")]
        public Result DeleteSet([FromRoute] int id)
        {
            var cnt = MonitoringKanBanSetHelper.Instance.GetCountById(id);
            if (cnt != 1)
            {
                return Result.GenError<Result>(Error.MonitoringKanBanSetNotExist);
            }
            MonitoringKanBanSetHelper.Instance.Delete(id);
            return Result.GenError<Result>(Error.Success);
        }
    }
}