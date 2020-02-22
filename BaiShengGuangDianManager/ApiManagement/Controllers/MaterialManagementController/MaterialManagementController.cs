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
        public Result IncreaseMaterial([FromBody] IncreaseMaterialManagementDetail materialManagement)
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
            var allBill = ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM `material_bill` WHERE Id IN @ids AND MarkedDelete = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
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

            MaterialHelper.InsertLog(materialManagement.Bill.Select(x => new MaterialLog
            {
                Time = markedDateTime,
                BillId = x.BillId,
                Code = allBill.First(y => y.Id == x.BillId).Code,
                Type = 1,
                Purpose = x.Purpose,
                Number = x.Number,
                RelatedPerson = x.RelatedPerson,
                Manager = createUserId
            }));

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
            var allBill = ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM `material_bill` WHERE Id IN @ids AND MarkedDelete = 0;", new { ids = mBill });
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
                        //foreach (var bill in extraBill)
                        //{
                        //    //var b = extraBill.First(x => x.BillId == bill.BillId);
                        //    //b.PlanId = materialManagement.PlanId;
                        //    //b.CreateUserId = createUserId;
                        //    //b.MarkedDateTime = markedDateTime;
                        //    bill.PlanId = materialManagement.PlanId;
                        //    bill.CreateUserId = createUserId;
                        //    bill.MarkedDateTime = markedDateTime;
                        //}
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
                MaterialHelper.InsertLog(materialManagement.Bill.Select(x => new MaterialLog
                {
                    Time = markedDateTime,
                    BillId = x.BillId,
                    Code = allBill.First(y => y.Id == x.BillId).Code,
                    Type = 2,
                    Purpose = materialManagement.PlanId != 0 ? materialManagement.Plan : x.Purpose,
                    PlanId = materialManagement.PlanId,
                    Number = x.Number,
                    RelatedPerson = x.RelatedPerson,
                    Manager = createUserId
                }));
            }
            return result;
        }
    }
}