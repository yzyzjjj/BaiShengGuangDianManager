using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;
using ApiManagement.Models.AccountModel;

namespace ApiManagement.Controllers.OtherController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class FlowCardReportController : ControllerBase
    {
        // GET: api/FlowCardReport
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lck">流程卡</param>
        /// <param name="jth">机台号</param>
        /// <param name="gx">工序 研磨1 粗抛 2  精抛 3</param>
        /// <param name="jgqty">加工数</param>
        /// <param name="qty">合格数</param>
        /// <param name="lpqty">裂片数</param>
        /// <param name="back"></param>
        /// <param name="jgr"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetFlowCardReport([FromQuery] string lck, string jth, int gx, int jgqty, int qty, int lpqty, bool back = true, string jgr = "")
        {
            var time = DateTime.Now;
            //研磨1 粗抛 2  精抛 3
            Console.WriteLine($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, {back}");
            Log.Debug($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, {back}");
            var device = DeviceLibraryHelper.GetDetail(0, jth);
            var deviceId = device?.Id ?? 0;
            var flowCard = FlowCardHelper.GetFlowCard(lck);
            var flowCardId = flowCard?.Id ?? -1;
            var processor = AccountInfoHelper.GetAccountByName(jgr);
            var processorId = processor?.Id ?? 0;
            var production = flowCard != null ? ProductionHelper.Instance.Get<Production>(flowCard.ProductionProcessId) : null;
            var productionId = production?.Id ?? 0;
            var flowCardReport = new FlowCardReport
            {
                ProcessType = ProcessType.Process,
                Time = time,
                FlowCardId = flowCardId,
                FlowCard = lck,
                ProductionId = productionId,
                Production = production?.ProductionProcessName ?? "",
                DeviceId = deviceId,
                Code = jth,
                Step = gx,
                ProcessorId = processorId,
                Processor = jgr,
                Back = back,
                Total = jgqty,
                HeGe = qty,
                LiePian = lpqty,
                State = flowCardId == 0 ? 2 : (processorId == 0 ? 3 : (deviceId == 0 ? 4 : 1))
            };
            FlowCardReportHelper.Instance.Add(flowCardReport);
            if (processor != null && processor.ProductionRole.IsNullOrEmpty())
            {
                var change = false;
                if (processor.ProductionRole.IsNullOrEmpty())
                {
                    change = true;
                    processor.ProductionRole = "0";
                }
                else if (processor.ProductionRole.Contains('0'))
                {
                    change = true;
                    processor.ProductionRole += ",0";
                }
                if (processor.MaxProductionRole.IsNullOrEmpty())
                {
                    change = true;
                    processor.MaxProductionRole = "0";
                }
                else if (processor.MaxProductionRole.Contains('0'))
                {
                    change = true;
                    processor.MaxProductionRole += ",0";
                }
                if (change)
                {
                    ServerConfig.ApiDb.Execute("UPDATE `accounts` SET `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;", processor);
                }
            }

            //研磨1 粗抛 2  精抛 3
            if (AnalysisHelper.ParamIntDic.ContainsKey(gx))
            {
                var flowCardInfo = new
                {
                    Id = flowCardId,
                    MarkedDateTime = flowCardReport.Time,
                    FaChu = flowCardReport.Total,
                    flowCardReport.HeGe,
                    flowCardReport.LiePian,
                    flowCardReport.DeviceId,
                    flowCardReport.Time,
                    JiaGongRen = flowCardReport.Processor
                };

                ServerConfig.ApiDb.Execute(
                    $"UPDATE `flowcard_library` SET `MarkedDateTime` = @MarkedDateTime, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][1]}` = @FaChu, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][2]}` = @HeGe, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][3]}` = @LiePian, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][4]}` = @DeviceId, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][0]}` = @Time, " +
                    $"`{AnalysisHelper.ParamIntDic[gx][5]}` = @JiaGongRen WHERE `Id` = @Id;",
                    flowCardInfo);

                if (back)
                {
                    var processId = ServerConfig.ApiDb.Query<int>(
                        "SELECT Id FROM `npc_monitoring_process_log` " +
                        "WHERE DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime) AND FlowCardId != 0 ORDER BY StartTime DESC LIMIT 1;",
                        new
                        {
                            DeviceId = deviceId,
                            ProcessType = ProcessType.Process
                        }).FirstOrDefault();
                    if (processId != 0)
                    {
                        flowCardReport.Id = processId;
                        ServerConfig.ApiDb.Execute(
                            "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId, `Processor` = @Processor " +
                            "WHERE `Id` > @Id AND DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime);",
                            flowCardReport);
                        Log.Debug($"UPDATE 流程卡:{flowCardReport.FlowCard}, 流程卡Id:{flowCardReport.FlowCardId}, 机台号:{flowCardReport.Code}, 设备Id:{flowCardReport.DeviceId}, 工序:{flowCardReport.Step}, 加工人:{flowCardReport.Processor}, Id:{flowCardReport.Id}");
                    }
                }
            }

            return Result.GenError<Result>(Error.Success);
        }
    }
}