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
        public static void Init(IConfiguration configuration)
        {
            _url = configuration.GetAppSettings<string>("ErpUrl");
            _time = new Timer(Call, null, 10000, 1000 * 60 * 1);
        }
        private static void Call(object state)
        {
            var sTime =
                ServerConfig.ApiDb.Query<string>("SELECT `CreateTime` FROM `flowcard_library` ORDER BY CreateTime DESC LIMIT 1;").FirstOrDefault();
            if (!sTime.IsNullOrEmpty())
            {
                _starTime = DateTime.Parse(sTime);
            }
            var queryTime1 = _starTime;
            var queryTime2 = DateTime.Now;
            var r = GetData(queryTime1.AddMinutes(-10), queryTime2);
            _starTime = !r ? queryTime1 : queryTime2;
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
        private static bool GetData(DateTime starTime, DateTime endTime)
        {
            var f = HttpServer.Get(_url, new Dictionary<string, string>
            {
                {"t1", starTime.ToStr()},
                {"t2", endTime.ToStr()},
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
                    Log.ErrorFormat("请求erp返回失败,原因:{0}", res.result);
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

                var r = res.data;
                if (r.Count <= 0)
                {
                    return true;
                }

                //原料批号
                var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia`;").ToDictionary(x => x.RawMateriaName);
                var erpRawMaterias = r.GroupBy(x => x.f_mate);
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

                //原料批号
                var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);
                var erpProductionLibraries = r.GroupBy(x => x.f_jhh);
                var newProductionLibraries = erpProductionLibraries.Where(x => !productionLibraries.ContainsKey(x.Key));
                var newPl = newProductionLibraries.Select(x => new ProductionLibrary
                {
                    CreateUserId = _createUserId,
                    MarkedDateTime = now,
                    ProductionProcessName = x.Key
                });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);",
                    newPl);
                productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);

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
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`, `CreateTime`, `WorkshopId`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority, @CreateTime, @WorkshopId);",
                    newFc);
            }
            catch (Exception e)
            {
                Log.ErrorFormat("erp数据解析失败,原因:{0}", e.Message);
                return false;
            }
            return true;

        }
    }
}
