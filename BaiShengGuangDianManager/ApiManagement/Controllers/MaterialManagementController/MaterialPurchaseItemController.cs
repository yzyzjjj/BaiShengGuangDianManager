using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料请购单列表
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialPurchaseItemController : ControllerBase
    {
        // GET: api/MaterialPurchaseItem/?qId=0
        [HttpGet]
        public DataResult GetMaterialPurchaseItem([FromQuery] int qId, int dId, int pId)
        {
            var result = new DataResult();
            var p = new List<string>();
            if (qId != 0)
            {
                p.Add(" AND Id = @qId");
            }

            if (dId != 0)
            {
                p.Add(" AND DepartmentId = @dId");
            }

            if (pId != 0)
            {
                p.Add(" AND PurchaseId = @pId");
            }

            var sql = "SELECT * FROM `material_purchase_item` WHERE `MarkedDelete` = 0" + p.Join("");
            var data = ServerConfig.ApiDb.Query<MaterialPurchaseItem>(sql, new { qId, dId, pId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialPurchaseItemNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialPurchaseItem
        [HttpPut]
        public Result PutMaterialPurchaseItem([FromBody] IEnumerable<MaterialPurchaseItem> items)
        {
            //if (items == null || !items.Any())
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            //}

            //if (items.Any(x => x.Code.IsNullOrEmpty()))
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemNotEmpty);
            //}

            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase_item` WHERE Id IN @id AND `MarkedDelete` = 0;",
            //        new { id = items.Select(x => x.Id) }).FirstOrDefault();
            //if (cnt != items.Count())
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            //}

            //if (items.Count() != items.GroupBy(x => new { x.PurchaseId, x.Code }).Count())
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemDuplicate);
            //}

            //cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase_item` WHERE Id NOT IN @Id AND PurchaseId IN @PurchaseId AND Code IN @Code AND `MarkedDelete` = 0;",
            //        new { Id = items.Select(x => x.Id), DepartmentId = items.Select(x => x.PurchaseId), Code = items.Select(x => x.Code) }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemIsExist);
            //}

            //var markedDateTime = DateTime.Now;
            //foreach (var item in items)
            //{
            //    item.MarkedDateTime = markedDateTime;
            //}

            //ServerConfig.ApiDb.Execute(
            //    "UPDATE `material_purchase_item` SET `MarkedDateTime` = @MarkedDateTime, `Class` = @Class, `Category` = @Category, `Name` = @Name, `Supplier` = @Supplier, " +
            //    "`Specification` = @Specification, `Number` = @Number, `Unit` = @Unit, `Remark` = @Remark, `Purchaser` = @Purchaser, `Order` = @Order, " +
            //    "`EstimatedTime` = @EstimatedTime, `ArrivalTime` = @ArrivalTime, `File` = @File, `FileUrl` = @FileUrl, `IsInspection` = @IsInspection, " +
            //    "`Currency` = @Currency, `Payment` = @Payment, `Transaction` = @Transaction, `Invoice` = @Invoice, `TaxPrice` = @TaxPrice, `TaxAmount` = @TaxAmount, " +
            //    "`Price` = @Price WHERE `Id` = @Id;", items);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialPurchaseItem/Increase
        [HttpPost("Increase")]
        public Result IncreaseMaterialPurchaseItem([FromBody] IEnumerable<MaterialPurchaseItem> materialPurchaseItems)
        {
            if (materialPurchaseItems == null || !materialPurchaseItems.Any())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }

            if (materialPurchaseItems.Any(x => x.Id == 0))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }

            if (materialPurchaseItems.Count() != materialPurchaseItems.GroupBy(x => x.Id).Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemDuplicate);
            }

            var purchaseIds = materialPurchaseItems.GroupBy(x => x.PurchaseId).Select(y => y.Key);
            var purchases = ServerConfig.ApiDb.Query<MaterialPurchase>("SELECT * FROM `material_purchase` WHERE `MarkedDelete` = 0 AND Id IN @purchaseIds;", new { purchaseIds });
            if (purchases.Count() != purchaseIds.Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            if (purchases.Any(x => x.State != MaterialPurchaseStateEnum.开始采购 || x.State != MaterialPurchaseStateEnum.仓库到货))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseIncreaseSateError);
            }

            var oldMaterialPurchaseItems =
                ServerConfig.ApiDb.Query<MaterialPurchaseItem>("SELECT * FROM `material_purchase_item` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = materialPurchaseItems.Select(x => x.Id) });
            if (oldMaterialPurchaseItems.Count() != materialPurchaseItems.Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var item in oldMaterialPurchaseItems)
            {
                var materialPurchaseItem = materialPurchaseItems.FirstOrDefault(x => x.Id == item.Id);
                if (materialPurchaseItem != null)
                {
                    item.MarkedDateTime = markedDateTime;
                    item.Stock += materialPurchaseItem.Count;
                }
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase_item` SET `MarkedDateTime` = @MarkedDateTime, `Stock` = @Stock WHERE `Id` = @Id;", oldMaterialPurchaseItems);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialPurchaseItem
        [HttpPost]
        public Result PostMaterialPurchaseItem([FromBody] IEnumerable<MaterialPurchaseItem> materialPurchaseItems)
        {
            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase_item` WHERE Code = @Code AND MarkedDelete = 0;",
            //        new { materialPurchaseItem.Code }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseItemIsExist);
            //}
            //materialPurchaseItem.CreateUserId = Request.GetIdentityInformation();
            //ServerConfig.ApiDb.Execute(
            //    "INSERT INTO `material_purchase_item` (`CreateUserId`, `MarkedDateTime`, `Time`, `IsErp`, `PurchaseId`, `Code`, `Class`, `Category`, `Name`, `Supplier`, `Specification`, `Number`, `Unit`, `Remark`, `Purchaser`, `Order`, `EstimatedTime`, `ArrivalTime`, `File`, `FileUrl`, `IsInspection`, `Currency`, `Payment`, `Transaction`, `Invoice`, `TaxPrice`, `TaxAmount`, `Price`) " +
            //    "VALUES (@CreateUserId, @MarkedDateTime, @Time, @IsErp, @PurchaseId, @Code, @Class, @Category, @Name, @Supplier, @Specification, @Number, @Unit, @Remark, @Purchaser, @Order, @EstimatedTime, @ArrivalTime, @File, @FileUrl, @IsInspection, @Currency, @Payment, @Transaction, @Invoice, @TaxPrice, @TaxAmount, @Price);",
            //    purchaseItems);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialPurchaseItem
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialPurchaseItem([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase_item` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}