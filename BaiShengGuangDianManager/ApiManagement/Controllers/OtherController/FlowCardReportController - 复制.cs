//using ApiManagement.Base.Helper;
//using ApiManagement.Base.Server;
//using ApiManagement.Models.AccountModel;
//using ApiManagement.Models.DeviceManagementModel;
//using ApiManagement.Models.FlowCardManagementModel;
//using ApiManagement.Models.OtherModel;
//using Microsoft.AspNetCore.Mvc;
//using ModelBase.Base.EnumConfig;
//using ModelBase.Base.Logger;
//using ModelBase.Base.Utils;
//using ModelBase.Models.Result;
//using ServiceStack;
//using System;using ModelBase.Models.BaseModel;
//using System.Collections.Generic;
//using System.Linq;using ModelBase.Models.BaseModel;
//using ApiManagement.Models.StatisticManagementModel;

//namespace ApiManagement.Controllers.OtherController
//{
//    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
//    public class FlowCardReportController : ControllerBase
//    {
//        // GET: api/FlowCardReport
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="lck">流程卡</param>
//        /// <param name="jth">机台号</param>
//        /// <param name="gx">工序 研磨1 粗抛 2  精抛 3</param>
//        /// <param name="jgqty">加工数</param>
//        /// <param name="qty">合格数</param>
//        /// <param name="lpqty">裂片数</param>
//        /// <param name="back"></param>
//        /// <param name="jgr"></param>
//        /// <returns></returns>
//        [HttpGet]
//        public Result GetFlowCardReport([FromQuery] string lck, string jth, int gx, int jgqty, int qty, int lpqty, bool back = true, string jgr = "", string reason = "", bool last = false)
//        {
//            var time = DateTime.Now;
//            //研磨1 粗抛 2  精抛 3
//            Console.WriteLine($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, 原因:{reason}, 末道:{last}, {back}");
//            Log.Debug($"时间:{time.ToStr()}, 流程卡:{lck}, 机台号:{jth}, 工序:{gx}, 加工数:{jgqty}, 合格数:{qty}, 裂片数:{lpqty}, 加工人:{jgr}, 原因:{reason}, 末道:{last}, {back}");
//            var device = DeviceLibraryHelper.GetDetail(0, jth);
//            var deviceId = device?.Id ?? 0;
//            var flowCard = FlowCardHelper.GetFlowCard(lck);
//            var flowCardId = flowCard?.Id ?? -1;
//            var processor = AccountInfoHelper.GetAccountByName(jgr);
//            var processorId = processor?.Id ?? 0;
//            var production = flowCard != null ? ProductionHelper.Instance.Get<Production>(flowCard.ProductionProcessId) : null;
//            var productionId = production?.Id ?? 0;
//            var flowCardReport = new FlowCardReport
//            {
//                ProcessType = ProcessType.Process,
//                Time = time,
//                FlowCardId = flowCardId,
//                FlowCard = lck,
//                ProductionId = productionId,
//                Production = production?.ProductionProcessName ?? "",
//                DeviceId = deviceId,
//                Code = jth,
//                Step = gx,
//                ProcessorId = processorId,
//                Processor = jgr,
//                Back = back,
//                Total = jgqty,
//                HeGe = qty,
//                LiePian = lpqty,
//                //State = flowCardId == 0 ? 2 : (processorId == 0 ? 3 : (deviceId == 0 ? 4 : 1)),
//                Last = last,
//                Reason = reason,
//            };
//            FlowCardReportHelper.Instance.Add(flowCardReport);
//            if (processor != null && processor.ProductionRole.IsNullOrEmpty())
//            {
//                var change = false;
//                if (processor.ProductionRole.IsNullOrEmpty())
//                {
//                    change = true;
//                    processor.ProductionRole = "0";
//                }
//                else if (processor.ProductionRole.Contains('0'))
//                {
//                    change = true;
//                    processor.ProductionRole += ",0";
//                }
//                if (processor.MaxProductionRole.IsNullOrEmpty())
//                {
//                    change = true;
//                    processor.MaxProductionRole = "0";
//                }
//                else if (processor.MaxProductionRole.Contains('0'))
//                {
//                    change = true;
//                    processor.MaxProductionRole += ",0";
//                }
//                if (change)
//                {
//                    ServerConfig.ApiDb.Execute("UPDATE `accounts` SET `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;", processor);
//                }
//            }

//            ////研磨1 粗抛 2  精抛 3
//            //if (AnalysisHelper.ParamIntDic.ContainsKey(gx))
//            //{
//            //    var flowCardInfo = new
//            //    {
//            //        Id = flowCardId,
//            //        MarkedDateTime = flowCardReport.Time,
//            //        FaChu = flowCardReport.Total,
//            //        flowCardReport.HeGe,
//            //        flowCardReport.LiePian,
//            //        flowCardReport.DeviceId,
//            //        flowCardReport.Time,
//            //        JiaGongRen = flowCardReport.Processor
//            //    };

