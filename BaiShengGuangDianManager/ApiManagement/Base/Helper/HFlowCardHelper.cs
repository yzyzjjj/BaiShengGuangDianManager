using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.OtherModel;
using ApiManagement.Models.StatisticManagementModel;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using BadTypeCount = ApiManagement.Models.DeviceManagementModel.BadTypeCount;

namespace ApiManagement.Base.Helper
{
    public class HFlowCardHelper
    {
        private static readonly string fRedisPre = "FlowCard";

        #region 10Lock
        private static readonly string lock_10Key = $"{fRedisPre}:Lock_10";
        #endregion

        #region 30Lock
        private static readonly string lock_30Key = $"{fRedisPre}:Lock_30";
        #endregion

        #region 60Lock
        private static readonly string lock_60Key = $"{fRedisPre}:Lock_60";
        #endregion

        #region DeviceLastFlowCard
        private static readonly string dfcLockKey = $"{fRedisPre}:DeviceFcLock";
        private static readonly string pTimeKey = $"{fRedisPre}:生产数据Time";
        private static readonly string pIdKey = $"{fRedisPre}:生产数据Id";
        #endregion

        private static Timer _timer10;
        private static Timer _timer30;
        private static Timer _timer60;
        private static int _id = 0;
        private static string _createUserId = "ErpSystem";
        private static string _url = ServerConfig.ErpUrl;
        private static bool _isInsert;
        private static bool _isUpdate;
        private static bool _isUpdateFlowCardProcessStep;
        private static bool _isUpdateProductionProcessStep;
        private static bool _isUpdateProcessStep;
        private static bool _isGetFlowCardReport;
        private static bool _isGetProductionPlan;
        private static bool _isUpdateProductionProcess;
        private static bool _isUpdateProductionSpecification;
        private static readonly Dictionary<string, string> ProcessStepName = new Dictionary<string, string>
        {
            {"线切割", "线切割"},
            {"精磨", "精磨"},
            {"Polish", "粗抛"},
            {"抛光", "粗抛"},
            {"粗抛", "粗抛"},
            {"精抛", "精抛"},
        };

