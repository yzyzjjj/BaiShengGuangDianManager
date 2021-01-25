using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace ApiManagement.Base.Helper
{
    public class FlowCardHelper
    {
        public FlowCardHelper()
        {
            Init();
        }
#if DEBUG
#else
        private static Timer _timer;
#endif
        private static int _id = 0;
        private static string _createUserId = "ErpSystem";
        private static string _url = ServerConfig.ErpUrl;
        private static bool _isInsert;
        private static bool _isUpdate;
        private static bool _isUpdateFlowCardProcessStep;
        private static bool _isUpdateProductionProcessStep;
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

        public void Init()
        {
#if DEBUG
            Console.WriteLine("FlowCardHelper 调试模式已开启");
#else
            _timer = new Timer(DoSth, null, 5000, 1000 * 60 * 1);
            Console.WriteLine("FlowCardHelper 发布模式已开启");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private static void DoSth(object state)
        {
            var _pre = "FlowCard";
            var lockKey = $"{_pre}:Lock";
            if (RedisHelper.SetIfNotExist(lockKey, "lock"))
            {
                try
                {
                    RedisHelper.SetExpireAt(lockKey, DateTime.Now.AddMinutes(5));
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
                RedisHelper.Remove(lockKey);

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
                ServerConfig.ApiDb.Query<int>("SELECT `FId` FROM `flowcard_library` ORDER BY FId DESC LIMIT 1;").FirstOrDefault();

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
                var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.ProductionProcessName);
                var erpProductionLibraries = r.GroupBy(x => x.f_jhh);
                var newProductionLibraries = erpProductionLibraries.Where(x => !productionLibraries.ContainsKey(x.Key));
                var newPl = newProductionLibraries.Select(x => new ProductionLibrary
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    ProductionProcessName = x.Key
                });
                var newPlTmp = new List<ProductionLibrary>();
                newPlTmp.AddRange(newPl);
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);",
                    newPl);
                productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library` WHERE MarkedDelete = 0;").ToDictionary(x => x.ProductionProcessName);

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

                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE `MarkedDelete` = 0;").ToDictionary(x => x.FlowCardName);
                var newFlowCardLibraries = erpFlowCardLibraries.Where(x => !flowCardLibraries.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                var newFc = newFlowCardLibraries.OrderBy(x => x.Value.f_id).Select(x => new FlowCardLibrary
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
                var newFcTmp = new List<FlowCardLibrary>();
                newFcTmp.AddRange(newFc);
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `FlowCardTypeId`, `FId`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @FlowCardTypeId, @FId);",
                    newFc);

                //流程卡更新
                //todo

                flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE `MarkedDelete` = 0;").ToDictionary(x => x.FlowCardName);

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
                }).FirstOrDefault();

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

                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library` WHERE `MarkedDelete` = 0 AND FID > @fid;", new
                {
                    fid = id
                });

                var update = new List<FlowCardLibrary>();
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

            return;
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
                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT a.Id, a.FlowCardName, a.ProductionProcessId FROM `flowcard_library` a LEFT JOIN flowcard_process_step b ON a.Id = b.FlowCardId WHERE a.CreateTime > ADDDATE(NOW(), -30) AND a.MarkedDelete = 0 AND ISNULL(b.Id);", 60).ToDictionary(x => x.Id);
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
            var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT a.Id, a.ProductionProcessName FROM `production_library` a LEFT JOIN production_process_step b ON a.Id = b.ProductionProcessId WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY a.Id;").ToDictionary(x => x.ProductionProcessName);
            if (productionLibraries.Any())
            {
                var f = HttpServer.Get(_url, new Dictionary<string, string>
                {
                    { "type", "getProcessParameters" },
                    { "jhh", productionLibraries.Select(x=>x.Key).Join(",")},
                });
                if (f == "fail")
                {
                    Log.ErrorFormat("UpdateProductionProcessStep 请求erp获取流程卡数据失败,url:{0}", _url);
                    return;
                }

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
                            var deviceProcessSteps = ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT Id, StepName FROM `device_process_step` WHERE MarkedDelete = 0;");
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
                var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT a.Id, ProductionProcessName FROM `production_library` a LEFT JOIN `process_management` b ON a.Id = b.ProductModels WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) AND ProductionProcessName != '';");
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
            var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT a.Id, a.ProductionProcessName FROM `production_library` a LEFT JOIN production_specification b ON a.Id = b.ProductionProcessId WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY a.Id;").ToDictionary(x => x.ProductionProcessName);
            if (productionLibraries.Any())
            {
                var f = HttpServer.Get(_url, new Dictionary<string, string>
                {
                    { "type", "getProcessParameters" },
                    { "jhh", productionLibraries.Select(x=>x.Key).Join(",")},
                });
                if (f == "fail")
                {
                    Log.ErrorFormat("UpdateProductionSpecification 请求erp获取流程卡数据失败,url:{0}", _url);
                    return;
                }

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

                                var newProductionSpecification = erpProductionProcessStep[pl.Key].khyq.OrderBy(x => x.id);
                                newPss.AddRange(newProductionSpecification.Select(processStep => new ProductionSpecification
                                {
                                    CreateUserId = _createUserId,
                                    MarkedDateTime = now,
                                    ProductionProcessId = productionLibraries[pl.Key].Id,
                                    SpecificationName = processStep.n,
                                    SpecificationValue = new List<string> { processStep.v.Trim(), processStep.note.Trim() }.Where(x => !x.IsNullOrEmpty()).Join(",")
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
                    Log.ErrorFormat("UpdateProductionSpecification erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
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
    }
}
