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
        private static Timer _time;
        private static DateTime _starTime = DateTime.Now.DayBeginTime();
        private static string _createUserId = "ErpSystem";
        private static string _url = "";
        private static bool isDeal;
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
            _time = new Timer(Call, null, 5000, 1000 * 60 * 1);
        }
        private static void Call(object state)
        {
            if (isDeal)
            {
                return;
            }

            isDeal = true;
            var sTime =
                ServerConfig.ApiDb.Query<string>("SELECT `CreateTime` FROM `flowcard_library` ORDER BY CreateTime DESC LIMIT 1;").FirstOrDefault();
            if (!sTime.IsNullOrEmpty())
            {
                _starTime = DateTime.Parse(sTime);
            }
            var queryTime1 = _starTime;
            var queryTime2 = DateTime.Now;
            var r = GetData(queryTime1, queryTime2);
            _starTime = !r ? queryTime1 : queryTime2;
            isDeal = false;
        }

        public class ErpFlowCard
        {
            public int f_id;
            public string f_lckh;
            public string f_jhh;
            public string f_mate;
            public DateTime f_inserttime;
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
        private static bool GetData(DateTime starTime, DateTime endTime)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                { "type", "getHairpin" },
                { "t1", starTime.ToStr()},
                { "t2", endTime.ToStr()},
            });
            if (f == "fail")
            {
                Log.ErrorFormat("请求erp获取流程卡数据失败,url:{0}", _url);
                return false;
            }

            var now = DateTime.Now;
            try
            {
                var rr = HttpUtility.UrlDecode(f);
                var res = JsonConvert.DeserializeObject<ErpRes>(rr);
                if (res.result != "ok")
                {
                    Log.ErrorFormat("请求erp获取流程卡数据返回错误,原因:{0}", res.result);
                    return false;
                }

                var relation = res.relation;
                //车间
                var workshops = ServerConfig.ApiDb.Query<Workshop>("SELECT * FROM `workshop`;").ToDictionary(x => x.Id);
                var erpWorkshops = relation.ToDictionary(x => x.id);
                var newWorkshops = erpWorkshops.Where(x => !workshops.ContainsKey(x.Key));
                var newWs = newWorkshops.Select(x => new Workshop
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    WorkshopName = erpWorkshops[x.Key].name,
                    Abbre = erpWorkshops[x.Key].abbre
                });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO workshop (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `WorkshopName`, `Abbre`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @WorkshopName, @Abbre);",
                    newWs);

                var updateWs = erpWorkshops.Where(x => workshops.ContainsKey(x.Key) && (x.Value.name != workshops[x.Key].WorkshopName || x.Value.abbre != workshops[x.Key].Abbre));

                ServerConfig.ApiDb.Execute(
                    "UPDATE workshop SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                    "`WorkshopName` = @WorkshopName, `Short` = @Short WHERE `Id` = @Id;",
                    updateWs);

                workshops = ServerConfig.ApiDb.Query<Workshop>("SELECT * FROM `workshop`;").ToDictionary(x => x.Id);

                var r = res.data.Select(x => new ErpFlowCard
                {
                    f_lckh = x.f_lckh.TrimEnd(),
                    f_jhh = x.f_jhh.TrimEnd(),
                    f_mate = x.f_mate.TrimEnd(),
                }).ToList();
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
                        Log.ErrorFormat("请求erp获取计划号工序数据失败,url:{0}", _url);
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
                var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library`;").ToDictionary(x => x.FlowCardName);
                var erpFlowCardLibraries = r.ToDictionary(x => $"{x.f_bz:d2}{x.f_lckh}");
                var newFlowCardLibraries = erpFlowCardLibraries.Where(x => !flowCardLibraries.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                var newFc = newFlowCardLibraries.OrderBy(x => x.Value.f_id).Select(x => new FlowCardLibrary
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    FlowCardName = x.Key,
                    ProductionProcessId = productionLibraries[x.Value.f_jhh].Id,
                    RawMateriaId = rawMaterias[x.Value.f_mate].Id,
                    CreateTime = x.Value.f_inserttime,
                    WorkshopId = x.Value.f_bz
                });
                var newFcTmp = new List<FlowCardLibrary>();
                newFcTmp.AddRange(newFc);
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `WorkshopId`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @WorkshopId);",
                    newFc);

                flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library`;").ToDictionary(x => x.FlowCardName);
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
                Log.ErrorFormat("erp数据解析失败,原因:{0}", e.Message);
                return false;
            }
            return true;

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
            else if (value.Contains("～"))
            {
                //0.202～0.216mm
                t = 2;
            }
            else if (value.Contains("+") && value.Contains("-"))
            {
                //0.21+0.013/-0.027mm
                t = 3;
            }

            var num = new List<decimal>();
            var s = value.Replace(" ", "").Select(x => x.ToString()).ToArray();
            var n = "";
            foreach (var sf in s)
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

            decimal p = 0;
            switch (t)
            {
                case 1:
                    if (num.Count >= 1)
                    {
                        p = num[0];
                        break;
                    }
                    else
                    {
                        break;
                    }
                case 2:
                    if (num.Count == 1)
                    {
                        p = num[0]; break;
                    }
                    else if (num.Count >= 2)
                    {
                        p = (num[0] + num[1]) / 2; break;
                    }
                    else
                    {
                        break;
                    }
                case 3:
                    if (num.Count == 1)
                    {
                        p = num[0]; break;
                    }
                    else if (num.Count >= 3)
                    {
                        p = num[0] + (num[1] + num[2]) / 2; break;
                    }
                    else
                    {
                        break;
                    }
                default:
                    if (num.Count >= 1)
                    {
                        p = num[0];
                    }
                    break;
            }

            return p.ToRound(4);
        }
    }
}
