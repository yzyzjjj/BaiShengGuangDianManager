using ApiManagement.Base.Server;
using ApiManagement.Models;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace ApiManagement.Base.Helper
{
    public class FlowCardHelper
    {
        private static Timer _time;
        private static DateTime _starTime = DateTime.Now.DayBeginTime();
        private static string _createUserId = "ERP";
        public static void Init(IConfiguration configuration)
        {
            _time = new Timer(Call, null, 10000, 1000 * 60 * 10);
        }
        private static void Call(object state)
        {
            var sTime =
                ServerConfig.ApiDb.Query<string>("SELECT `CreateTime` FROM `flowcard_library` ORDER BY Id DESC LIMIT 1;").FirstOrDefault();
            if (!sTime.IsNullOrEmpty())
            {
                _starTime = DateTime.Parse(sTime);
            }
            var queryTime1 = _starTime;
            var queryTime2 = DateTime.Now;
            _starTime = queryTime2;
            GetData(queryTime1, queryTime2);
        }

        public class ErpFlowCard
        {
            public int Id;
            public string FlowCard;
            public string Pno;
            public string Mate;
            public DateTime InsertTime;
        }

        private static void GetData(DateTime starTime, DateTime endTime)
        {
            return;

            //var res =
            //    "[{\"Id\":1,\"FlowCard\":\"2000001\",\"Pno\":\"gl8888881\",\"Mate\":\"BG66-10481\",\"InsertTime\":\"2018-03-23 10:43:44\"},{\"Id\":2,\"FlowCard\":\"2000002\",\"Pno\":\"gl888888\",\"Mate\":\"BG66-1048\",\"InsertTime\":\"2018-03-23 10:43:44\"}]";
            //var now = DateTime.Now;
            //try
            //{
            //    var r = JsonConvert.DeserializeObject<List<ErpFlowCard>>(res);
            //    var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia`;").ToDictionary(x => x.RawMateriaName);
            //    var productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);
            //    var flowCardLibraries = ServerConfig.ApiDb.Query<FlowCardLibrary>("SELECT * FROM `flowcard_library`;").ToDictionary(x => x.FlowCardName);

            //    var erpRawMaterias = r.GroupBy(x => x.Mate);
            //    var newRawMaterias = erpRawMaterias.Where(x => !rawMaterias.ContainsKey(x.Key));
            //    var newRm = newRawMaterias.Select(x => new RawMateria
            //    {
            //        CreateUserId = _createUserId,
            //        MarkedDateTime = now,
            //        RawMateriaName = x.Key
            //    });

            //    ServerConfig.ApiDb.Execute(
            //        "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
            //        "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);",
            //        newRm);

            //    rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia`;").ToDictionary(x => x.RawMateriaName);

            //    var erpProductionLibraries = r.GroupBy(x => x.Pno);
            //    var newProductionLibraries = erpProductionLibraries.Where(x => !productionLibraries.ContainsKey(x.Key));


            //    var newPl = newProductionLibraries.Select(x => new ProductionLibrary
            //    {
            //        CreateUserId = _createUserId,
            //        MarkedDateTime = now,
            //        ProductionProcessName = x.Key
            //    });

            //    ServerConfig.ApiDb.Execute(
            //        "INSERT INTO production_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProductionProcessName`) " +
            //        "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProductionProcessName);",
            //        newPl);

            //    productionLibraries = ServerConfig.ApiDb.Query<ProductionLibrary>("SELECT * FROM `production_library`;").ToDictionary(x => x.ProductionProcessName);

            //    var erpFlowCardLibraries = r.ToDictionary(x => x.FlowCard);
            //    var newFlowCardLibraries = erpFlowCardLibraries.Where(x => !flowCardLibraries.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);

            //    var newFc = newFlowCardLibraries.Select(x => new FlowCardLibrary
            //    {
            //        CreateUserId = _createUserId,
            //        MarkedDateTime = now,
            //        FlowCardName = x.Key,
            //        ProductionProcessId = productionLibraries[x.Value.Pno].Id,
            //        RawMateriaId = rawMaterias[x.Value.Mate].Id,
            //    });

            //    ServerConfig.ApiDb.Execute(
            //        "INSERT INTO flowcard_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FlowCardName`, `ProductionProcessId`, `RawMateriaId`, `RawMaterialQuantity`, `Sender`, `InboundNum`, `Remarks`, `Priority`) " +
            //        "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FlowCardName, @ProductionProcessId, @RawMateriaId, @RawMaterialQuantity, @Sender, @InboundNum, @Remarks, @Priority);",
            //        newFc);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    throw;
            //}






        }
    }
}
