using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.OtherModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Models.Result;

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
            if (flowCardId != 0)
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

                        if (ServerConfig.RedisHelper.Exists(AnalysisHelper.FlowCardDeviceKey))
                        {
                            var deviceId = ServerConfig.ApiDb.Query<int>("SELECT Id FROM `device_library` WHERE `Code` = @code;",
                                new { code = jth }).FirstOrDefault();
                            var deviceList = ServerConfig.RedisHelper.Get<IEnumerable<FlowCardReport>>(AnalysisHelper.FlowCardDeviceKey);
                            if (deviceList.Any(x => x.DeviceId == deviceId))
                            {
                                var currentDeviceListDb = ServerConfig.ApiDb.Query<FlowCardReport>(
                                    "SELECT MAX(id) Id, DeviceId FROM `npc_monitoring_process_log` WHERE OpName = '加工' AND NOT ISNULL(EndTime) GROUP BY DeviceId;");

                                if (currentDeviceListDb.Any(x => x.DeviceId == deviceId))
                                {
                                    var param = new
                                    {
                                        FlowCardId = flowCardId,
                                        ProcessorId = processorId,
                                        Id1 = deviceList.First(x => x.DeviceId == deviceId).Id,
                                        Id2 = currentDeviceListDb.First(x => x.DeviceId == deviceId).Id,
                                        DeviceId = deviceId
                                    };
                                    ServerConfig.ApiDb.Execute(
                                        "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `ProcessorId` = @ProcessorId WHERE `Id` > @Id1 AND `Id` <= @Id2 AND DeviceId = @DeviceId;",
                                        param);
                                    deviceList.First(x => x.DeviceId == deviceId).Id =
                                        currentDeviceListDb.First(x => x.DeviceId == deviceId).Id;

                                    ServerConfig.RedisHelper.SetForever(AnalysisHelper.FlowCardDeviceKey, deviceList);
                                    Log.Debug($"UPDATE 流程卡:{lck}, 流程卡Id:{flowCardId}, 机台号:{jth}, 设备Id:{deviceId}, 工序:{gx}, 加工人:{jgr}, Id:{param.Id1} - {param.Id2}");
                                }
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