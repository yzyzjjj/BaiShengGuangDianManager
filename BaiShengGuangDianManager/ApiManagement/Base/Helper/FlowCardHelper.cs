using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.Extensions.Configuration;
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
        private static Timer _insertTimer;
        private static Timer _updateTimer;
        private static int _id = 0;
        private static string _createUserId = "ErpSystem";
        private static string _url = "";
        private static bool isInsert;
        private static bool isUpdate;
        private static Dictionary<string, string> _processStepName = new Dictionary<string, string>
        {
            {"线切割", "线切割"},
            {"精磨", "精磨"},
            {"Polish", "粗抛"},
            {"抛光", "粗抛"},
            {"粗抛", "粗抛"},
            {"精抛", "精抛"},
        };
        public static void Init(IConfiguration configuration)
        {
            _url = configuration.GetAppSettings<string>("ErpUrl");
            _insertTimer = new Timer(Insert, null, 5000, 1000 * 60 * 1);
            _updateTimer = new Timer(Update, null, 5000, 1000 * 60 * 1);
        }

        private static void Update(object state)
        {
            if (isUpdate)
            {
                return;
            }

            isUpdate = true;
            var sId =
                ServerConfig.ApiDb.Query<int>("SELECT FId FROM `flowcard_library` WHERE FId != 0 AND DATE(CreateTime) = @Time ORDER BY CreateTime LIMIT 1;", new
                {
                    Time = DateTime.Today.AddDays(-1)
                }).FirstOrDefault();

            if (sId != 0)
            {
                UpdateData(sId - 1);
            }
            isUpdate = false;
        }

        private static void Insert(object state)
        {
            if (isInsert)
            {
                return;
            }

            isInsert = true;
            var sId =
                ServerConfig.ApiDb.Query<int>("SELECT `FId` FROM `flowcard_library` ORDER BY FId DESC LIMIT 1;").FirstOrDefault();

            var queryId1 = _id;
            var queryId2 = sId;
            var r = InsertData(queryId2);
            _id = !r ? queryId1 : queryId2;
            isInsert = false;
        }
        private static bool InsertData(int id)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getHairpin" },
                { "id", id.ToString()},
            });
            if (f == "fail")
            {
                Log.ErrorFormat("InsertData 请求erp获取流程卡数据失败,url:{0}", _url);
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

                //原料批号
                var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia`;").ToDictionary(x => x.RawMateriaName);
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
                rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia`;").ToDictionary(x => x.RawMateriaName);

                //计划号
                var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);
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
                productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);

                //数据库计划号工序
                var productionProcessStep = ServerConfig.ApiDb.Query<ProductionProcessStepDetail>("SELECT a.*, b.ProductionProcessName FROM `production_process_step` a JOIN `production_library` b ON a.ProductionProcessId = b.Id; ")
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
                    if (rrr != "[][]")
                    {
                        var erpJhhGxes = JsonConvert.DeserializeObject<ErpJhhGx[]>(rrr);
                        if (erpJhhGxes.Any())
                        {
                            var deviceProcessSteps = ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT Id, StepName FROM `device_process_step` WHERE MarkedDelete = 0;");

                            //erp计划号工序
                            var erpProductionProcessStep = erpJhhGxes.ToDictionary(x => x.jhh);
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
                                        var n = _processStepName.ContainsKey(processStep.n)
                                            ? _processStepName[processStep.n]
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

                            productionProcessStep = ServerConfig.ApiDb.Query<ProductionProcessStepDetail>("SELECT a.*, b.ProductionProcessName FROM `production_process_step` a JOIN `production_library` b ON a.ProductionProcessId = b.Id; ")
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
                Log.ErrorFormat("InsertData erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                return false;
            }
            return true;

        }
        private static void UpdateData(int id)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getHairpin" },
                { "id", id.ToString()},
            });
            if (f == "fail")
            {
                Log.ErrorFormat("UpdateData 请求erp获取流程卡数据失败,url:{0}", _url);
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
                        if (flowCardLibrary.Sender != erpFlowCardLibrary.f_fcygbh
                            || flowCardLibrary.RawMaterialQuantity != int.Parse(erpFlowCardLibrary.f_qty)
                            || flowCardLibrary.InboundNum != erpFlowCardLibrary.f_rkxh
                            || flowCardLibrary.Remarks != erpFlowCardLibrary.f_note)
                        {
                            flowCardLibrary.Sender = erpFlowCardLibrary.f_fcygbh;
                            flowCardLibrary.RawMaterialQuantity = int.Parse(erpFlowCardLibrary.f_qty);
                            flowCardLibrary.InboundNum = erpFlowCardLibrary.f_rkxh;
                            flowCardLibrary.Remarks = erpFlowCardLibrary.f_note;
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
                Log.ErrorFormat("UpdateData erp数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                return;
            }

            return;
        }



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
        public class ErpJhhGx
        {
            public string jhh;
            public ErpGx[] gx;
        }
    }
}