//            //    ServerConfig.ApiDb.Execute(
//            //        $"UPDATE `flowcard_library` SET `MarkedDateTime` = @MarkedDateTime, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][1]}` = @FaChu, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][2]}` = @HeGe, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][3]}` = @LiePian, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][4]}` = @DeviceId, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][0]}` = @Time, " +
//            //        $"`{AnalysisHelper.ParamIntDic[gx][5]}` = @JiaGongRen WHERE `Id` = @Id;",
//            //        flowCardInfo);

//            //    if (back)
//            //    {
//            //        var processId = ServerConfig.ApiDb.Query<int>(
//            //            "SELECT Id FROM `npc_monitoring_process_log` " +
//            //            "WHERE DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime) AND FlowCardId != 0 ORDER BY StartTime DESC LIMIT 1;",
//            //            new
//            //            {
//            //                DeviceId = deviceId,
//            //                ProcessType = ProcessType.Process
//            //            }).FirstOrDefault();
//            //        if (processId != 0)
//            //        {
//            //            flowCardReport.Id = processId;
//            //            ServerConfig.ApiDb.Execute(
//            //                "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId, `Processor` = @Processor " +
//            //                "WHERE `Id` > @Id AND DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime);",
//            //                flowCardReport);
//            //            Log.Debug($"UPDATE 流程卡:{flowCardReport.FlowCard}, 流程卡Id:{flowCardReport.FlowCardId}, 机台号:{flowCardReport.Code}, 设备Id:{flowCardReport.DeviceId}, 工序:{flowCardReport.Step}, 加工人:{flowCardReport.Processor}, Id:{flowCardReport.Id}");
//            //        }
//            //    }
//            //}

//            return Result.GenError<Result>(Error.Success);
//        }


//        // GET: api/FlowCardReport
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="lck">流程卡</param>
//        /// <param name="jth">机台号</param>
//        /// <param name="gx">工序 研磨1 粗抛 2  精抛 3</param>
//        /// <param name="jgqty">加工数</param>
//        /// <param name="qty">合格数</param>
//        /// <param name="lpqty">裂片数</param>
//        /// <param name="back"></param>
//        /// <param name="jgr"></param>
//        /// <returns></returns>
//        [HttpPost]
//        public Result PostFlowCardReport([FromBody] IEnumerable<ErpFlowCardReport> fcs)
//        {
//            try
//            {
//                if (fcs == null || !fcs.Any())
//                {
//                    return Result.GenError<Result>(Error.Fail);
//                }
//                if (fcs.Any(x => x.time == default(DateTime)))
//                {
//                    return Result.GenError<Result>(Error.Fail);
//                }
//                var time = DateTime.Now;
//                Console.WriteLine($"批量上报，时间:{time.ToStr()}, 流程卡:{fcs.Count()}");
//                Log.Debug($"批量上报，时间:{time.ToStr()}, 流程卡:{fcs.Count()}");
//                var devices = DeviceLibraryHelper.GetDetail(0, fcs.Select(x => x.jth).Distinct()).ToDictionary(x => x.Code);
//                var flowCards = FlowCardHelper.GetFlowCards(fcs.Select(x => x.lck).Distinct()).ToDictionary(x => x.FlowCardName);
//                var processors = AccountInfoHelper.GetAccountByName(fcs.Select(x => x.jgr).Distinct()).ToDictionary(x => x.Name);
//                var productions = flowCards.Any()
//                    ? ProductionHelper.Instance.GetByIds<Production>(flowCards.Values.Select(x => x.ProductionProcessId).Distinct()).ToDictionary(x => x.Id)
//                    : new Dictionary<int, Production>();
//                var addFcs = new List<FlowCardReport>();
//                foreach (var fc in fcs)
//                {
//                    //研磨1 粗抛 2  精抛 3
//                    Console.WriteLine($"时间:{fc.time.ToStr()}, 流程卡:{fc.lck}, 机台号:{fc.jth}, 工序:{fc.gx}, 加工数:{fc.jgqty}, 合格数:{fc.qty}, 裂片数:{fc.lpqty}, 加工人:{fc.jgr}, 原因:{fc.reason}, 末道:{fc.last}, {fc.back}");
//                    Log.Debug($"时间:{fc.time.ToStr()}, 流程卡:{fc.lck}, 机台号:{fc.jth}, 工序:{fc.gx}, 加工数:{fc.jgqty}, 合格数:{fc.qty}, 裂片数:{fc.lpqty}, 加工人:{fc.jgr}, 原因:{fc.reason}, 末道:{fc.last}, {fc.back}");

