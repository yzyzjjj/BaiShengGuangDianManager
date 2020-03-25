using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.PlanManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialManagementController : ControllerBase
    {

        // GET: api/MaterialManagement?categoryId=0&nameId=0&supplierId=0&specificationId=0&qId=0&siteId
        [HttpGet]
        public DataResult GetMaterialManagement([FromQuery] int categoryId, int nameId, int supplierId, int specificationId, int qId, int siteId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && nameId == 0 && supplierId == 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT b.*, a.*, a.Id BillId, c.InTime, c.OutTime, IFNULL(c.Number, 0) Number, d.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.Id = c.BillId " +
                    $"JOIN `material_site` d ON a.SiteId = d.Id WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (nameId != 0 && supplierId == 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT b.*, a.*, a.Id BillId, c.InTime, c.OutTime, IFNULL(c.Number, 0) Number, d.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id WHERE a.NameId = @nameId ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.Id = c.BillId " +
                    $"JOIN `material_site` d ON a.SiteId = d.Id  WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (supplierId != 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT b.*, a.*, a.Id BillId, c.InTime, c.OutTime, IFNULL(c.Number, 0) Number, d.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id WHERE a.SupplierId = @supplierId ) b ON a.SpecificationId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.Id = c.BillId " +
                    $"JOIN `material_site` d ON a.SiteId = d.Id WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (specificationId != 0 && qId == 0)
            {
                sql =
                    "SELECT b.*, a.*, a.Id BillId, c.InTime, c.OutTime, IFNULL(c.Number, 0) Number, d.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.Id = c.BillId " +
                    $"JOIN `material_site` d ON a.SiteId = d.Id WHERE a.SpecificationId = @specificationId AND a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else
            {
                sql =
                    "SELECT b.*, a.*, a.Id BillId, c.InTime, c.OutTime, IFNULL(c.Number, 0) Number, d.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.Id = c.BillId " +
                    $"JOIN `material_site` d ON a.SiteId = d.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }

            var data = ServerConfig.ApiDb.Query<MaterialManagementDetail>(sql, new { categoryId, nameId, supplierId, specificationId, qId, siteId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialBillNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // POST: api/MaterialManagement/Increase
        [HttpPost("Increase")]
        public object IncreaseMaterial([FromBody] IncreaseMaterialManagementDetail materialManagement)
        {
            if (materialManagement.Bill == null)
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }

            materialManagement.Bill = materialManagement.Bill.Where(x => x.Number > 0);
            if (!materialManagement.Bill.Any())
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }
            var mBill = materialManagement.Bill.GroupBy(x => x.BillId).Select(x => x.Key);
            var allBill = ServerConfig.ApiDb.Query<ProductionPlanBillStockDetail>("SELECT a.*, IFNULL(b.Number, 0) Number FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.MarkedDelete = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            var planBill = materialManagement.Bill.Where(x => x.PlanId != 0).GroupBy(y => new { y.PlanId, y.BillId }).Select(z => z.Key);
            var actualPlanBill = new List<OpMaterialManagement>();
            actualPlanBill.AddRange(planBill.Select(x => new OpMaterialManagement
            {
                BillId = x.BillId,
                PlanId = x.PlanId,
                Number = materialManagement.Bill.Where(y => y.PlanId == x.PlanId && y.BillId == x.BillId).Sum(y => y.Number),
                Purpose = materialManagement.Bill.FirstOrDefault(y => y.PlanId == x.PlanId && y.BillId == x.BillId)?.Purpose ?? "",
                RelatedPerson = materialManagement.Bill.FirstOrDefault(y => y.PlanId == x.PlanId && y.BillId == x.BillId)?.RelatedPerson ?? "",
            }));
            var logs = new List<MaterialLog>();
            var markedDateTime = DateTime.Now;
            var createUserId = Request.GetIdentityInformation();
            if (planBill.Any())
            {
                var allPlanBill = ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT Id, PlanId, BillId, ActualConsumption FROM `production_plan_bill` WHERE PlanId IN @PlanId AND BillId IN @BillId AND `MarkedDelete` = 0;",
                    new { PlanId = planBill.Select(x => x.PlanId), BillId = planBill.Select(x => x.BillId) });
                Dictionary<dynamic, dynamic> plans = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Plan FROM  `production_plan` WHERE Id IN @PlanID;",
                    new { PlanId = planBill.Select(x => x.PlanId) }).ToDictionary(x => x.Id);
                Dictionary<dynamic, dynamic> billCode = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Code FROM  `material_bill` WHERE Id IN @BillId;",
                    new { BillId = planBill.Select(x => x.BillId) }).ToDictionary(x => x.Id);
                if (allPlanBill.Count() != planBill.Count())
                {
                    var result = new DataResult { errno = Error.ProductionPlanBillNotExist };
                    var notExist = planBill.Where(x =>
                        allPlanBill.All(y => y.PlanId != x.PlanId && y.BillId != x.BillId));
                    //plans = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Plan FROM  `production_plan` WHERE Id IN @PlanID;",
                    //    new { PlanId = planBill.Select(x => x.PlanId) }).ToDictionary(x => x.Id);
                    //billCode = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Code FROM  `material_bill` WHERE Id IN @BillId;",
                    //   new { BillId = planBill.Select(x => x.BillId) }).ToDictionary(x => x.Id);
                    result.datas.AddRange(notExist.Where(x => plans.ContainsKey(x.PlanId) && billCode.ContainsKey(x.BillId)).Select(y => new { Plan = plans[y.PlanId], Code = billCode[y.BillId] }));
                    return result;
                }

                var notEnough = actualPlanBill.Where(x =>
                    allPlanBill.First(y => y.PlanId == x.PlanId && y.BillId == x.BillId).ActualConsumption < x.Number);
                if (notEnough.Any())
                {
                    var result = new DataResult { errno = Error.ProductionPlanBillActualConsumeLess };
                    result.datas.AddRange(notEnough.Where(x => plans.ContainsKey(x.PlanId) && billCode.ContainsKey(x.BillId)).Select(y => new { Plan = plans[y.PlanId], Code = billCode[y.BillId] }));
                    return result;
                }

                foreach (var bill in allPlanBill)
                {
                    bill.ActualConsumption -=
                        actualPlanBill.FirstOrDefault(y => y.PlanId == bill.PlanId && y.BillId == bill.BillId)
                            ?.Number ?? 0;
                    bill.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE production_plan_bill SET `MarkedDateTime` = @MarkedDateTime, `ActualConsumption` = @ActualConsumption WHERE `Id` = @Id;", allPlanBill);
                logs.AddRange(actualPlanBill.Select(x => new MaterialLog
                {
                    Time = markedDateTime,
                    BillId = x.BillId,
                    Code = allBill.First(y => y.Id == x.BillId).Code,
                    Type = 1,
                    PlanId = x.PlanId,
                    Plan = plans == null ? "" : plans[x.PlanId].Plan,
                    Purpose = plans == null ? "" : plans[x.PlanId].Plan,
                    Number = x.Number,
                    OldNumber = allBill.First(y => y.Id == x.BillId).Number,
                    RelatedPerson = x.RelatedPerson,
                    Manager = createUserId
                }));
            }

            #region 更新
            var existBill = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT * FROM `material_management` WHERE BillId IN @ids;", new { ids = mBill });
            foreach (var bill in existBill)
            {
                bill.Number += materialManagement.Bill.Where(x => x.BillId == bill.BillId).Sum(y => y.Number);
                bill.InTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute("UPDATE material_management SET `InTime` = @InTime, `Number` = @Number WHERE `Id` = @Id;", existBill);
            #endregion

            #region 添加
            var addBill = materialManagement.Bill.Where(x => existBill.All(y => y.BillId != x.BillId));
            ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `Number`) VALUES (@BillId, @InTime, @Number);", addBill.Select(
                x =>
                {
                    x.CreateUserId = createUserId;
                    x.InTime = markedDateTime;
                    return x;
                }));
            #endregion
            logs.AddRange(materialManagement.Bill.Where(y => y.PlanId == 0).Select(x => new MaterialLog
            {
                Time = markedDateTime,
                BillId = x.BillId,
                Code = allBill.First(y => y.Id == x.BillId).Code,
                Type = 1,
                Purpose = x.Purpose,
                Number = x.Number,
                OldNumber = allBill.First(y => y.Id == x.BillId).Number,
                RelatedPerson = x.RelatedPerson,
                Manager = createUserId
            }));
            if (logs.Any())
            {
                var sql =
                    "SELECT b.`Name`, b.NameId, a.SpecificationId, b.Specification, a.Id FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b " +
                    "ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id WHERE a.Id IN @Id;";

                var data = ServerConfig.ApiDb.Query<MaterialManagementDetail>(sql, new { Id = logs.Select(x => x.BillId) });
                MaterialHelper.InsertLog(logs.Select(x =>
                {
                    var d = data.FirstOrDefault(y => y.Id == x.BillId);
                    if (d != null)
                    {
                        x.NameId = d.NameId;
                        x.Name = d.Name;
                        x.SpecificationId = d.SpecificationId;
                        x.Specification = d.Specification;
                    }
                    return x;
                }));
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialManagement/Consume
        [HttpPost("Consume")]
        public DataResult ConsumeMaterial([FromBody] ConsumeMaterialManagementDetail materialManagement)
        {
            var result = new DataResult();
            if (materialManagement.Bill == null)
            {
                return Result.GenError<DataResult>(Error.MaterialManagementNotEmpty);
            }

            materialManagement.Bill = materialManagement.Bill.Where(x => x.Number > 0);
            if (!materialManagement.Bill.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialManagementNotEmpty);
            }
            var mBill = materialManagement.Bill.GroupBy(x => x.BillId).Select(x => x.Key);
            var allBill = ServerConfig.ApiDb.Query<ProductionPlanBillStockDetail>("SELECT  a.*, IFNULL(b.Number, 0) Number FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.MarkedDelete = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<DataResult>(Error.MaterialBillNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (materialManagement.PlanId != 0)
            {
                var plan =
                    ServerConfig.ApiDb
                        .Query<ProductionPlan>("SELECT * FROM `production_plan` WHERE Id = @id AND `MarkedDelete` = 0;",
                            new { id = materialManagement.PlanId }).FirstOrDefault();
                if (plan == null)
                {
                    return Result.GenError<DataResult>(Error.ProductionPlanNotExist);
                }

                materialManagement.Plan = plan.Plan;
            }

            #region 检验库存
            var consumeBill = mBill.Select(x => new OpMaterialManagement
            {
                BillId = x,
                Number = materialManagement.Bill.Where(y => y.BillId == x).Sum(z => z.Number)
            });
            var existBill = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT * FROM `material_management` WHERE BillId IN @ids;", new { ids = mBill });
            var less = new List<string>();
            foreach (var bill in consumeBill)
            {
                var estBill = existBill.FirstOrDefault(x => x.BillId == bill.BillId);
                if (estBill != null && estBill.Number >= bill.Number)
                {
                    continue;
                }
                less.Add(allBill.FirstOrDefault(x => x.Id == bill.BillId).Code);
            }
            #endregion

            if (less.Any())
            {
                result.errno = Error.MaterialManagementLess;
                result.datas.AddRange(less);
            }
            else
            {
                #region 计划领料
                if (materialManagement.PlanId != 0)
                {
                    var planBill = ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT * FROM `production_plan_bill` WHERE `PlanId` = @PlanId AND MarkedDelete = 0;", new
                    {
                        PlanId = materialManagement.PlanId
                    });

                    #region 更新
                    foreach (var bill in planBill)
                    {
                        var b = consumeBill.FirstOrDefault(x => x.BillId == bill.BillId);
                        if (b != null)
                        {
                            bill.MarkedDateTime = markedDateTime;
                            bill.ActualConsumption += b.Number;
                        }
                    }
                    if (planBill.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                        "UPDATE production_plan_bill SET `MarkedDateTime` = @MarkedDateTime, `ActualConsumption` = @ActualConsumption WHERE `Id` = @Id;", planBill);
                    }
                    #endregion

                    #region 添加额外领用的物料
                    var extraBill = consumeBill.Where(x => planBill.All(y => y.BillId != x.BillId)).ToList();
                    if (extraBill.Any())
                    {
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO production_plan_bill (`CreateUserId`, `MarkedDateTime`, `PlanId`, `BillId`, `ActualConsumption`, `Extra`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @BillId, @Number, 1);",
                            extraBill.Select(x =>
                            {

                                x.PlanId = materialManagement.PlanId;
                                x.CreateUserId = createUserId;
                                x.MarkedDateTime = markedDateTime;
                                return x;
                            }));
                    }

                    #endregion
                }
                #endregion

                #region 消耗
                foreach (var bill in existBill)
                {
                    bill.Number -= consumeBill.First(x => x.BillId == bill.BillId).Number;
                    bill.OutTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute("UPDATE material_management SET `OutTime` = @OutTime, `Number` = @Number WHERE `Id` = @Id;", existBill);
                #endregion

                var logs = materialManagement.Bill.Select(x => new MaterialLog
                {
                    Time = markedDateTime,
                    BillId = x.BillId,
                    Code = allBill.First(y => y.Id == x.BillId).Code,
                    Type = 2,
                    Purpose = materialManagement.PlanId != 0 ? materialManagement.Plan : x.Purpose,
                    PlanId = materialManagement.PlanId,
                    Number = x.Number,
                    OldNumber = allBill.First(y => y.Id == x.BillId).Number,
                    RelatedPerson = x.RelatedPerson,
                    Manager = createUserId
                });

                var sql =
                    "SELECT b.`Name`, b.NameId, a.SpecificationId, b.Specification, a.Id FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b " +
                    "ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id WHERE a.Id IN @Id;";

                var data = ServerConfig.ApiDb.Query<MaterialManagementDetail>(sql, new { Id = logs.Select(x => x.BillId) });
                MaterialHelper.InsertLog(logs.Select(x =>
                {
                    var d = data.FirstOrDefault(y => y.Id == x.BillId);
                    if (d != null)
                    {
                        x.NameId = d.NameId;
                        x.Name = d.Name;
                        x.SpecificationId = d.SpecificationId;
                        x.Specification = d.Specification;
                    }
                    return x;
                }));
            }
            return result;
        }

        // POST: api/MaterialManagement/Correct
        [HttpPost("Correct")]
        public object CorrectMaterial([FromBody] IncreaseMaterialManagementDetail materialManagement)
        {
            if (materialManagement.Bill == null)
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }

            if (!materialManagement.Bill.Any())
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }
            var mBill = materialManagement.Bill.GroupBy(x => x.BillId).Select(x => x.Key);
            var allBill = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT a.Id, a.`Code`, IFNULL(b.Number, 0) Number, IFNULL(b.BillId, 0) BillId FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.`MarkedDelete` = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            var updateBill = materialManagement.Bill.GroupBy(y => y.BillId).Select(z => z.Key);
            var actualBill = new List<OpMaterialManagement>();
            actualBill.AddRange(updateBill.Select(x => new OpMaterialManagement
            {
                BillId = x,
                Number = materialManagement.Bill.Where(y => y.BillId == x).Sum(y => y.Number),
                Remark = materialManagement.Bill.FirstOrDefault(y => y.BillId == x)?.Remark ?? "",
                RelatedPerson = materialManagement.Bill.FirstOrDefault(y => y.BillId == x)?.RelatedPerson ?? "",
            }));
            var logs = new List<MaterialLog>();
            var markedDateTime = DateTime.Now;
            var createUserId = Request.GetIdentityInformation();
            #region 更新

            var existBill = allBill.Where(x => x.BillId != 0);
            foreach (var bill in existBill)
            {
                var d = actualBill.FirstOrDefault(x => x.BillId == bill.BillId);
                if (d != null && d.Number != bill.Number)
                {
                    logs.Add(new MaterialLog
                    {
                        Time = markedDateTime,
                        BillId = bill.BillId,
                        Code = bill.Code,
                        Type = 3,
                        Mode = d.Number > bill.Number ? 0 : 1,
                        Purpose = d.Remark,
                        Number = d.Number,
                        OldNumber = bill.Number,
                        RelatedPerson = d.RelatedPerson,
                        Manager = createUserId
                    });
                    bill.Number = d.Number;
                }
            }

            ServerConfig.ApiDb.Execute("UPDATE material_management SET `Number` = @Number WHERE `BillId` = @BillId;", existBill);
            #endregion

            #region 添加
            var addBill = actualBill.Where(x => existBill.All(y => y.BillId != x.BillId));
            ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `Number`) VALUES (@BillId, @InTime, @Number);", addBill.Select(
                x =>
                    {
                        x.CreateUserId = createUserId;
                        x.InTime = markedDateTime;
                        return x;
                    }));
            #endregion
            if (logs.Any())
            {
                var sql =
                  "SELECT b.`Name`, b.NameId, a.SpecificationId, b.Specification, a.Id FROM `material_bill` a " +
                  "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                  "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                  "JOIN ( SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b " +
                  "ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id WHERE a.Id IN @Id;";
                var data = ServerConfig.ApiDb.Query<MaterialManagementDetail>(sql, new { Id = logs.Select(x => x.BillId) });
                MaterialHelper.InsertLog(logs.Select(x =>
                {
                    var d = data.FirstOrDefault(y => y.Id == x.BillId);
                    if (d != null)
                    {
                        x.NameId = d.NameId;
                        x.Name = d.Name;
                        x.SpecificationId = d.SpecificationId;
                        x.Specification = d.Specification;
                    }
                    return x;
                }));
            }

            return Result.GenError<Result>(Error.Success);
        }
    }
}