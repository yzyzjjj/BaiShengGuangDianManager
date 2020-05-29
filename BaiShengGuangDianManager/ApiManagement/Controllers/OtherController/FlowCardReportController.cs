using ApiManagement.Base.Server;
using ApiManagement.Models.OtherModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.OtherController
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlowCardReportController : ControllerBase
    {
        // GET: api/FlowCardReport
        [HttpGet]
        public Result GetFlowCardReport([FromQuery] string lck, int jth, int gx, bool back = true, string jgr = "")
        {
            //研磨1 粗抛 2  精抛 3
            Console.WriteLine($"流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工人:{jgr}, {back}");
            Log.Debug($"流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工人:{jgr}, {back}");
            ServerConfig.ApiDb.Execute(
                "INSERT INTO flowcard_report (`Time`, `FlowCard`, `Code`, `Step`, `Back`) VALUES (@Time, @FlowCard, @Code, @Step, @Back);",
                new
                {
                    Time = DateTime.Now,
                    FlowCard = lck,
                    Code = jth,
                    Step = gx,
                    Back = back
                });

            var flowCardId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `flowcard_library` WHERE FlowCardName = @flowCard;",
                new
                {
                    flowCard = lck
                }).FirstOrDefault();
            var processorId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `processor` WHERE ProcessorName = @name;",
                new
                {
                    name = jgr,
                }).FirstOrDefault();

            var deviceId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `device_library` WHERE `Code` = @code;",
                new { code = jth }).FirstOrDefault();

            var flowCardPre = "FlowCard";
            var flowCardDeviceKey = $"{flowCardPre}:Device";
            var flowCardLockKey = $"{flowCardPre}:Lock";
            var deviceList = ServerConfig.RedisHelper.Get<IEnumerable<FlowCardReport>>(flowCardDeviceKey);
            if (flowCardId != 0 && deviceList.Any(x => x.DeviceId == deviceId))
            {
                switch (gx)
                {
                    case 1:
                        break;

                    case 2:
                        if (!back)
                        {
                            break;
                        }

                        if (ServerConfig.RedisHelper.Exists(flowCardDeviceKey))
                        {
                            var currentDeviceListDb = ServerConfig.ApiDb.Query<FlowCardReport>(
                                "SELECT Id, DeviceId, FlowCardId, StartTime FROM `npc_monitoring_process_log` WHERE OpName = '加工' AND NOT ISNULL(EndTime) AND DeviceId = @DeviceId ORDER BY StartTime DESC;",
                                new
                                {
                                    DeviceId = deviceId
                                });

                            if (currentDeviceListDb.Any())
                            {
                                FlowCardReport first = null;
                                foreach (var x in currentDeviceListDb)
                                {
                                    if (x.FlowCardId == 0)
                                    {
                                        first = x;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (first != null)
                                {
                                    var param = currentDeviceListDb.Where(x => x.Id >= first.Id).Select(y => new
                                    {
                                        FlowCardId = flowCardId,
                                        FlowCard = lck,
                                        ProcessorId = processorId,
                                        Id = y.Id,
                                        DeviceId = deviceId
                                    });
                                    ServerConfig.ApiDb.Execute(
                                        "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId WHERE `Id` = @Id AND DeviceId = @DeviceId AND `FlowCardId` = 0;",
                                        param);
                                    deviceList.First(x => x.DeviceId == deviceId).Id =
                                        currentDeviceListDb.First(x => x.DeviceId == deviceId).Id;
                                    Log.Debug($"UPDATE 流程卡:{lck}, 流程卡Id:{flowCardId}, 机台号:{jth}, 设备Id:{deviceId}, 工序:{gx}, 加工人:{jgr}, Id:{param.LastOrDefault()?.Id ?? 0} - {param.FirstOrDefault()?.Id ?? 0}");
                                }
                                ServerConfig.RedisHelper.SetForever(flowCardDeviceKey, deviceList);
                            }
                        }
                        //AnalysisHelper.FlowCardReport(true);
                        break;
                    case 3:
                        break;
                }


            }

            return Result.GenError<Result>(Error.Success);
        }
    }
}