//                    //var deviceId = devices.ContainsKey(fc.jth) ? devices[fc.jth].Id : 0;
//                    //var flowCard = flowCards.ContainsKey(fc.lck) ? flowCards[fc.lck] : null;
//                    //var flowCardId = flowCard?.Id ?? 0;
//                    //var productionProcessId = flowCard?.ProductionProcessId ?? 0;
//                    //var production = productions.ContainsKey(productionProcessId) ? productions[productionProcessId] : null;
//                    //var productionId = production?.Id ?? 0;
//                    //var processor = processors.ContainsKey(fc.jgr) ? processors[fc.jgr] : null;
//                    //var processorId = processor?.Id ?? 0;
//                    var flowCardReport = new FlowCardReport
//                    {
//                        ProcessType = ProcessType.Process,
//                        Time = time,
//                        //FlowCardId = flowCardId,
//                        FlowCard = fc.lck,
//                        //ProductionId = productionId,
//                        //Production = production?.ProductionProcessName ?? "",
//                        //DeviceId = deviceId,
//                        Code = fc.jth,
//                        Step = fc.gx,
//                        //ProcessorId = processorId,
//                        Processor = fc.jgr,
//                        Total = fc.jgqty,
//                        HeGe = fc.qty,
//                        LiePian = fc.lpqty,
//                        Back = fc.back,
//                        Last = fc.last,
//                        //State = 0,
//                        Reason = fc.reason,
//                    };
//                    addFcs.Add(flowCardReport);
//                    //if (processor != null && processor.ProductionRole.IsNullOrEmpty())
//                    //{
//                    //    var change = false;
//                    //    if (processor.ProductionRole.IsNullOrEmpty())
//                    //    {
//                    //        change = true;
//                    //        processor.ProductionRole = "0";
//                    //    }
//                    //    else if (processor.ProductionRole.Contains('0'))
//                    //    {
//                    //        change = true;
//                    //        processor.ProductionRole += ",0";
//                    //    }
//                    //    if (processor.MaxProductionRole.IsNullOrEmpty())
//                    //    {
//                    //        change = true;
//                    //        processor.MaxProductionRole = "0";
//                    //    }
//                    //    else if (processor.MaxProductionRole.Contains('0'))
//                    //    {
//                    //        change = true;
//                    //        processor.MaxProductionRole += ",0";
//                    //    }
//                    //    if (change)
//                    //    {
//                    //        updateAcs.Add(processor);
//                    //    }
//                    //}

//                    ////研磨1 粗抛 2  精抛 3
//                    //if (AnalysisHelper.ParamIntDic.ContainsKey(fc.gx))
//                    //{
//                    //    if (gxFcs.ContainsKey(fc.gx))
//                    //    {
//                    //        gxFcs.Add(fc.gx, new List<ErpUpdateFlowCard>());
//                    //    }
//                    //    var flowCardInfo = new ErpUpdateFlowCard
//                    //    {
//                    //        Id = flowCardId,
//                    //        MarkedDateTime = flowCardReport.Time,
//                    //        FaChu = flowCardReport.Total,
//                    //        HeGe = flowCardReport.HeGe,
//                    //        LiePian = flowCardReport.LiePian,
//                    //        DeviceId = flowCardReport.DeviceId,
//                    //        Time = flowCardReport.Time,
//                    //        JiaGongRen = flowCardReport.Processor
//                    //    };
//                    //    gxFcs[fc.gx].Add(flowCardInfo);
//                    //}
//                    //if (fc.back)
//                    //{
//                    //    var processId = ServerConfig.ApiDb.Query<int>(
//                    //        "SELECT Id FROM `npc_monitoring_process_log` " +
//                    //        "WHERE DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime) AND FlowCardId != 0 ORDER BY StartTime DESC LIMIT 1;",
//                    //        new
//                    //        {
//                    //            DeviceId = deviceId,
//                    //            ProcessType = ProcessType.Process
//                    //        }).FirstOrDefault();
//                    //    if (processId != 0)
//                    //    {
//                    //        flowCardReport.Id = processId;
//                    //        ServerConfig.ApiDb.Execute(
//                    //            "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId, `Processor` = @Processor " +
//                    //            "WHERE `Id` > @Id AND StartTime < @Time AND DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime);",
//                    //            flowCardReport);
//                    //        Log.Debug($"UPDATE 流程卡:{flowCardReport.FlowCard}, 流程卡Id:{flowCardReport.FlowCardId}, 机台号:{flowCardReport.Code}, " +
//                    //                  $"设备Id:{flowCardReport.DeviceId}, 工序:{flowCardReport.Step}, 加工人:{flowCardReport.Processor}, Id:{flowCardReport.Id}");
//                    //    }
//                    //}
//                }
//                if (addFcs.Any())
//                {
//                    FlowCardReportHelper.Instance.Add(addFcs);
//                }

//                if (updateAcs.Any())
//                {
//                    ServerConfig.ApiDb.Execute("UPDATE `accounts` SET `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;", updateAcs);
//                }

//                Console.WriteLine("批量上报完成");
//                Log.Debug("批量上报完成");
//                return Result.GenError<Result>(Error.Success);
//            }
//            catch (Exception e)
//            {
//                Log.Error(e);
//                return Result.GenError<Result>(Error.Fail);
//            }
//        }
//    }
//}