using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.PlanManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.PlanManagementController
{
    /// <summary>
    /// 计划所用物料
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ProductionPlanBillController : ControllerBase
    {
        // GET: api/ProductionPlanBill?planId=0&qId=0&qId=0

        [HttpGet]
        public DataResult GetProductionPlanBill([FromQuery] int planId, int qId, bool stock)
        {
            var result = new DataResult();
            string sql;
            if (!stock)
            {
                if (planId != 0 && qId == 0)
                {
                    sql =
                        "SELECT b.*, a.* FROM `production_plan_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                        "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                        "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                        "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id JOIN `material_site` c ON a.SiteId = c.Id ) b ON a.BillId = b.Id " +
                        "WHERE a.PlanId = @planId AND a.`MarkedDelete` = 0 ORDER BY a.Extra, a.Id;";
                }
                else
                {
                    sql =
                        "SELECT b.*, a.* FROM `production_plan_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                        "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                        "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                        "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                        $"JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id JOIN `material_site` c ON a.SiteId = c.Id ) b ON a.BillId = b.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0 ORDER BY a.Extra, a.Id;";
                }

                var data = ServerConfig.ApiDb.Query<ProductPlanBillDetail>(sql, new {planId, qId});
                if (qId != 0 && !data.Any())
                {
                    return Result.GenError<DataResult>(Error.ProductionPlanBillNotExist);
                }
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    "SELECT b.*, a.*, IFNULL(c.Number, 0) Number FROM `production_plan_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id JOIN `material_site` c ON a.SiteId = c.Id ) b ON a.BillId = b.Id " +
                    "LEFT JOIN `material_management` c ON a.BillId = c.BillId WHERE a.PlanId = @planId AND a.`MarkedDelete` = 0 ORDER BY a.Id, a.Extra;";
                result.datas.AddRange(ServerConfig.ApiDb.Query<ProductPlanBillStockDetail>(sql, new { planId }));
            }
            return result;
        }

        //// PUT: api/ProductionPlanBill
        //[HttpPut]
        //public Result PutProductionPlanBill([FromBody] IEnumerable<ProductionPlanBill> materialBills)
        //{
        //    if (materialBills == null)
        //    {
        //        return Result.GenError<Result>(Error.ProductionPlanBillNotExist);
        //    }

        //    if (materialBills.Any(x => x.UpdateImage))
        //    {
        //        if (materialBills.Count() != 1)
        //        {
        //            return Result.GenError<Result>(Error.ParamError);
        //        }
        //        var markedDateTime = DateTime.Now;
        //        try
        //        {
        //            foreach (var materialBill in materialBills)
        //            {
        //                var imageList = JsonConvert.DeserializeObject<IEnumerable<string>>(materialBill.Images);
        //                materialBill.Images = imageList.ToJSON();
        //                materialBill.MarkedDateTime = markedDateTime;
        //            }

        //            ServerConfig.ApiDb.Execute(
        //                "UPDATE material_bill SET `MarkedDateTime` = @MarkedDateTime, `Images` = @Images WHERE `Id` = @Id;", materialBills);
        //        }
        //        catch (Exception e)
        //        {
        //            return Result.GenError<Result>(Error.ParamError);
        //        }
        //    }
        //    else
        //    {
        //        if (materialBills.Any(x => x.Code.IsNullOrEmpty()))
        //        {
        //            return Result.GenError<Result>(Error.ProductionPlanBillNotEmpty);
        //        }

        //        var materialCodes = materialBills.GroupBy(x => x.SpecificationId).Select(y => y.Key);
        //        var cnt =
        //            ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialCodes }).FirstOrDefault();
        //        if (cnt != materialCodes.Count())
        //        {
        //            return Result.GenError<Result>(Error.MaterialSupplierNotExist);
        //        }

        //        var ids = materialBills.Select(x => x.Id);
        //        var uProductionPlanBills =
        //            ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT * FROM `material_bill` WHERE Id IN @id AND `MarkedDelete` = 0;",
        //                new { id = ids });
        //        if (uProductionPlanBills.Count() != materialBills.Count())
        //        {
        //            return Result.GenError<Result>(Error.ProductionPlanBillNotExist);
        //        }
        //        foreach (var materialBill in materialBills)
        //        {
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).SpecificationId = materialBill.SpecificationId;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Code = materialBill.Code;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Unit = materialBill.Unit;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Price = materialBill.Price;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Stock = materialBill.Stock;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Images = materialBill.Images;
        //            uProductionPlanBills.First(x => x.Id == materialBill.Id).Remark = materialBill.Remark;
        //        }

        //        if (uProductionPlanBills.GroupBy(x => new { x.SpecificationId, x.Code }).Select(y => y.Key).Count() != uProductionPlanBills.Count())
        //        {
        //            return Result.GenError<Result>(Error.ProductionPlanBillDuplicate);
        //        }

        //        var mBs =
        //            ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT * FROM `material_bill` WHERE SpecificationId IN @specificationId AND Id NOT IN @id AND `MarkedDelete` = 0;",
        //                new { specificationId = materialCodes, id = ids }).ToList();

        //        foreach (var materialCode in materialCodes)
        //        {
        //            var mns = mBs.Where(x => x.SpecificationId == materialCode);
        //            var uMns = uProductionPlanBills.Where(x => x.SpecificationId == materialCode);
        //            foreach (var materialBill in uMns)
        //            {
        //                if (!mns.Any(x => x.Id == materialBill.Id))
        //                {
        //                    mBs.Add(materialBill);
        //                }
        //            }
        //        }

        //        if (mBs.GroupBy(x => new { x.SpecificationId, x.Code }).Select(y => y.Key).Count() != mBs.Count())
        //        {
        //            return Result.GenError<Result>(Error.ProductionPlanBillIsExist);
        //        }

        //        var markedDateTime = DateTime.Now;
        //        foreach (var materialBill in materialBills)
        //        {
        //            materialBill.MarkedDateTime = markedDateTime;
        //        }

        //        ServerConfig.ApiDb.Execute(
        //            "UPDATE material_bill SET `MarkedDateTime` = @MarkedDateTime, `SpecificationId` = @SpecificationId, `Code` = @Code, `Unit` = @Unit, `Price` = @Price, `Stock` = @Stock, `Images` = @Images, `Remark` = @Remark WHERE `Id` = @Id;", materialBills);
        //    }
        //    return Result.GenError<Result>(Error.Success);
        //}

        //// POST: api/ProductionPlanBill
        //[HttpPost]
        //public Result PostProductionPlanBill([FromBody] ProductionPlanBill materialBill)
        //{
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_specification` WHERE Id = @Id AND `MarkedDelete` = 0;",
        //            new { Id = materialBill.SpecificationId }).FirstOrDefault();
        //    if (cnt == 0)
        //    {
        //        return Result.GenError<Result>(Error.MaterialSpecificationNotExist);
        //    }

        //    cnt =
        //       ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Code = @Code AND SpecificationId = @SpecificationId AND MarkedDelete = 0;",
        //           new { materialBill.Code, materialBill.SpecificationId }).FirstOrDefault();
        //    if (cnt > 0)
        //    {
        //        return Result.GenError<Result>(Error.ProductionPlanBillIsExist);
        //    }
        //    materialBill.CreateUserId = Request.GetIdentityInformation();
        //    materialBill.MarkedDateTime = DateTime.Now;
        //    ServerConfig.ApiDb.Execute(
        //      "INSERT INTO material_bill (`CreateUserId`, `MarkedDateTime`, `SpecificationId`, `Code`, `Unit`, `Price`, `Stock`, `Images`, `Remark`) " +
        //      "VALUES (@CreateUserId, @MarkedDateTime, @SpecificationId, @Code, @Unit, @Price, @Stock, @Images, @Remark);",
        //      materialBill);

        //    return Result.GenError<Result>(Error.Success);
        //}

        //// DELETE: api/ProductionPlanBill
        ///// <summary>
        ///// 删除
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[HttpDelete]
        //public Result DeleteProductionPlanBill([FromRoute] int id)
        //{
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
        //    if (cnt == 0)
        //    {
        //        return Result.GenError<Result>(Error.ProductionPlanBillNotExist);
        //    }

        //    ServerConfig.ApiDb.Execute(
        //        "UPDATE `material_bill` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
        //        {
        //            MarkedDateTime = DateTime.Now,
        //            MarkedDelete = true,
        //            Id = ids
        //        });
        //    return Result.GenError<Result>(Error.Success);
        //}
    }
}