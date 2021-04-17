using ApiManagement.Base.Server;
using ApiManagement.Models.PlanManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

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
                   "SELECT a.*, IFNULL(b.PlannedConsumption, 0) PlannedConsumption, IFNULL(b.ActualConsumption, 0) ActualConsumption, IFNULL(b.ExtraConsumption, 0) ExtraConsumption, " +
                   "IFNULL(b.PlannedCost, 0) PlannedCost, IFNULL(b.ActualCost, 0) ActualCost FROM `production_plan` a LEFT JOIN ( SELECT PlanId, SUM(IF (Extra = 0,PlannedConsumption,0)) " +
                   "PlannedConsumption, SUM(ActualConsumption) ActualConsumption, SUM(IF (Extra = 1, ActualConsumption,0)) ExtraConsumption, SUM(IF (Extra = 0,PlannedConsumption* b.Price,0)) " +
                   "PlannedCost, SUM(ActualConsumption * b.Price) ActualCost FROM `production_plan_bill` a JOIN `material_bill` b ON a.BillId = b.Id WHERE a.`MarkedDelete` = 0 GROUP BY PlanId) b ON a.Id = b.PlanId " +
                   $"WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0 ORDER BY a.Id DESC;";
            if (simple)
            {
                sql = $"SELECT * FROM `production_plan` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0 ORDER BY Id DESC;";
            }
            var data = ServerConfig.ApiDb.Query<ProductPlanDetail>(sql, new { qId });
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
                        //"LEFT JOIN `material_management` c ON a.BillId = c.BillId WHERE a.PlanId = @planId AND a.`MarkedDelete` = 0 ORDER BY a.Extra, a.Id;";
                        "LEFT JOIN `material_management` c ON a.BillId = c.BillId WHERE a.PlanId = @planId AND a.`MarkedDelete` = 0 ORDER BY a.Id DESC;";
                    plan.FirstBill.AddRange(ServerConfig.ApiDb.Query<ProductPlanBillStockDetail>(sql, new { planId = plan.Id }));
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
        public Result PutProductionPlan([FromRoute] int id, [FromBody] OpProductPlan productionPlan)
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
                var planBill = ServerConfig.ApiDb.Query<ProductPlanBill>("SELECT * FROM `production_plan_bill` WHERE `PlanId` = @PlanId AND MarkedDelete = 0;", new
                {
                    PlanId = id
                });

                productionPlan.Bill =
                    productionPlan.Bill.OrderByDescending(x => x.Id);
                var plBill = new Dictionary<int, ProductPlanBill>();
                foreach (var bill in productionPlan.Bill)
                {
                    if (!plBill.ContainsKey(bill.BillId))
                    {
                        if (bill.Id == 0)
                        {
                            bill.CreateUserId = createUserId;
                            bill.MarkedDateTime = markedDateTime;
                            bill.PlanId = id;
                        }
                        if (planBill != null && planBill.Any())
                        {
                            bill.Extra = planBill.FirstOrDefault(x => x.BillId == bill.BillId)?.Extra ?? false;
                        }
                        plBill.Add(bill.BillId, bill);
                    }
                    else
                    {
                        plBill[bill.BillId].MarkedDateTime = markedDateTime;
                        plBill[bill.BillId].PlannedConsumption += bill.PlannedConsumption;
                    }
                }

                //productionPlan.Bill = productionPlan.Bill.Where(x => !x.Extra);
                var pBill = plBill.Values.Where(z => z.BillId != 0).GroupBy(x => x.BillId).Select(y => y.Key);
                cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Id IN @ids;",
                    new { ids = pBill }).FirstOrDefault();
                if (cnt != pBill.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillNotExist);
                }

                var updateBill = new List<ProductPlanBill>();
                #region 更新
                updateBill.AddRange(plBill.Values.Where(x => planBill.Any(y => y.Id == x.Id)));
                #endregion

                #region 删除
                var deleteBill = planBill.Where(x => plBill.Values.All(y => y.Id != x.Id));
                if (deleteBill.Any(x => x.ActualConsumption > 0))
                {
                    return Result.GenError<Result>(Error.ProductionPlanBillConsumed);
                }
                updateBill.AddRange(deleteBill.Select(x =>
                {
                    x.MarkedDelete = true;
                    return x;
                }));
                #endregion

                #region 添加
                var addBill = plBill.Values.Where(x => planBill.All(y => y.Id != x.Id));
                #endregion

                ServerConfig.ApiDb.Execute(
                    "UPDATE production_plan_bill SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete`= @MarkedDelete, `BillId` = @BillId, `PlannedConsumption` = @PlannedConsumption, `Extra` = @Extra WHERE `Id` = @Id;", updateBill);

                if (addBill.Any())
                {
                    ServerConfig.ApiDb.Execute(
                        "INSERT INTO production_plan_bill (`CreateUserId`, `MarkedDateTime`, `PlanId`, `BillId`, `PlannedConsumption`) " +
                        "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @BillId, @PlannedConsumption);",
                        addBill);
                }
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
        public Result PostProductionPlan([FromBody] OpProductPlan productionPlan)
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
                var pBill = new List<ProductPlanBill>();
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

        // POST: api/ProductionPlan
        /// <summary>
        /// 计划转移
        /// </summary>
        /// <returns></returns>
        [HttpPost("Move")]
        public Result MoveProductionPlan([FromBody] ProductPlanMove productionPlan)
        {
            if (productionPlan.Bill == null || !productionPlan.Bill.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var fromId = productionPlan.FromId;
            var toId = productionPlan.ToId;
            if (fromId == toId)
            {
                return Result.GenError<Result>(Error.ProductionPlanSame);
            }
            var plans = ServerConfig.ApiDb.Query<ProductPlan>("SELECT * FROM `production_plan` WHERE Id IN @ids AND MarkedDelete = 0;",
                new { ids = new List<int> { fromId, toId } });
            if (plans.Count() != 2)
            {
                return Result.GenError<Result>(Error.ProductionPlanNotExist);
            }

            var planBill =
                ServerConfig.ApiDb.Query<ProductPlanBill>("SELECT * FROM `production_plan_bill` WHERE Id IN @Bill AND PlanId = @fromId AND `MarkedDelete` = 0;",
                    new { fromId, productionPlan.Bill });

            if (planBill.Count() != productionPlan.Bill.Count())
            {
                return Result.GenError<Result>(Error.ProductionPlanBillNotExist);
            }

            if (planBill.Any(y => y.ActualConsumption <= 0))
            {
                return Result.GenError<Result>(Error.ProductionPlanBillNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var bill in planBill)
            {
                bill.MarkedDateTime = markedDateTime;
                bill.PlanId = toId;
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `production_plan_bill` SET `MarkedDateTime` = @MarkedDateTime, `PlanId` = @PlanId WHERE `Id` = @Id;"
                , planBill);

            var bills = planBill.GroupBy(x => x.BillId).Select(y => y.Key);
            var plan = plans.FirstOrDefault(x => x.Id == toId)?.Plan ?? "";
            ServerConfig.ApiDb.Execute(
                "UPDATE `material_log` SET PlanId = @toId, Purpose = @plan WHERE PlanId = @fromId AND BillId IN @bills;"
                , new { fromId, toId, plan, bills });

            productionPlan.CreateUserId = createUserId;
            productionPlan.MarkedDateTime = markedDateTime;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO  `production_plan_move` (`CreateUserId`, `MarkedDateTime`, `FromPlanId`, `ToPlanId`, `List`) VALUES (@CreateUserId, @MarkedDateTime, @FromId, @ToId, @List);"
                , productionPlan);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ProductionPlan
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public Result DeleteProductionPlan([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `production_plan` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProductionPlanNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `production_plan` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}