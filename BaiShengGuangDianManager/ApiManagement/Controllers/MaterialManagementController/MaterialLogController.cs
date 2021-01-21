using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using ApiManagement.Models.PlanManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料日志
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialLogController : ControllerBase
    {
        // GET: api/MaterialLog?planId=0&qId=0...
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="isPlan">是否是计划相关 0  1  </param>
        /// <param name="planId"></param>
        /// <param name="billId"></param>
        /// <param name="qId">日志Id</param>
        /// <param name="type">0  ; 1 入库; 2 出库;</param>
        /// <param name="purposeId">0 所有 1 计划 2 其他</param>
        /// <param name="purpose">来源/用途</param>
        /// <returns></returns>
        [HttpGet]
        public MaterialDataResult GetMaterialLog([FromQuery] DateTime startTime, DateTime endTime, int isPlan, int planId, int billId, int qId, int type, int purposeId, string purpose = "")
        {
            var result = new MaterialDataResult();
            var param = new List<string>();
            if (startTime != default(DateTime) && endTime != default(DateTime))
            {
                param.Add("a.Time >= @startTime AND a.Time <= @endTime");
            }
            if (isPlan != 0)
            {
                param.Add("a.PlanId != 0");
            }
            if (planId != 0)
            {
                param.Add("a.PlanId = @planId");
            }
            if (purposeId == 1)
            {
                param.Add("a.PlanId != 0");
            }
            else if (purposeId == 2)
            {
                param.Add("a.PlanId = 0");
            }
            if (billId != 0)
            {
                param.Add("a.BillId = @billId");
            }
            if (qId != 0)
            {
                param.Add("a.Id = @qId");
            }
            if (type != 0)
            {
                param.Add("a.Type = @type");
            }
            if (purpose != "")
            {
                param.Add("a.Purpose = @purpose");
            }
            param.Add("a.Number != 0");

            //var sql = "SELECT a.*, b.Plan, c.`Code`, c.`Price`, d.`Name`, e.Specification FROM `material_log` a " +
            //          "LEFT JOIN `production_plan` b ON a.PlanId = b.Id " +
            //          "LEFT JOIN `material_bill` c ON a.BillId = c.Id " +
            //          "LEFT JOIN `material_name` d ON a.NameId = d.Id " +
            //          "LEFT JOIN `material_specification` e ON a.SpecificationId = e.Id";
            var sql =
                "SELECT a.*, b.* FROM `material_log` a " +
                "JOIN (SELECT a.*, b.`Category` " +
                "FROM (SELECT a.*, b.`Name`, b.CategoryId " +
                "FROM (SELECT a.*, b.Supplier, b.NameId " +
                "FROM (SELECT a.Id BillId, a.`Code`, a.`Price`, a.Unit, a.SpecificationId, b.Specification, b.SupplierId, c.Site " +
                "FROM material_bill a " +
                "JOIN material_specification b ON a.SpecificationId = b.Id " +
                "JOIN material_site c ON a.SiteId = c.Id) a " +
                "JOIN material_supplier b ON a.SupplierId = b.Id) a " +
                "JOIN material_name b ON a.NameId = b.Id) a " +
                "JOIN material_category b ON a.CategoryId = b.Id) b ON a.BillId = b.BillId";

            if (param.Any())
            {
                sql += " WHERE " + param.Join(" AND ");
            }
            var data = ServerConfig.ApiDb.Query<MaterialLog>(sql, new
            {
                startTime,
                endTime,
                planId,
                billId,
                qId,
                type,
                purpose
            }).OrderByDescending(x => x.Id);
            result.Count = data.Sum(x => x.Number);
            result.Sum = data.Sum(x => x.Number * x.Price);
            result.datas.AddRange(data);
            return result;
        }



        /// <summary>
        /// 入库修正/领用修正
        /// </summary>
        /// <param name="materialLogs"></param>
        /// <returns></returns>
        // PUT: api/MaterialLog
        [HttpPut]
        public DataResult PutMaterialLog([FromBody] IEnumerable<MaterialLog> materialLogs)
        {
            if (materialLogs == null || !materialLogs.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialLogNotExist);
            }

            if (materialLogs.Any(x => x.Number < 0))
            {
                return Result.GenError<DataResult>(Error.NumberCannotBeNegative);
            }
            if (materialLogs.GroupBy(x => x.Type).Count() > 1)
            {
                return Result.GenError<DataResult>(Error.MaterialLogTypeDifferent);
            }

            var logIds = materialLogs.Where(x => x.Number == 0).Select(y => y.Id);
            var billIds = materialLogs.Select(x => x.BillId);
            var timeLog = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM (SELECT * FROM `material_log` " +
                    $"WHERE BillId IN @billIds{(logIds.Any() ? " AND Id NOT IN @logIds" : "")} AND Number > 0 Order By Time DESC) a GROUP BY Type;",
                    new { billIds, logIds });
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_log` WHERE Id IN @id;",
                    new { id = materialLogs.Select(x => x.Id) }).FirstOrDefault();
            if (cnt != materialLogs.Count())
            {
                return Result.GenError<DataResult>(Error.MaterialLogNotExist);
            }

            var result = new DataResult();
            var markedDateTime = DateTime.Now;
            var newLogs = new List<MaterialLog>();
            var newLogChanges = new List<MaterialLogChange>();
            var materials = billIds.ToDictionary(x => x, x =>
            {
                var change = new MaterialManagementChange
                {
                    BillId = x,
                    Number = 0
                };
                // 1 入库; 2 出库;3 冲正;
                var inLog = timeLog.FirstOrDefault(y => y.BillId == x && y.Type == 1);
                if (inLog != null)
                {
                    change.InTime = inLog.Time;
                }

                var outLog = timeLog.FirstOrDefault(y => y.BillId == x && y.Type == 2);
                if (outLog != null)
                {
                    change.OutTime = outLog.Time;
                }

                return change;
            });
            foreach (var billId in billIds)
            {
                var minId = materialLogs.Where(x => x.BillId == billId).Min().Id;
                var billLogs = ServerConfig.ApiDb.Query<MaterialLog>("SELECT * FROM `material_log` WHERE Id >= @Id AND BillId = @BillId Order By Time;", new { Id = minId, BillId = billId });
                foreach (var billLog in billLogs)
                {
                    var changeBillLog = ClassExtension.ParentCopyToChild<MaterialLog, MaterialLogChange>(billLog);
                    if (!materials.ContainsKey(billId))
                    {
                        continue;
                    }
                    var material = materials[billId];
                    if (!material.Init)
                    {
                        material.Init = true;
                        material.Number = billLog.OldNumber;
                    }
                    var update = materialLogs.FirstOrDefault(x => x.Id == billLog.Id);
                    if (update == null)
                    {
                        billLog.OldNumber = material.Number;
                    }
                    var num = update?.Number ?? billLog.Number;
                    // 1 入库; 2 出库;3 冲正;
                    switch (billLog.Type)
                    {
                        case 1:
                            billLog.Number = num;
                            billLog.OldNumber = material.Number;
                            material.Number += num;
                            break;
                        case 2:
                            billLog.Number = num;
                            if (billLog.Number > material.Number)
                            {
                                result.datas.Add(billLog.Name);
                                result.errno = Error.MaterialLogConsumeLaterError;
                                return result;
                            }
                            billLog.OldNumber = material.Number;
                            material.Number -= num;
                            break;
                        case 3:
                            billLog.Number = num;
                            billLog.OldNumber = material.Number;
                            material.Number = num;
                            break;
                    }
                    changeBillLog.ChangeNumber = billLog.Number;
                    changeBillLog.ChangeOldNumber = billLog.OldNumber;
                    newLogs.Add(billLog);
                    newLogChanges.Add(changeBillLog);
                }
            }

            if (newLogChanges.All(x => x.ChangeNumber == x.Number))
            {
                return Result.GenError<DataResult>(Error.Success);
            }
            var planLogs = newLogChanges.Where(x => x.PlanId != 0 && x.ChangeNumber != x.Number);
            if (planLogs.Any())
            {
                var existProductionPlanBills = ServerConfig.ApiDb.Query<ProductionPlanBill>(
                    "SELECT * FROM `production_plan_bill` WHERE MarkedDelete = 0 AND PlanId IN @PlanId AND BillId IN @BillId;",
                    new
                    {
                        PlanId = planLogs.Select(x => x.PlanId),
                        BillId = planLogs.Select(x => x.BillId),
                    });
                foreach (var billLog in newLogChanges.OrderByDescending(x => x.Id))
                {
                    if (billLog.PlanId != 0)
                    {
                        var planBill = existProductionPlanBills.FirstOrDefault(x =>
                            x.PlanId == billLog.PlanId && x.BillId == billLog.BillId);
                        if (planBill != null)
                        {
                            // 1 退回; 2 领用;
                            switch (billLog.Type)
                            {
                                case 1:
                                    planBill.ActualConsumption += billLog.Number;
                                    break;
                                case 2:
                                    planBill.ActualConsumption -= billLog.Number;
                                    break;
                                case 3:
                                    break;
                            }
                        }
                    }
                }

                //var productionPlanBills = new List<ProductionPlanBill>();
                foreach (var billLog in newLogChanges)
                {
                    if (billLog.PlanId != 0)
                    {
                        var planBill = existProductionPlanBills.FirstOrDefault(x =>
                            x.PlanId == billLog.PlanId && x.BillId == billLog.BillId);
                        if (planBill != null)
                        {
                            // 1 退回; 2 领用;
                            switch (billLog.Type)
                            {
                                case 1:
                                    if (planBill.ActualConsumption < billLog.ChangeNumber)
                                    {
                                        result.datas.Add(billLog.Name);
                                        result.errno = Error.ProductionPlanBillActualConsumeLess;
                                        return result;
                                    }
                                    planBill.ActualConsumption -= billLog.ChangeNumber;
                                    break;
                                case 2:
                                    planBill.ActualConsumption += billLog.ChangeNumber;
                                    break;
                                case 3:
                                    break;
                            }
                        }
                    }
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE `production_plan_bill` SET `ActualConsumption` = @ActualConsumption WHERE `Id` = @Id;",
                    existProductionPlanBills);
            }

            var purchases = newLogChanges.Where(x => x.Purpose.Contains("Erp采购"));
            if (purchases.Any())
            {
                var erpIds = purchases.Select(x => int.TryParse(x.Purpose.Replace("Erp采购-", ""), out var erpId) ? erpId : 0).Where(y => y != 0);
                var pBillIds = purchases.Select(x => x.BillId);
                var purchaseItems = ServerConfig.ApiDb.Query<MaterialPurchaseItem>(
                    "SELECT a.*, b.ErpId FROM `material_purchase_item` a " +
                    "JOIN `material_purchase` b ON a.PurchaseId = b.Id " +
                    "WHERE b.ErpId IN @erpIds AND a.BillId IN @pBillIds AND a.MarkedDelete = 0 AND b.MarkedDelete = 0;", new
                    {
                        erpIds,
                        pBillIds
                    });

                foreach (var purchase in purchases)
                {
                    if (int.TryParse(purchase.Purpose.Replace("Erp采购-", ""), out var erpId) && erpId != 0)
                    {
                        var pItem = purchaseItems.FirstOrDefault(x => x.BillId == purchase.BillId && x.ErpId == erpId);
                        //if (purchase.ChangeNumber < purchase.Number)
                        if (pItem != null)
                        {
                            pItem.Stock += purchase.ChangeNumber - purchase.Number;
                        }
                    }
                }

                ServerConfig.ApiDb.Execute("UPDATE `material_purchase_item` SET `Stock` = @Stock WHERE `Id` = @Id;",
                    purchaseItems);
            }
            if (newLogs.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE `material_log` SET `Number` = @Number, `OldNumber` = @OldNumber WHERE `Id` = @Id;",
                        newLogs);

                ServerConfig.ApiDb.Execute(
                    "UPDATE `material_management` SET " +
                    "`InTime` = IF(ISNULL(`InTime`) OR `InTime` != @InTime, @InTime, `InTime`), " +
                    "`OutTime` = IF(ISNULL(`OutTime`) OR `OutTime` != @OutTime, @OutTime, `OutTime`), " +
                    "`Number` = @Number WHERE `BillId` = @BillId;",
                        materials.Values.Where(x => x.Init));

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO `material_log_change` (`NewTime`, `Id`, `Time`, `BillId`, `Code`, `NameId`, `Name`, `SpecificationId`, `Specification`, `Type`, `Mode`, `Purpose`, `PlanId`, `Plan`, `Number`, `OldNumber`, `RelatedPerson`, `Manager`, `ChangeNumber`, `ChangeOldNumber`) " +
                    "VALUES ( @NewTime, @Id, @Time, @BillId, @Code, @NameId, @Name, @SpecificationId, @Specification, @Type, @Mode, @Purpose, @PlanId, @Plan, @Number, @OldNumber, @RelatedPerson, @Manager, @ChangeNumber, @ChangeOldNumber);",
                    newLogChanges.OrderBy(x => x.BillId).ThenBy(y => y.Time).Select(z =>
                      {
                          z.NewTime = markedDateTime;
                          return z;
                      }));

                //TimerHelper.DayBalance(newLogs.GroupBy(x => x.Time).Where(y => !y.Key.InSameDay(markedDateTime)).Select(z => z.Key));
                TimerHelper.DayBalance(newLogs);
            }


            return Result.GenError<DataResult>(Error.Success);
        }
    }
}