﻿using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.PlanManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers.PlanManagementController
{
    /// <summary>
    /// 计划管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ProductionPlanController : ControllerBase
    {
        // GET: api/ProductionPlan?qId=0
        [HttpGet]
        public DataResult GetProductionPlan([FromQuery]int qId, bool first, bool simple)
        {
            var sql =
                   "SELECT a.*, IFNULL(b.PlannedConsumption, 0) PlannedConsumption, IFNULL(b.ActualConsumption, 0) ActualConsumption, " +
                   "IFNULL(b.ExtraConsumption, 0) ExtraConsumption, IFNULL(b.PlannedCost, 0) PlannedCost, IFNULL(b.ActualCost, 0) ActualCost FROM `production_plan` a " +
                   "LEFT JOIN ( SELECT PlanId, SUM( IF ( Extra = 0, PlannedConsumption, 0 ) ) PlannedConsumption, SUM( IF ( Extra = 0, ActualConsumption, 0 ) ) ActualConsumption, " +
                   "SUM( IF ( Extra = 1, ActualConsumption, 0 ) ) ExtraConsumption, SUM(IF(Extra = 0, b.Price, 0)) PlannedCost, SUM(b.Price) ActualCost " +
                   $"FROM `production_plan_bill` a JOIN `material_bill` b ON a.BillId = b.Id GROUP BY PlanId ) b ON a.Id = b.PlanId WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0;";
            if (simple)
            {
                sql = $"SELECT * FROM `production_plan` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
            }
            var data = ServerConfig.ApiDb.Query<ProductionPlanDetail>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.ProductionPlanNotExist);
            }

            if (data.Any())
            {
                if (first)
                {
                    var plan = data.First();
                    sql =
                        "SELECT b.*, a.*, IFNULL(c.Number, 0) Number FROM `production_plan_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                        "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                        "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                        "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id JOIN `material_site` c ON a.SiteId = c.Id ) b ON a.BillId = b.Id " +
                        "LEFT JOIN `material_management` c ON a.BillId = c.BillId WHERE a.PlanId = @planId AND a.`MarkedDelete` = 0 ORDER BY a.Id, a.Extra;";
                    plan.FirstBill.AddRange(ServerConfig.ApiDb.Query<ProductionPlanBillStockDetail>(sql, new { planId = plan.Id }));
                }
            }
            var result = new DataResult();
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/ProductionPlan
        /// <summary>
        /// 更新计划
        /// </summary>
        /// <returns></returns>
        [HttpPut("{id}")]
        public Result PutProductionPlan([FromRoute] int id, [FromBody] OpProductionPlan productionPlan)
        {
            var cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_plan` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionPlanNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            productionPlan.Id = id;
            productionPlan.MarkedDateTime = markedDateTime;
            if (productionPlan.Bill == null || !productionPlan.Bill.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE `production_plan_bill` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `PlanId` = @PlanId;", new
                    {
                        MarkedDateTime = DateTime.Now,
                        MarkedDelete = true,
                        PlanId = id
                    });
            }
            else
            {
                productionPlan.Bill = productionPlan.Bill.Where(x => !x.Extra);
                var pBill = productionPlan.Bill.GroupBy(x => x.BillId).Select(y => y.Key);
                cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Id IN @ids AND MarkedDelete = 0;",
                    new { ids = pBill }).FirstOrDefault();
                if (cnt != pBill.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillNotExist);
                }

                var plBill = new List<ProductionPlanBill>();
                foreach (var billId in pBill.Where(x => x != 0))
                {
                    var bill = productionPlan.Bill.First(x => x.BillId == billId);
                    bill.PlannedConsumption = productionPlan.Bill.Where(x => x.BillId == billId).Sum(y => y.PlannedConsumption);
                    bill.PlanId = id;
                    bill.CreateUserId = createUserId;
                    bill.MarkedDateTime = markedDateTime;
                    plBill.Add(bill);
                }
                foreach (var billId in pBill.Where(x => x == 0))
                {
                    var bill = productionPlan.Bill.First(x => x.BillId == billId);
                    bill.PlannedConsumption = productionPlan.Bill.Where(x => x.BillId == billId).Sum(y => y.PlannedConsumption);
                    bill.PlanId = id;
                    bill.CreateUserId = createUserId;
                    bill.MarkedDateTime = markedDateTime;
                    plBill.Add(bill);
                }

                productionPlan.Bill = plBill;
                var planBill = ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT * FROM `production_plan_bill` WHERE `PlanId` = @PlanId AND Extra = 0 AND MarkedDelete = 0;", new
                {
                    PlanId = id
                });

                #region 更新
                var existBill = productionPlan.Bill.Where(x => planBill.Any(y => y.Id == x.Id));
                foreach (var bill in existBill)
                {
                    bill.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE production_plan_bill SET `MarkedDateTime` = @MarkedDateTime, `BillId` = @BillId, `PlannedConsumption` = @PlannedConsumption WHERE `Id` = @Id;", existBill);
                #endregion

                #region 删除
                var deleteBill = planBill.Where(x => productionPlan.Bill.All(y => y.Id != x.Id));
                if (deleteBill.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `production_plan_bill` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @ids;", new
                        {
                            MarkedDateTime = DateTime.Now,
                            MarkedDelete = true,
                            ids = deleteBill.Select(x => x.Id)
                        });
                }

                #endregion

                #region 添加
                var addBill = productionPlan.Bill.Where(x => planBill.All(y => y.Id != x.Id));
                if (addBill.Any())
                {
                    foreach (var bill in addBill)
                    {
                        bill.CreateUserId = createUserId;
                        bill.MarkedDateTime = markedDateTime;
                        bill.PlanId = id;
                    }
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO production_plan_bill (`CreateUserId`, `MarkedDateTime`, `PlanId`, `BillId`, `PlannedConsumption`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @BillId, @PlannedConsumption);",
                        addBill);
                }
                #endregion
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE production_plan SET `MarkedDateTime` = @MarkedDateTime, `Plan` = @Plan, `Remark` = @Remark WHERE `Id` = @Id;", productionPlan);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProductionPlan
        /// <summary>
        /// 添加新计划
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostProductionPlan([FromBody] OpProductionPlan productionPlan)
        {
            if (productionPlan.Plan.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.ProductionPlanNotEmpty);
            }
            var cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_plan` WHERE Plan = @Plan AND MarkedDelete = 0;",
                    new { productionPlan.Plan }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ProductionPlanIsExist);
            }

            IEnumerable<int> planBill = null;
            if (productionPlan.Bill != null && productionPlan.Bill.Any())
            {
                planBill = productionPlan.Bill.GroupBy(x => x.BillId).Select(y => y.Key);
                cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Id IN @ids AND MarkedDelete = 0;",
                        new { ids = planBill }).FirstOrDefault();
                if (cnt != planBill.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillNotExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;

            productionPlan.CreateUserId = createUserId;
            productionPlan.MarkedDateTime = markedDateTime;
            var id = ServerConfig.ApiDb.Query<int>(
              "INSERT INTO production_plan (`CreateUserId`, `MarkedDateTime`, `Plan`, `Remark`) VALUES (@CreateUserId, @MarkedDateTime, @Plan, @Remark);SELECT LAST_INSERT_ID();",
                productionPlan).FirstOrDefault();

            if (planBill != null)
            {
                var pBill = new List<ProductionPlanBill>();
                foreach (var billId in planBill)
                {
                    var bill = productionPlan.Bill.First(x => x.BillId == billId);
                    bill.PlannedConsumption = productionPlan.Bill.Where(x => x.BillId == billId).Sum(y => y.PlannedConsumption);
                    bill.PlanId = id;
                    bill.CreateUserId = createUserId;
                    bill.MarkedDateTime = markedDateTime;
                    pBill.Add(bill);
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO production_plan_bill (`CreateUserId`, `MarkedDateTime`, `PlanId`, `BillId`, `PlannedConsumption`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @BillId, @PlannedConsumption);",
                    pBill);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ProductionPlan
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteProductionPlan([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_plan` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionPlanNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `production_plan` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}