        public static void Init()
        {
#if DEBUG
            Console.WriteLine("FlowCardHelper 调试模式已开启");
            //UpdateProcessStep();
            //GetFlowCardReport();
            //GetProductionPlan();
            _timer10 = new Timer(DoSth10, null, 5000, 1000 * 10 * 1);
            _timer30 = new Timer(DoSth30, null, 5000, 1000 * 30 * 1);
            _timer60 = new Timer(DoSth60, null, 5000, 1000 * 60 * 1);
#else
            Console.WriteLine("FlowCardHelper 发布模式已开启");
            _timer10 = new Timer(DoSth10, null, 5000, 1000 * 10 * 1);
            _timer30 = new Timer(DoSth30, null, 5000, 1000 * 30 * 1);
            _timer60 = new Timer(DoSth60, null, 5000, 1000 * 60 * 1);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private static void DoSth10(object state)
        {
            if (RedisHelper.SetIfNotExist(lock_10Key, DateTime.Now.ToStr()))
            {
                RedisHelper.SetExpireAt(lock_10Key, DateTime.Now.AddMinutes(5));
                UpdateProcessStep();
                UseFlowCardReportGet();
                //UseFlowCardReport();
                //UseFlowCardReportGet();
                //DeviceLastFlowCard();
                RedisHelper.Remove(lock_10Key);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void DoSth30(object state)
        {
            if (RedisHelper.SetIfNotExist(lock_30Key, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(lock_30Key, DateTime.Now.AddMinutes(5));
                    GetProductionPlan();
                    GetFlowCardReport();
                    DeviceLastFlowCard();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(lock_30Key);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void DoSth60(object state)
        {
            if (RedisHelper.SetIfNotExist(lock_60Key, DateTime.Now.ToStr()))
            {
                try
                {
                    RedisHelper.SetExpireAt(lock_60Key, DateTime.Now.AddMinutes(5));
                    UpdateProductionProcessStep();
                    UpdateProductionSpecification();
                    UpdateProductionProcess();

                    Insert();
                    Update();
                    UpdateFlowCardProcessStep();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                RedisHelper.Remove(lock_60Key);
            }
        }

        /// <summary>
        /// 插入流程卡 计划号 原材料
        /// </summary>
        private static void Insert()
        {
            if (_isInsert)
            {
                return;
            }

            _isInsert = true;
            var sId =
                ServerConfig.ApiDb.Query<int>("SELECT `FId` FROM `flowcard_library` ORDER BY FId DESC LIMIT 1;", 120).FirstOrDefault();

            var queryId1 = _id;
            var queryId2 = sId;
            var r = InsertFlowCard(queryId2);
            _id = !r ? queryId1 : queryId2;
            _isInsert = false;
        }

        /// <summary>
        /// 插入流程卡 计划号 原材料
        /// </summary>
        private static bool InsertFlowCard(int id)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getHairpin" },
                { "id", id.ToString()},
            });
            if (f == "fail")
            {
                Log.ErrorFormat("InsertFlowCard 请求erp获取流程卡数据失败,url:{0}", _url);
                return false;
            }

            var now = DateTime.Now;
            try
            {
                var rr = HttpUtility.UrlDecode(f);
                var res = JsonConvert.DeserializeObject<ErpRes>(rr);
                if (res.result != "ok")
                {
                    Log.ErrorFormat("InsertData 请求erp获取流程卡数据返回错误,原因:{0}", res.result);
                    return false;
                }

                var relation = res.relation;
                //卡类型
                var flowCardTypes = ServerConfig.ApiDb.Query<FlowCardType>("SELECT * FROM `flowcard_type`;").ToDictionary(x => x.Id);
                var erpFlowCardTypes = relation.ToDictionary(x => x.id);
                var newFlowCardTypes = erpFlowCardTypes.Where(x => !flowCardTypes.ContainsKey(x.Key));
                var newWs = newFlowCardTypes.Select(x => new FlowCardType
                {
                    Id = x.Key,
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    TypeName = erpFlowCardTypes[x.Key].name,
                    Abbre = erpFlowCardTypes[x.Key].abbre
                });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO flowcard_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `TypeName`, `Abbre`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @TypeName, @Abbre);",
                    newWs);

                var updateFct = flowCardTypes.Values.Where(x => erpFlowCardTypes.ContainsKey(x.Id) && (x.TypeName != erpFlowCardTypes[x.Id].name || x.Abbre != erpFlowCardTypes[x.Id].abbre));
                var updateFcts = new List<FlowCardType>();
                foreach (var ws in updateFct)
                {
                    ws.MarkedDateTime = DateTime.Now;
                    ws.TypeName = erpFlowCardTypes[ws.Id].name;
                    ws.Abbre = erpFlowCardTypes[ws.Id].abbre;
                    updateFcts.Add(ws);
                }
                ServerConfig.ApiDb.Execute("UPDATE flowcard_type SET `MarkedDateTime` = @MarkedDateTime, `TypeName` = @TypeName, `Abbre` = @Abbre WHERE `Id` = @Id;", updateFcts);

                flowCardTypes = ServerConfig.ApiDb.Query<FlowCardType>("SELECT * FROM `flowcard_type`;").ToDictionary(x => x.Id);

                var r = res.data;
                if (r.Count <= 0)
                {
                    return true;
                }

                foreach (var dr in r)
                {
                    dr.f_jhh = dr.f_jhh.Trim();
                    dr.f_lckh = dr.f_lckh.Trim();
                    dr.f_mate = dr.f_mate.Trim();
                }
                //原料批号
                var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE MarkedDelete = 0;").ToDictionary(x => x.RawMateriaName);
                var erpRawMaterias = r.GroupBy(x => x.f_mate).ToDictionary(x => x.Key);
                var newRawMaterias = erpRawMaterias.Where(x => !rawMaterias.ContainsKey(x.Key));
                var newRm = newRawMaterias.Select(x => new RawMateria
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    RawMateriaName = x.Key
                });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);",
                    newRm);
                rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE MarkedDelete = 0;").ToDictionary(x => x.RawMateriaName);

                //计划号
                var productionLibraries = ServerConfig.ApiDb.Query<Production>("SELECT * FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.ProductionProcessName);
                var erpProductionLibraries = r.GroupBy(x => x.f_jhh);
                var newProductionLibraries = erpProductionLibraries.Where(x => !productionLibraries.ContainsKey(x.Key));
                var newPl = newProductionLibraries.Select(x => new Production
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    ProductionProcessName = x.Key
                });
                var newPlTmp = new List<Production>();
                newPlTmp.AddRange(newPl);
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);",
                    newPl);
                productionLibraries = ServerConfig.ApiDb.Query<Production>("SELECT * FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.ProductionProcessName);

                //数据库计划号工序
                var productionProcessStep = ServerConfig.ApiDb.Query<ProductionProcessStepDetail>("SELECT a.*, b.ProductionProcessName FROM `production_process_step` a JOIN `production_library` b ON a.ProductionProcessId = b.Id WHERE a.MarkedDelete = 0; ")
                    .GroupBy(x => x.ProductionProcessId).ToDictionary(x => x.Key);
                //获取的流程卡用到的计划号
                if (erpProductionLibraries.Any())
                {
                    var ff = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        { "type", "getProcessParameters" },
                        { "jhh", erpProductionLibraries.Select(x=>x.Key).Join(",")},
                    });
                    if (ff == "fail")
                    {
                        Log.ErrorFormat("InsertData 请求erp获取计划号工序数据失败,url:{0}", _url);
                        return false;
                    }
                    var rrr = HttpUtility.UrlDecode(ff);
                    if (rrr != "[][]" && rrr != "[]")
                    {
                        var erpJhhes = JsonConvert.DeserializeObject<ErpJhh[]>(rrr);
                        if (erpJhhes.Any())
                        {
                            foreach (var dr in erpJhhes)
                            {
                                dr.jhh = dr.jhh.Trim();
                                foreach (var ddr in dr.gx)
                                {
                                    ddr.n = ddr.n.Trim();
                                    ddr.v = ddr.v.Trim();
                                }
                            }
                            var deviceProcessSteps = ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT Id, StepName FROM `device_process_step` WHERE MarkedDelete = 0;");

                            //erp计划号工序
                            var erpProductionProcessStep = erpJhhes.ToDictionary(x => x.jhh);
                            //var newProductionProcessStep = erpProductionProcessStep.Where(x => !productionProcessStep.ContainsKey(x.Key));

                            //计划号新工序
                            var newPps = new List<ProductionProcessStep>();
                            foreach (var pl in newPlTmp)
                            {
                                if (erpProductionProcessStep.ContainsKey(pl.ProductionProcessName))
                                {
                                    var newProductionProcessStep = erpProductionProcessStep[pl.ProductionProcessName].gx;
                                    var i = 1;
                                    foreach (var processStep in newProductionProcessStep)
                                    {
                                        var n = ProcessStepName.ContainsKey(processStep.n)
                                            ? ProcessStepName[processStep.n]
                                            : processStep.n;

                                        var dps = deviceProcessSteps.FirstOrDefault(x => x.StepName == n);
                                        newPps.Add(new ProductionProcessStep
                                        {
                                            CreateUserId = _createUserId,
                                            MarkedDateTime = now,
                                            ProductionProcessId = productionLibraries[pl.ProductionProcessName].Id,
                                            ProcessStepOrder = i++,
                                            ProcessStepId = dps?.Id ?? 0,
                                            ProcessStepRequirements = processStep.v,
                                            ProcessStepRequirementMid = GetMid(processStep.v)
                                        });
                                    }
                                }
                            }
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                                newPps);

                            productionProcessStep = ServerConfig.ApiDb.Query<ProductionProcessStepDetail>("SELECT a.*, b.ProductionProcessName FROM `production_process_step` a JOIN `production_library` b ON a.ProductionProcessId = b.Id WHERE a.MarkedDelete = 0; ")
                              .GroupBy(x => x.ProductionProcessId).ToDictionary(x => x.Key);
                        }
                    }
                }

                //流程卡
                //var erpFlowCardLibraries = r.ToDictionary(x => $"{x.f_bz:d2}{x.f_lckh}");
                var erpFlowCardLibraries = r.ToDictionary(x => x.f_lckh);

                var fcIds = new List<int>();
                fcIds.AddRange(ServerConfig.ApiDb.Query<int>("SELECT Id FROM `flowcard_library` WHERE FlowCardName IN @FlowCardName;", new
                {
                    FlowCardName = erpFlowCardLibraries.Keys
                }));
                fcIds.AddRange(ServerConfig.ApiDb.Query<int>("SELECT Id FROM `flowcard_library` GROUP BY FlowCardName HAVING COUNT(1) > 1;"));
                if (fcIds.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `flowcard_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`IN @Id AND MarkedDelete = 0;", new
                        {
                            MarkedDateTime = DateTime.Now,
                            MarkedDelete = true,
                            Id = fcIds
                        });
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `flowcard_process_step` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`IN @Id;", new
                        {
                            MarkedDateTime = DateTime.Now,
                            MarkedDelete = true,
                            Id = fcIds
                        });
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `flowcard_specification` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `FlowCardId`IN @Id;", new
                        {
                            MarkedDateTime = DateTime.Now,
                            MarkedDelete = true,
                            Id = fcIds
                        });
                }

                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM `flowcard_library` WHERE CreateTime > ADDDATE(NOW(), -30) AND  `MarkedDelete` = 0;").ToDictionary(x => x.FlowCardName);
                //var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM `flowcard_library` WHERE `MarkedDelete` = 0;").ToDictionary(x => x.FlowCardName);
                var newFlowCardLibraries = erpFlowCardLibraries.Where(x => !flowCardLibraries.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                var newFc = newFlowCardLibraries.OrderBy(x => x.Value.f_id).Select(x => new FlowCard
                {
                    FId = x.Value.f_id,
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    FlowCardName = x.Key,
                    ProductionProcessId = productionLibraries[x.Value.f_jhh].Id,
                    RawMateriaId = rawMaterias[x.Value.f_mate].Id,
                    CreateTime = x.Value.f_inserttime,
                    FlowCardTypeId = x.Value.f_bz,
                    RawMaterialQuantity = int.Parse(x.Value.f_qty),
                    Sender = x.Value.f_fcygbh,
                    InboundNum = x.Value.f_rkxh,
                    Remarks = x.Value.f_note
                });
                var newFcTmp = new List<FlowCard>();
                newFcTmp.AddRange(newFc);
                FlowCardHelper.Instance.Add(newFc);

                //流程卡更新
                //todo

                flowCardLibraries = ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM `flowcard_library` WHERE CreateTime > ADDDATE(NOW(), -30) AND  `MarkedDelete` = 0;").ToDictionary(x => x.FlowCardName);

                newFc = flowCardLibraries.Values.Where(x => newFcTmp.Any(y => y.FlowCardName == x.FlowCardName));
                if (productionProcessStep.Any())
                {
                    //流程卡新工序
                    var newFps = new List<FlowCardProcessStep>();
                    foreach (var fc in newFc)
                    {
                        if (!productionProcessStep.ContainsKey(fc.ProductionProcessId))
                        {
                            continue;
                        }

                        var newFlowCardProcessStep = productionProcessStep[fc.ProductionProcessId];
                        foreach (var processStep in newFlowCardProcessStep)
                        {
                            newFps.Add(new FlowCardProcessStep
                            {
                                CreateUserId = _createUserId,
                                MarkedDateTime = now,
                                FlowCardId = fc.Id,
                                ProcessStepOrder = processStep.ProcessStepOrder,
                                ProcessStepId = processStep.ProcessStepId,
                                ProcessStepRequirements = processStep.ProcessStepRequirements,
                                ProcessStepRequirementMid = processStep.ProcessStepRequirementMid,
                            });
                        }
                    }

                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                        newFps.OrderBy(x => x.FlowCardId).ThenBy(x => x.ProcessStepOrder));
                }
            }
            catch (Exception e)
            {
                Log.ErrorFormat("InsertFlowCard erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                return false;
            }
            return true;

        }

        /// <summary>
        /// 更新流程卡信息
        /// </summary>
        private static void Update()
        {
            if (_isUpdate)
            {
                return;
            }

            _isUpdate = true;
            var sId =
                ServerConfig.ApiDb.Query<int>("SELECT FId FROM `flowcard_library` WHERE FId != 0 AND DATE(CreateTime) = @Time ORDER BY CreateTime LIMIT 1;", new
                {
                    Time = DateTime.Today.AddDays(-1)
                }, 120).FirstOrDefault();

            if (sId != 0)
            {
                UpdateFlowCard(sId - 1);
            }
            _isUpdate = false;
        }

        /// <summary>
        /// 更新流程卡信息
        /// </summary>
        private static void UpdateFlowCard(int id)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getHairpin" },
                { "id", id.ToString()},
            });
            if (f == "fail")
            {
                Log.ErrorFormat("UpdateFlowCard 请求erp获取流程卡数据失败,url:{0}", _url);
                return;
            }

            try
            {
                var rr = HttpUtility.UrlDecode(f);
                var res = JsonConvert.DeserializeObject<ErpRes>(rr);
                if (res.result != "ok")
                {
                    Log.ErrorFormat("UpdateData 请求erp获取流程卡数据返回错误,原因:{0}", res.result);
                    return;
                }

                var r = res.data;
                if (r.Count <= 0)
                {
                    return;
                }
                foreach (var dr in r)
                {
                    dr.f_jhh = dr.f_jhh.Trim();
                    dr.f_lckh = dr.f_lckh.Trim();
                    dr.f_mate = dr.f_mate.Trim();
                }
                //流程卡
                //var erpFlowCardLibraries = r.ToDictionary(x => $"{x.f_bz:d2}{x.f_lckh}");
                var erpFlowCardLibraries = r.ToDictionary(x => x.f_lckh);

                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCard>("SELECT * FROM `flowcard_library` WHERE `MarkedDelete` = 0 AND FID > @fid;", new
                {
                    fid = id
                });

                var update = new List<FlowCard>();
                foreach (var flowCardLibrary in flowCardLibraries)
                {
                    if (erpFlowCardLibraries.ContainsKey(flowCardLibrary.FlowCardName))
                    {
                        var erpFlowCardLibrary = erpFlowCardLibraries[flowCardLibrary.FlowCardName];
                        var bUpdate = false;
                        if (flowCardLibrary.Sender.IsNullOrEmpty() && flowCardLibrary.Sender != erpFlowCardLibrary.f_fcygbh)
                        {
                            bUpdate = true;
                            flowCardLibrary.Sender = erpFlowCardLibrary.f_fcygbh;
                        }

                        if (flowCardLibrary.RawMaterialQuantity == 0 && flowCardLibrary.RawMaterialQuantity != int.Parse(erpFlowCardLibrary.f_qty))
                        {
                            bUpdate = true;
                            flowCardLibrary.RawMaterialQuantity = int.Parse(erpFlowCardLibrary.f_qty);
                        }

                        if (flowCardLibrary.InboundNum.IsNullOrEmpty() && flowCardLibrary.InboundNum != erpFlowCardLibrary.f_rkxh)
                        {
                            bUpdate = true;
                            flowCardLibrary.InboundNum = erpFlowCardLibrary.f_rkxh;
                        }

                        if (flowCardLibrary.Remarks.IsNullOrEmpty() && flowCardLibrary.Remarks != erpFlowCardLibrary.f_note)
                        {
                            bUpdate = true;
                            flowCardLibrary.Remarks = erpFlowCardLibrary.f_note;
                        }

                        if (bUpdate)
                        {
                            update.Add(flowCardLibrary);
                        }
                    }
                    else
                    {
                        flowCardLibrary.MarkedDelete = true;
                        update.Add(flowCardLibrary);
                    }
                }
                ServerConfig.ApiDb.Execute(
                    "UPDATE flowcard_library SET `MarkedDelete` = @MarkedDelete, `RawMaterialQuantity` = @RawMaterialQuantity, `Sender` = @Sender, " +
                    "`InboundNum` = @InboundNum, `Remarks` = @Remarks WHERE `Id` = @Id;", update);

            }
            catch (Exception e)
            {
                Log.ErrorFormat("UpdateFlowCard erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                return;
            }
        }

        /// <summary>
        /// 更新流程卡工序
        /// </summary>
        private static void UpdateFlowCardProcessStep()
        {
            if (_isUpdateFlowCardProcessStep)
            {
                return;
            }

            _isUpdateFlowCardProcessStep = true;
            try
            {
                //计划号
                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCard>("SELECT a.Id, a.FlowCardName, a.ProductionProcessId FROM `flowcard_library` a LEFT JOIN flowcard_process_step b ON a.Id = b.FlowCardId WHERE a.CreateTime > ADDDATE(NOW(), -30) AND a.MarkedDelete = 0 AND ISNULL(b.Id);", 60).ToDictionary(x => x.Id);
                if (flowCardLibraries.Any())
                {
                    //数据库计划号工序
                    var productionProcessStep = ServerConfig.ApiDb.Query<ProductionProcessStepDetail>("SELECT a.*, b.ProductionProcessName FROM `production_process_step` a JOIN `production_library` b ON a.ProductionProcessId = b.Id WHERE a.MarkedDelete = 0; ")
                        .GroupBy(x => x.ProductionProcessId).ToDictionary(x => x.Key);
                    var now = DateTime.Now;
                    if (productionProcessStep.Any())
                    {
                        //流程卡新工序
                        var newFps = new List<FlowCardProcessStep>();
                        foreach (var fc in flowCardLibraries)
                        {
                            var productionProcessId = fc.Value.ProductionProcessId;
                            if (!productionProcessStep.ContainsKey(productionProcessId))
                            {
                                continue;
                            }

                            var newFlowCardProcessStep = productionProcessStep[productionProcessId];
                            foreach (var processStep in newFlowCardProcessStep)
                            {
                                newFps.Add(new FlowCardProcessStep
                                {
                                    CreateUserId = _createUserId,
                                    MarkedDateTime = now,
                                    FlowCardId = fc.Key,
                                    ProcessStepOrder = processStep.ProcessStepOrder,
                                    ProcessStepId = processStep.ProcessStepId,
                                    ProcessStepRequirements = processStep.ProcessStepRequirements,
                                    ProcessStepRequirementMid = processStep.ProcessStepRequirementMid,
                                });
                            }
                        }

                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO flowcard_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                            newFps.OrderBy(x => x.FlowCardId).ThenBy(x => x.ProcessStepOrder));
                    }

                }
            }
            catch (Exception e)
            {
                Log.ErrorFormat("UpdateFlowCardProcessStep erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
            }
            _isUpdateFlowCardProcessStep = false;
        }

        /// <summary>
        /// 更新计划号工序
        /// </summary>
        private static void UpdateProductionProcessStep()
        {
            if (_isUpdateProductionProcessStep)
            {
                return;
            }

            _isUpdateProductionProcessStep = true;
            //计划号
            var productionLibraries = ServerConfig.ApiDb.Query<Production>("SELECT a.Id, a.ProductionProcessName FROM `production_library` a LEFT JOIN production_process_step b ON a.Id = b.ProductionProcessId WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY a.Id;").ToDictionary(x => x.ProductionProcessName);
            if (productionLibraries.Any())
            {
                var f = HttpServer.Get(_url, new Dictionary<string, string>
                {
                    { "type", "getProcessParameters" },
                    { "jhh", productionLibraries.Select(x=>x.Key).Join(",")},
                });
                if (f != "fail")
                {
                    try
                    {
                        var rrr = HttpUtility.UrlDecode(f);
                        if (rrr != "[][]" && rrr != "[]")
                        {
                            var erpJhhes = JsonConvert.DeserializeObject<ErpJhh[]>(rrr);
                            if (erpJhhes.Any())
                            {
                                foreach (var dr in erpJhhes)
                                {
                                    dr.jhh = dr.jhh.Trim();
                                    foreach (var ddr in dr.gx)
                                    {
                                        ddr.n = ddr.n.Trim();
                                        ddr.v = ddr.v.Trim();
                                    }
                                }

                                //erp计划号工序
                                var erpProductionProcessStep = erpJhhes.ToDictionary(x => x.jhh);
                                //var newProductionProcessStep = erpProductionProcessStep.Where(x => !productionProcessStep.ContainsKey(x.Key));
                                var deviceProcessSteps =
                                    ServerConfig.ApiDb.Query<DeviceProcessStep>(
                                        "SELECT Id, StepName FROM `device_process_step` WHERE MarkedDelete = 0;");
                                var now = DateTime.Now;
                                //计划号新工序
                                var newPps = new List<ProductionProcessStep>();
                                foreach (var pl in productionLibraries)
                                {
                                    if (erpProductionProcessStep.ContainsKey(pl.Key))
                                    {
                                        var newProductionProcessStep = erpProductionProcessStep[pl.Key].gx;
                                        var i = 1;
                                        foreach (var processStep in newProductionProcessStep)
                                        {
                                            var n = ProcessStepName.ContainsKey(processStep.n)
                                                ? ProcessStepName[processStep.n]
                                                : processStep.n;

                                            var dps = deviceProcessSteps.FirstOrDefault(x => x.StepName == n);
                                            newPps.Add(new ProductionProcessStep
                                            {
                                                CreateUserId = _createUserId,
                                                MarkedDateTime = now,
                                                ProductionProcessId = productionLibraries[pl.Key].Id,
                                                ProcessStepOrder = i++,
                                                ProcessStepId = dps?.Id ?? 0,
                                                ProcessStepRequirements = processStep.v,
                                                ProcessStepRequirementMid = GetMid(processStep.v)
                                            });
                                        }
                                    }
                                }

                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO production_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `ProcessStepOrder`, `ProcessStepId`, `ProcessStepRequirements`, `ProcessStepRequirementMid`) " +
                                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @ProcessStepOrder, @ProcessStepId, @ProcessStepRequirements, @ProcessStepRequirementMid);",
                                    newPps);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("UpdateProductionProcessStep erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                    }
                }
                else
                {
                    Log.ErrorFormat("UpdateProductionProcessStep 请求erp获取流程卡数据失败,url:{0}", _url);
                }
            }

            _isUpdateProductionProcessStep = false;
        }

        /// <summary>
        /// 更新计划号工艺
        /// </summary>
        private static void UpdateProductionProcess()
        {
            if (_isUpdateProductionProcess)
            {
                return;
            }

            var key = "isUpdateProductionProcess";
            if (!RedisHelper.Exists(key))
            {
                RedisHelper.SetForever(key, 0);
            }
            var isUpdateProductionProcessFlag = RedisHelper.Get<int>(key) == 1;
            if (isUpdateProductionProcessFlag)
            {
                var now = DateTime.Now;
                _isUpdateProductionProcess = true;
                //计划号
                var productionLibraries = ServerConfig.ApiDb.Query<Production>("SELECT a.Id, ProductionProcessName FROM `production_library` a LEFT JOIN `process_management` b ON a.Id = b.ProductModels WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) AND ProductionProcessName != '';");
                if (productionLibraries.Any())
                {
                    try
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO process_management (`CreateUserId`, `MarkedDateTime`, `ProcessNumber`, `ProductModels`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @ProcessNumber, @ProductModels);",
                            productionLibraries.Where(y => !y.ProductionProcessName.IsNullOrEmpty()).Select(x => new
                            {
                                CreateUserId = "admin",
                                MarkedDateTime = now,
                                ProcessNumber = x.ProductionProcessName + "粗抛",
                                ProductModels = x.Id,
                            }));
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("UpdateProductionProcess 更新计划号工艺失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                    }
                }
                var modelIds = ServerConfig.ApiDb
                    .Query<string>("SELECT GROUP_CONCAT(Id) FROM `device_model` WHERE MarkedDelete = 0")
                    .FirstOrDefault();
                var deviceIds = ServerConfig.ApiDb
                    .Query<string>("SELECT GROUP_CONCAT(Id) FROM `device_library` WHERE MarkedDelete = 0")
                    .FirstOrDefault();
                ServerConfig.ApiDb.Execute(
                    "UPDATE `process_management` SET DeviceModels = @modelIds, DeviceIds = @deviceIds WHERE CreateUserId = @CreateUserId AND ISNULL(DeviceIds)",
                    new
                    {
                        modelIds,
                        deviceIds,
                        CreateUserId = "admin"
                    });

                _isUpdateProductionProcess = false;
            }
        }

        /// <summary>
        /// 更新计划号规格
        /// </summary>
        private static void UpdateProductionSpecification()
        {
            if (_isUpdateProductionSpecification)
            {
                return;
            }

            _isUpdateProductionSpecification = true;
            //计划号
            var productionLibraries = ServerConfig.ApiDb.Query<Production>("SELECT a.Id, a.ProductionProcessName FROM `production_library` a LEFT JOIN production_specification b ON a.Id = b.ProductionProcessId WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY a.Id;").ToDictionary(x => x.ProductionProcessName);
            if (productionLibraries.Any())
            {
                var f = HttpServer.Get(_url, new Dictionary<string, string>
                {
                    { "type", "getProcessParameters" },
                    { "jhh", productionLibraries.Select(x=>x.Key).Join(",")},
                });
                if (f != "fail")
                {
                    try
                    {
                        var rrr = HttpUtility.UrlDecode(f);
                        if (rrr != "[][]" && rrr != "[]")
                        {
                            var erpJhhes = JsonConvert.DeserializeObject<ErpJhh[]>(rrr);
                            if (erpJhhes.Any())
                            {
                                foreach (var dr in erpJhhes)
                                {
                                    dr.jhh = dr.jhh.Trim();
                                    foreach (var ddr in dr.gx)
                                    {
                                        ddr.n = ddr.n.Trim();
                                        ddr.v = ddr.v.Trim();
                                    }
                                }

                                //erp计划号规格 客户要求
                                var erpProductionProcessStep = erpJhhes.ToDictionary(x => x.jhh);
                                var now = DateTime.Now;
                                //计划号客户要求
                                var newPss = new List<ProductionSpecification>();
                                foreach (var pl in productionLibraries)
                                {
                                    if (!erpProductionProcessStep.ContainsKey(pl.Key))
                                    {
                                        continue;
                                    }

                                    var newProductionSpecification =
                                        erpProductionProcessStep[pl.Key].khyq.OrderBy(x => x.id);
                                    newPss.AddRange(newProductionSpecification.Select(processStep =>
                                        new ProductionSpecification
                                        {
                                            CreateUserId = _createUserId,
                                            MarkedDateTime = now,
                                            ProductionProcessId = productionLibraries[pl.Key].Id,
                                            SpecificationName = processStep.n,
                                            SpecificationValue =
                                                new List<string> { processStep.v.Trim(), processStep.note.Trim() }
                                                    .Where(x => !x.IsNullOrEmpty()).Join(",")
                                        }));
                                }

                                ServerConfig.ApiDb.Execute(
                                    "INSERT INTO production_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessId`, `SpecificationName`, `SpecificationValue`) " +
                                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessId, @SpecificationName, @SpecificationValue);",
                                    newPss);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("UpdateProductionSpecification erp数据解析失败,原因:{0},错误:{1}", e.Message,
                            e.StackTrace);
                    }
                }
                else
                {
                    Log.ErrorFormat("UpdateProductionSpecification 请求erp获取流程卡数据失败,url:{0}", _url);
                }
            }

            _isUpdateProductionSpecification = false;
        }

        /// <summary>
        /// 取平均值
        /// </summary>
        public static decimal GetMid(string value)
        {
            if (value == "")
            {
                return 0;
            }

            value += ";";
            var t = 0;
            if (value.Contains("±"))
            {
                //0.255±0.005mm
                t = 1;
            }
            else if (value.Contains("+") && value.Contains("/") && value.Contains("-"))
            {
                //0.21+0.013/-0.027mm
                t = 3;
            }
            else if (value.Contains("~") || value.Contains("~") || value.Contains("-"))
            {
                //0.202～0.216mm
                t = 2;
            }

            var num = new List<decimal>();
            var s = value.Replace(" ", "").Select(x => x.ToString()).ToArray();
            var n = "";
            foreach (var sf in s)
            {
                if (t == 3)
                {
                    if (int.TryParse(sf, out _) || sf == "." || sf == "-")
                    {
                        n += sf;
                    }
                    else
                    {
                        if (decimal.TryParse(n, out var k))
                        {
                            num.Add(k);
                        }

                        n = "";
                    }
                }
                else
                {
                    if (int.TryParse(sf, out _) || sf == ".")
                    {
                        n += sf;
                    }
                    else
                    {
                        if (decimal.TryParse(n, out var k))
                        {
                            num.Add(k);
                        }

                        n = "";
                    }
                }
            }

            decimal p = 0;
            switch (t)
            {
                case 1:
                    if (num.Count >= 1)
                    {
                        p = num[0];
                    }
                    break;
                case 2:
                    if (num.Count == 1)
                    {
                        p = num[0];
                    }
                    else if (num.Count >= 2)
                    {
                        p = (num[0] + num[1]) / 2;
                    }
                    break;
                case 3:
                    if (num.Count == 1)
                    {
                        p = num[0];
                    }
                    else if (num.Count >= 3)
                    {
                        p = num[0] + (num[1] + num[2]) / 2;
                    }
                    break;
                default:
                    if (num.Count >= 1)
                    {
                        p = num[0];
                    }
                    else
                    {
                        p = 0;
                    }
                    break;
            }

            return p.ToRound(4);
        }

        /// <summary>
        /// 更新生产工序
        /// </summary>
        private static void UpdateProcessStep()
        {
            if (_isUpdateProcessStep)
            {
                return;
            }

            _isUpdateProcessStep = true;

            //旧工序
            var oldSteps = DeviceProcessStepHelper.GetDetails();
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getGxlb" },
            });
            if (f != "fail")
            {
                try
                {
                    var rrr = HttpUtility.UrlDecode(f);
                    if (rrr != "[][]" && rrr != "[]")
                    {
                        var add = new List<DeviceProcessStepDetail>();
                        var update = new List<DeviceProcessStepDetail>();
                        var now = DateTime.Now;
                        var erpSteps = JsonConvert.DeserializeObject<ErpStep[]>(rrr);
                        if (erpSteps.Any())
                        {
                            foreach (var step in erpSteps)
                            {
                                var newStep = new DeviceProcessStepDetail(step, _createUserId, now);
                                var oldStep = oldSteps.FirstOrDefault(x => x.Abbrev == step.gxid);
                                if (oldStep != null)
                                {
                                    if (oldStep.HaveChange(newStep))
                                    {
                                        newStep.Id = oldStep.Id;
                                        update.Add(newStep);
                                    }
                                }
                                else
                                {
                                    add.Add(newStep);
                                }
                            }

                            if (add.Any())
                            {
                                DeviceProcessStepHelper.Instance.Add(add);
                            }

                            if (update.Any())
                            {
                                DeviceProcessStepHelper.Instance.Update<DeviceProcessStepDetail>(update);
                                DeviceProcessStepHelper.Update(update);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("UpdateProcessStep erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                }

            }
            else
            {
                Log.ErrorFormat("UpdateProcessStep 请求erp获取生产工序数据失败,url:{0}", _url);
            }
            _isUpdateProcessStep = false;
        }

        /// <summary>
        /// 主动获取报表
        /// </summary>
        private static void GetFlowCardReport()
        {
            if (_isGetFlowCardReport)
            {
                return;
            }

            _isGetFlowCardReport = true;
            try
            {
                //工序
                var updateStep = new List<DeviceProcessStepDetail>();
                var steps = DeviceProcessStepHelper.GetDetailsFrom(DataFrom.Erp);
                var tFromIds = FlowCardReportGetHelper.GetStepFromId();
                if (steps.Any())
                {
                    var now = DateTime.Now;
                    var specials = new List<string> { "fp", "fx", "-" };
                    var add = new List<FlowCardReportGet>();
                    var update = new List<FlowCardReportGet>();
                    foreach (var step in steps)
                    {
                        if (step.Abbrev.IsNullOrEmpty())
                        {
                            continue;
                        }

                        var from = tFromIds.FirstOrDefault(x => x.Id == step.Id);
                        var change = false;
                        var f = HttpServer.Get(_url, new Dictionary<string, string>
                        {
                            { "type", "getGwData" },
                            { "gxid", step.Abbrev },
                            { "id", ((from?.FromId??step.FromId)+1).ToString() },
                        });
                        if (f != "fail")
                        {
                            try
                            {
                                var rrr = HttpUtility.UrlDecode(f);
                                if (rrr != "[][]" && rrr != "[]")
                                {
                                    var erpReports = JsonConvert.DeserializeObject<ErpFlowCardReportGet[]>(rrr);
                                    if (erpReports.Any())
                                    {
                                        step.FromId = erpReports.Max(x => x.f_id);
                                        change = true;

                                        foreach (var report in erpReports)
                                        {
                                            var fc = new FlowCardReportGet(report, step, now);
                                            var b = report.bl.ToString();
                                            if (b != "[]")
                                            {
                                                var bc = new List<BadTypeCount>();
                                                var bllb = JObject.Parse(b);
                                                foreach (var bl in bllb)
                                                {
                                                    var name = bl.Key;
                                                    var bad = step.ErrorList.FirstOrDefault(x => x.name == name);
                                                    if (bad != null)
                                                    {
                                                        var v = bl.Value.ToString();
                                                        if (int.TryParse(v, out var count))
                                                        {
                                                        }
                                                        bc.Add(new BadTypeCount(bad, count));
                                                    }
                                                }

                                                fc.LiePian = bc.Sum(x => x.count);
                                                if (fc.LiePian > 0)
                                                {
                                                    bc = bc.OrderByDescending(x => x.count).ToList();
                                                }

                                                fc.Reason = bc.ToJSON();
                                            }
                                            add.Add(fc);
                                        }
                                    }
                                }

                                if (step.Api == 1)
                                {
                                    step.Api = 0;
                                    change = true;
                                }
                            }
                            catch (Exception e)
                            {
                                step.Api = 1;
                                change = true;
                                Log.ErrorFormat("GetFlowCardReport erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                            }

                            if (change)
                            {
                                updateStep.Add(step);
                            }
                        }
                        else
                        {
                            Log.Error($"GetFlowCardReport 请求erp获取报表数据失败,step:{step.StepName},Id:{step.Id},url:{_url}");
                        }

                    }


                    if (updateStep.Any())
                    {
                        DeviceProcessStepHelper.UpdateFromId(updateStep);
                    }

                    if (add.Any())
                    {
                        var devices = DeviceLibraryHelper.GetDetails(1, add.Where(x => !x.Code.IsNullOrEmpty()).Select(x => x.Code).Distinct()).ToDictionary(x => x.Code);
                        var reportFlowCards = add.Where(x => !x.FlowCard.IsNullOrEmpty()).Select(x => x.FlowCard).Concat(add.Where(x => !x.OldFlowCard.IsNullOrEmpty()).Select(x => x.OldFlowCard)).Distinct();
                        var flowCards = FlowCardHelper.GetFlowCards(reportFlowCards).ToDictionary(x => x.FlowCardName);
                        var processors = AccountInfoHelper.GetAccountInfoByNames(add.Where(x => !x.Processor.IsNullOrEmpty()).Select(x => x.Processor).Distinct()).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
                        var productions = flowCards.Any()
                            ? ProductionHelper.Instance.GetByIds<Production>(flowCards.Values.Select(x => x.ProductionProcessId).Distinct()).ToDictionary(x => x.Id)
                            : new Dictionary<int, Production>();
                        foreach (var fc in add)
                        {
                            var flowCard = flowCards.ContainsKey(fc.FlowCard) ? flowCards[fc.FlowCard] : null;
                            var flowCardId = flowCard?.Id ?? 0;
                            fc.FlowCardId = flowCardId;
                            if (!specials.Any(x => fc.FlowCard.Contains(x)) && fc.FlowCardId == 0)
                            {
                                var dfc = FlowCardHelper.GetFlowCardAll(fc.FlowCard);
                                fc.FlowCardId = dfc?.Id ?? 0;
                            }
                            var oldFlowCard = flowCards.ContainsKey(fc.OldFlowCard) ? flowCards[fc.OldFlowCard] : null;
                            var oldFlowCardId = oldFlowCard?.Id ?? 0;
                            fc.OldFlowCardId = oldFlowCardId;
                            var productionProcessId = oldFlowCard?.ProductionProcessId ?? 0;
                            if (!specials.Any(x => fc.OldFlowCard.Contains(x)) && fc.OldFlowCardId == 0)
                            {
                                var dfc = FlowCardHelper.GetFlowCardAll(fc.OldFlowCard);
                                fc.OldFlowCardId = dfc?.Id ?? 0;
                                productionProcessId = dfc?.ProductionProcessId ?? 0;
                            }

                            var production = productions.ContainsKey(productionProcessId) ? productions[productionProcessId] : null;
                            var productionId = production?.Id ?? 0;
                            fc.ProductionId = productionId;
                            fc.Production = production?.ProductionProcessName ?? "";

                            var deviceId = devices.ContainsKey(fc.Code) ? devices[fc.Code].Id : 0;
                            fc.DeviceId = deviceId;
                            var processor = processors.ContainsKey(fc.Processor) ? processors[fc.Processor] : null;
                            var processorId = processor?.Id ?? 0;
                            fc.ProcessorId = processorId;
                            fc.State = (fc.FlowCardId == 0 || fc.OldFlowCardId == 0) ? 2 : (fc.ProcessorId == 0 && !fc.Processor.IsNullOrEmpty() ? 3 : (fc.DeviceId == 0 && !fc.Code.IsNullOrEmpty() ? 4 : 1));
                            if ((!specials.Any(x => fc.FlowCard.Contains(x)) || !specials.Any(x => fc.FlowCard.Contains(x)))
                                && fc.State == 2 && (now - fc.Time).TotalMinutes < 10)
                            {
                                fc.State = 0;
                            }
                        }
                        FlowCardReportGetHelper.Instance.Add(add.OrderBy(x => x.Time));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            _isGetFlowCardReport = false;
        }


        /// <summary>
        /// 主动获取计划
        /// </summary>
        private static void GetProductionPlan()
        {
            if (_isGetProductionPlan)
            {
                return;
            }

            _isGetProductionPlan = true;

            try
            {
                var productions = ProductionHelper.Instance.GetAll<Production>();
                var steps = DeviceProcessStepHelper.GetDetailsFrom(DataFrom.Erp);
                if (steps.Any() && productions.Any())
                {
                    //最大时间
                    var date1 = ProductionPlanHelper.GetMaxDate();
                    var now = DateTime.Now;
                    var today = now.Date;
                    if (date1 == default(DateTime))
                    {
                        date1 = today;
                    }

                    var date2 = today;
                    var plans = new List<ProductionPlan>();
                    plans.AddRange(ProductionPlanHelper.GetDetails(date1, date2));
                    var update = new List<ProductionPlan>();
                    var add = new List<ProductionPlan>();
                    var del = new List<ProductionPlan>();
                    #region 获取计划
                    var f = HttpServer.Get(_url, new Dictionary<string, string>
                    {
                        { "type", "getProductionPlan" },
                        { "date1", date1.ToDateStr() },
                        { "date2", date2.ToDateStr() },
                    });
                    if (f != "fail")
                    {
                        var rrr = HttpUtility.UrlDecode(f);
                        if (rrr != "[][]" && rrr != "[]")
                        {
                            var result = JsonConvert.DeserializeObject<ErpProductionPlanResult>(rrr);
                            if (result.jhdata.Any())
                            {
                                var jhdata = result.jhdata.Select(jhPlan =>
                                {
                                    var production =
                                        productions.FirstOrDefault(x => x.ProductionProcessName == jhPlan.f_jhh);
                                    var step =
                                        steps.FirstOrDefault(x => x.StepName == jhPlan.f_gxname);
                                    return new ProductionPlan(jhPlan, production, step, _createUserId, now);
                                });

                                foreach (var pp in jhdata)
                                {
                                    var oldPlan = plans.FirstOrDefault(x =>
                                        x.Date == pp.Date && x.ProductionProcessName == pp.ProductionProcessName &&
                                        x.StepName == pp.StepName);
                                    if (oldPlan == null)
                                    {
                                        add.Add(pp);
                                    }
                                    else if (oldPlan.HaveChange(pp))
                                    {
                                        pp.Id = oldPlan.Id;
                                        update.Add(pp);
                                    }
                                }

                                del.AddRange(plans.Where(x => jhdata.All(pp =>
                                    !(x.Date == pp.Date && x.ProductionId == pp.ProductionId &&
                                      x.StepId == pp.StepId))));
                            }
                        }

                        #endregion

                        if (update.Any())
                        {
                            ProductionPlanHelper.Instance.Update<ProductionPlan>(update);
                        }

                        if (add.Any())
                        {
                            ProductionPlanHelper.Instance.Add(add);
                        }

                        if (del.Any())
                        {
                            ProductionPlanHelper.Instance.Delete(del.Select(x => x.Id));
                        }
                    }
                    else
                    {
                        Log.Error($"GetProductionPlan 请求erp获取数据失败,date1:{date1}, date2:{date2}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorFormat("GetFlowCardReport erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
            }
            _isGetProductionPlan = false;
        }

        public class ErpFlowCard
        {
            public int f_id;
            /// <summary>
            /// 流程卡号
            /// </summary>
            public string f_lckh;
            /// <summary>
            /// 计划号
            /// </summary>
            public string f_jhh;
            /// <summary>
            /// 原料
            /// </summary>
            public string f_mate;
            /// <summary>
            /// 发出厚度
            /// </summary>
            public string f_fchd;
            /// <summary>
            /// 发出数
            /// </summary>
            public string f_qty;
            /// <summary>
            /// 发出人
            /// </summary>
            public string f_fcygbh;
            /// <summary>
            /// 入库序号
            /// </summary>
            public string f_rkxh;
            /// <summary>
            /// 备注
            /// </summary>
            public string f_note;
            /// <summary>
            /// 
            /// </summary>
            public DateTime f_inserttime;
            /// <summary>
            /// 
            /// </summary>
            public int f_bz;
        }

        public class ErpRelation
        {
            public int id;
            public string abbre;
            public string name;
        }

        public class ErpRes
        {
            public string result;
            public List<ErpFlowCard> data;
            public ErpRelation[] relation;
        }


        /// <summary>
        /// 计划号工序
        /// </summary>
        public class ErpGx
        {
            public string n;
            public string v;
        }
        /// <summary>
        /// 计划号客户要求
        /// </summary>
        public class ErpYq
        {
            public int id;
            public string n;
            public string v;
            public string note;
        }
        public class ErpJhh
        {
            public string jhh;
            public ErpGx[] gx;
            public ErpYq[] khyq;
        }
        /// <summary>
        /// 生产工序
        /// </summary>
        public class ErpStep
        {
            /// <summary>
            /// 工序名称
            /// </summary>
            public string gxmc;
            /// <summary>
            /// 工序id
            /// </summary>
            public string gxid;
            /// <summary>
            /// F=发出，J=检验，G=加工
            /// </summary>
            public string gxtype;
            /// <summary>
            /// 是否有合格数字段
            /// </summary>
            public bool hg;
            /// <summary>
            /// 不良类型
            /// </summary>
            public BadType[] bllx;
        }

        /// <summary>
        /// 生产计划返回
        /// </summary>
        public class ErpProductionPlanResult
        {
            public string error;
            public ErpProductionPlan[] jhdata;
        }

        /// <summary>
        /// 生产计划
        /// </summary>
        public class ErpProductionPlan
        {
            /// <summary>
            /// 工序名称
            /// </summary>
            public string f_gxname;
            /// <summary>
            /// 计划号
            /// </summary>
            public string f_jhh;
            /// <summary>
            /// 计划时间
            /// </summary>
            public DateTime f_jhdate;
            /// <summary>
            /// 加工数
            /// </summary>
            public decimal f_yqty;
            /// <summary>
            /// 修改数
            /// </summary>
            public decimal? f_xqty;
            /// <summary>
            /// 修改原因
            /// </summary>
            public string f_xgyy;
            /// <summary>
            /// 备注
            /// </summary>
            public string f_note;
            /// <summary>
            /// 修改数
            /// </summary>
            public decimal f_qty;
        }

        public static void UseFlowCardReport()
        {
            try
            {
                var fcs = ServerConfig.ApiDb.Query<FlowCardReport>(
                    "SELECT * FROM `flowcard_report` WHERE State = 0 AND Time > @Time ORDER BY Time;", new { Time = DateTime.Today.AddDays(-5) });

                if (fcs.Any())
                {
                    var devices = DeviceLibraryHelper.GetDetails(0, fcs.Select(x => x.Code).Distinct()).ToDictionary(x => x.Code);
                    var flowCards = FlowCardHelper.GetFlowCards(fcs.Select(x => x.FlowCard).Distinct()).ToDictionary(x => x.FlowCardName);
                    var processors = AccountInfoHelper.GetAccountInfoByNames(fcs.Select(x => x.Processor).Distinct()).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
                    var productions = flowCards.Any()
                        ? ProductionHelper.Instance.GetByIds<Production>(flowCards.Values.Select(x => x.ProductionProcessId).Distinct()).ToDictionary(x => x.Id)
                        : new Dictionary<int, Production>();

                    var gxFcs = new Dictionary<int, List<ErpUpdateFlowCard>>();
                    var updateAcs = new Dictionary<int, AccountInfo>();
                    var fr = new List<FlowCardReport>();
                    var updateLog = new List<MonitoringProcessLog>();

                    var gxs = fcs.GroupBy(x => x.Step).Select(x => x.Key);
                    foreach (var gx in gxs)
                    {
                        if (!gxFcs.ContainsKey(gx))
                        {
                            gxFcs.Add(gx, new List<ErpUpdateFlowCard>());
                        }
                        //研磨1 粗抛 2  精抛 3
                        if (AnalysisHelper.ParamIntDic.ContainsKey(gx))
                        {
                            var fcc = fcs.Where(x => x.Step == gx);
                            var flowCardInfos = fcc.Select(fc => new ErpUpdateFlowCard
                            {
                                Id = fc.FlowCardId,
                                MarkedDateTime = fc.Time,
                                FaChu = fc.Total,
                                HeGe = fc.HeGe,
                                LiePian = fc.LiePian,
                                DeviceId = fc.DeviceId,
                                Time = fc.Time,
                                JiaGongRen = fc.Processor
                            });
                            gxFcs[gx].AddRange(flowCardInfos);
                            fr.AddRange(fcc.Select(fc =>
                            {
                                var deviceId = devices.ContainsKey(fc.Code) ? devices[fc.Code].Id : 0;
                                var flowCard = flowCards.ContainsKey(fc.FlowCard) ? flowCards[fc.FlowCard] : null;
                                var flowCardId = flowCard?.Id ?? 0;
                                var productionProcessId = flowCard?.ProductionProcessId ?? 0;
                                var production = productions.ContainsKey(productionProcessId) ? productions[productionProcessId] : null;
                                var productionId = production?.Id ?? 0;
                                var processor = processors.ContainsKey(fc.Processor) ? processors[fc.Processor] : null;
                                var processorId = processor?.Id ?? 0;
                                fc.FlowCardId = flowCardId;
                                fc.ProductionId = productionId;
                                fc.Production = production?.ProductionProcessName ?? "";
                                fc.DeviceId = deviceId;
                                fc.ProcessorId = processorId;
                                fc.State = fc.FlowCardId == 0 ? 2 : (fc.ProcessorId == 0 ? 3 : (fc.DeviceId == 0 ? 4 : 1));
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
                                    if (change && !updateAcs.ContainsKey(processorId))
                                    {
                                        updateAcs[processorId] = processor;
                                    }
                                }
                                return fc;
                            }));

                            //foreach (var fc in fcc)
                            //{
                            //    if (fc.Back)
                            //    {
                            //        var processId = ServerConfig.ApiDb.Query<int>(
                            //            "SELECT Id FROM `npc_monitoring_process_log` " +
                            //            "WHERE DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime) AND FlowCardId != 0 ORDER BY StartTime DESC LIMIT 1;",
                            //            new
                            //            {
                            //                DeviceId = fc.DeviceId,
                            //                ProcessType = ProcessType.Process
                            //            }).FirstOrDefault();
                            //        if (processId != 0)
                            //        {
                            //            fc.Id1 = processId;
                            //            fc.ProcessType = ProcessType.Process;
                            //            ServerConfig.ApiDb.Execute(
                            //                "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId, `Processor` = @Processor " +
                            //                "WHERE `Id` > @Id1 AND StartTime < @Time AND DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime);",
                            //                fc);
                            //            Log.Debug($"UPDATE 流程卡:{fc.FlowCard}, 流程卡Id:{fc.FlowCardId}, 机台号:{fc.Code}, " +
                            //                      $"设备Id:{fc.DeviceId}, 工序:{fc.Step}, 加工人:{fc.Processor}, Id:{fc.Id}");
                            //        }
                            //    }
                            //}
                        }
                    }

                    if (fr.Any())
                    {
                        FlowCardReportHelper.Update(fr);
                        //ServerConfig.ApiDb.Execute($"UPDATE `flowcard_report` SET `State` = @State WHERE `Id` = @Id;", fr);
                    }
                    //if (gxFcs.Any())
                    //{
                    //    foreach (var (gx, fcis) in gxFcs)
                    //    {
                    //        if (fcis.Any())
                    //        {
                    //            ServerConfig.ApiDb.Execute(
                    //                $"UPDATE `flowcard_library` SET `MarkedDateTime` = @MarkedDateTime, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][1]}` = @FaChu, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][2]}` = @HeGe, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][3]}` = @LiePian, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][4]}` = @DeviceId, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][0]}` = @Time, " +
                    //                $"`{AnalysisHelper.ParamIntDic[gx][5]}` = @JiaGongRen WHERE `Id` = @Id;",
                    //                fcis);
                    //        }
                    //    }
                    //}
                    //if (updateAcs.Any())
                    //{
                    //    ServerConfig.ApiDb.Execute("UPDATE `accounts` SET `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;", updateAcs.Values);
                    //}
                }
            }
            catch (Exception e)
            {
                Log.ErrorFormat("UseFlowCardReport,原因:{0},错误:{1}", e.Message, e.StackTrace);
            }
        }

        public static void UseFlowCardReportGet()
        {
            try
            {
                var now = DateTime.Now;
                var fcs = ServerConfig.ApiDb.Query<FlowCardReportGet>(
                    "SELECT * FROM `flowcard_report_get` WHERE State = 0 AND Time > @Time ORDER BY Time;", new { Time = now.Date.AddDays(-20) });

                if (fcs.Any())
                {
                    var specials = new List<string> { "fp", "fx", "-" };
                    var devices = DeviceLibraryHelper.GetDetails(0, fcs.Select(x => x.Code).Distinct()).ToDictionary(x => x.Code);
                    var reportFlowCards = fcs.Select(x => x.FlowCard).Concat(fcs.Select(x => x.OldFlowCard)).Distinct();
                    var flowCards = FlowCardHelper.GetFlowCards(reportFlowCards).ToDictionary(x => x.FlowCardName);
                    var processors = AccountInfoHelper.GetAccountInfoByNames(fcs.Select(x => x.Processor).Distinct()).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
                    var productions = flowCards.Any()
                        ? ProductionHelper.Instance.GetByIds<Production>(flowCards.Values.Select(x => x.ProductionProcessId).Distinct()).ToDictionary(x => x.Id)
                        : new Dictionary<int, Production>();

                    var gxFcs = new Dictionary<string, List<ErpUpdateFlowCardGet>>();
                    var updateAcs = new Dictionary<int, AccountInfo>();
                    var fr = new List<FlowCardReportGet>();
                    var updateLog = new List<MonitoringProcessLog>();

                    var gxs = fcs.GroupBy(x => x.StepAbbrev).Select(x => x.Key);
                    foreach (var gx in gxs)
                    {
                        if (!gxFcs.ContainsKey(gx))
                        {
                            gxFcs.Add(gx, new List<ErpUpdateFlowCardGet>());
                        }
                        var fcc = fcs.Where(x => x.StepAbbrev == gx).Select(fc =>
                        {
                            if (fc.NeedUpdate)
                            {
                                fc.MarkedDateTime = now;
                                if (fc.FlowCardId == 0 && !fc.FlowCard.IsNullOrEmpty())
                                {
                                    var flowCard = flowCards.ContainsKey(fc.FlowCard) ? flowCards[fc.FlowCard] : null;
                                    fc.FlowCardId = flowCard?.Id ?? 0;
                                    if (!specials.Any(x => fc.FlowCard.Contains(x)) && fc.FlowCardId == 0)
                                    {
                                        var dfc = FlowCardHelper.GetFlowCardAll(fc.FlowCard);
                                        fc.FlowCardId = dfc?.Id ?? 0;
                                    }
                                    fc.Update = fc.Update || fc.FlowCardId != 0;
                                }

                                var productionProcessId = 0;
                                if (fc.OldFlowCardId == 0 && !fc.OldFlowCard.IsNullOrEmpty())
                                {
                                    var oldFlowCard = flowCards.ContainsKey(fc.OldFlowCard) ? flowCards[fc.OldFlowCard] : null;
                                    fc.OldFlowCardId = oldFlowCard?.Id ?? 0;
                                    productionProcessId = oldFlowCard?.ProductionProcessId ?? 0;
                                    if (!specials.Any(x => fc.OldFlowCard.Contains(x)) && fc.OldFlowCardId == 0)
                                    {
                                        var dfc = FlowCardHelper.GetFlowCardAll(fc.OldFlowCard);
                                        fc.OldFlowCardId = dfc?.Id ?? 0;
                                        productionProcessId = dfc?.ProductionProcessId ?? 0;
                                    }
                                    fc.Update = fc.Update || fc.OldFlowCardId != 0;
                                }

                                if (fc.ProductionId == 0)
                                {
                                    if (productionProcessId == 0)
                                    {
                                        var oldFlowCard = flowCards.ContainsKey(fc.OldFlowCard) ? flowCards[fc.OldFlowCard] : null;
                                        productionProcessId = oldFlowCard?.ProductionProcessId ?? 0;
                                    }
                                    var production = productions.ContainsKey(productionProcessId) ? productions[productionProcessId] : null;
                                    var productionId = production?.Id ?? 0;
                                    fc.ProductionId = productionId;
                                    fc.Production = production?.ProductionProcessName ?? "";
                                    fc.Update = fc.Update || fc.ProductionId != 0;
                                }

                                if (fc.DeviceId == 0 && !fc.Code.IsNullOrEmpty())
                                {
                                    var deviceId = devices.ContainsKey(fc.Code) ? devices[fc.Code].Id : 0;
                                    fc.DeviceId = deviceId;
                                    fc.Update = fc.Update || fc.DeviceId != 0;
                                }

                                if (fc.ProcessorId == 0 && !fc.Processor.IsNullOrEmpty())
                                {
                                    var processor = processors.ContainsKey(fc.Processor) ? processors[fc.Processor] : null;
                                    var processorId = processor?.Id ?? 0;
                                    fc.ProcessorId = processorId;
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
                                        if (change && !updateAcs.ContainsKey(processorId))
                                        {
                                            updateAcs[processorId] = processor;
                                        }
                                    }
                                    fc.Update = fc.Update || fc.ProcessorId != 0;
                                }

                                var oldState = fc.State;
                                fc.State = (fc.FlowCardId == 0 || fc.OldFlowCardId == 0) ? 2 : (fc.ProcessorId == 0 && !fc.Processor.IsNullOrEmpty() ? 3 : (fc.DeviceId == 0 && !fc.Code.IsNullOrEmpty() ? 4 : 1));
                                if (oldState != fc.State)
                                {
                                    fc.Update = true;
                                }

                                if ((!specials.Any(x => fc.FlowCard.Contains(x)) || !specials.Any(x => fc.OldFlowCard.Contains(x)))
                                    && fc.State == 2 && (now - fc.Time).TotalMinutes < 10)
                                {
                                    fc.State = 0;
                                }
                            }
                            return fc;
                        });
                        fr.AddRange(fcc.Where(x => x.Update));

                        //研磨1 粗抛 2  精抛 3
                        if (AnalysisHelper.ParamAbbrevDic.ContainsKey(gx))
                        {
                            var flowCardInfos = fcc.Select(fc => new ErpUpdateFlowCardGet
                            {
                                Id = fc.FlowCardId,
                                MarkedDateTime = fc.Time,
                                FaChu = fc.Total,
                                HeGe = fc.HeGe,
                                LiePian = fc.LiePian,
                                DeviceId = fc.DeviceId,
                                Time = fc.Time,
                                JiaGongRen = fc.Processor
                            });
                            gxFcs[gx].AddRange(flowCardInfos);

                            foreach (var fc in fcc)
                            {
                                if (fc.Back)
                                {
                                    var processId = ServerConfig.ApiDb.Query<int>(
                                        "SELECT Id FROM `npc_monitoring_process_log` " +
                                        "WHERE DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime) AND FlowCardId != 0 ORDER BY StartTime DESC LIMIT 1;",
                                        new
                                        {
                                            DeviceId = fc.DeviceId,
                                            ProcessType = ProcessType.Process
                                        }).FirstOrDefault();
                                    if (processId != 0)
                                    {
                                        fc.Id1 = processId;
                                        fc.ProcessType = ProcessType.Process;
                                        ServerConfig.ApiDb.Execute(
                                            "UPDATE npc_monitoring_process_log SET `FlowCardId` = @FlowCardId, `FlowCard` = @FlowCard, `ProcessorId` = @ProcessorId, `Processor` = @Processor " +
                                            "WHERE `Id` > @Id1 AND StartTime < @Time AND DeviceId = @DeviceId AND ProcessType = @ProcessType AND NOT ISNULL(EndTime);",
                                            fc);
                                        Log.Debug($"UPDATE 流程卡:{fc.FlowCard}, 流程卡Id:{fc.FlowCardId}, 机台号:{fc.Code}, " +
                                                  $"设备Id:{fc.DeviceId}, 工序:{fc.Step}, 加工人:{fc.Processor}, Id:{fc.Id}");
                                    }
                                }
                            }
                        }
                    }

                    if (fr.Any())
                    {
                        FlowCardReportGetHelper.Update(fr);
                        //ServerConfig.ApiDb.Execute($"UPDATE `flowcard_report` SET `State` = @State WHERE `Id` = @Id;", fr);
                    }
                    if (gxFcs.Any())
                    {
                        foreach (var (gx, fcis) in gxFcs)
                        {
                            if (fcis.Any())
                            {
                                ServerConfig.ApiDb.Execute(
                                    $"UPDATE `flowcard_library` SET `MarkedDateTime` = @MarkedDateTime, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][1]}` = @FaChu, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][2]}` = @HeGe, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][3]}` = @LiePian, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][4]}` = @DeviceId, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][0]}` = @Time, " +
                                    $"`{AnalysisHelper.ParamAbbrevDic[gx][5]}` = @JiaGongRen WHERE `Id` = @Id;",
                                    fcis);
                            }
                        }
                    }
                    if (updateAcs.Any())
                    {
                        ServerConfig.ApiDb.Execute("UPDATE `accounts` SET `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;", updateAcs.Values);
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorFormat("UseFlowCardReportGet,原因:{0},错误:{1}", e.Message, e.StackTrace);
            }
        }

        private static int _dealLength = 1000;
        /// <summary>
        /// 更新设备流程卡加工记录
        /// </summary>
        private static void DeviceLastFlowCard()
        {
            try
            {
                var devices = new List<MonitoringProcess>();

                var now = DateTime.Now;
                var rTime = RedisHelper.Get<DateTime>(pTimeKey);
                var pId = RedisHelper.Get<int>(pIdKey);
                //var pDayId = RedisHelper.Get<int>(pDayIdKey);
                if (rTime == default(DateTime))
                {
                    rTime = now;
                    RedisHelper.SetForever(pTimeKey, rTime.ToStr());
                    pId = ServerConfig.ApiDb.Query<int>("SELECT IFNULL(MAX(Id), 0) FROM `flowcard_report_get` WHERE Time < @rTime AND State != 0 ORDER BY Id DESC LIMIT 1;",
                        new { rTime }).FirstOrDefault();
                    RedisHelper.SetForever(pIdKey, pId);
                    return;
                }
                var mData = ServerConfig.ApiDb.Query<FlowCardReportGet>("SELECT * FROM `flowcard_report_get` WHERE Id > @pId ORDER BY Id LIMIT @limit;",
                    new
                    {
                        pId,
                        limit = _dealLength
                    });
                if (mData.Any(x => x.State == 0))
                {
                    return;
                }

                var endId = pId;
                if (mData.Any())
                {
                    endId = mData.Max(x => x.Id);
                    mData = mData.OrderBy(x => x.Time);

                    var drs = mData.GroupBy(x => x.DeviceId).ToDictionary(y => y.Key,
                        y => y.OrderByDescending(x => x.Time).FirstOrDefault());

                    devices.AddRange(drs.Select(x => new MonitoringProcess
                    {
                        DeviceId = x.Key,
                        LastFlowCardId = x.Value.FlowCardId,
                        LastFlowCard = x.Value.FlowCard,
                    }));
                }
                RedisHelper.SetForever(pIdKey, endId);

                if (devices.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE npc_proxy_link SET `LastFlowCardId` = @LastFlowCardId, `LastFlowCard` = @LastFlowCard WHERE `DeviceId` = @DeviceId;",
                        devices);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
