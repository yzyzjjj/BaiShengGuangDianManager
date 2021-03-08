using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;
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
    /// 物料请购单
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialPurchaseController : ControllerBase
    {
        // GET: api/MaterialPurchase/?qId=0
        [HttpGet]
        public DataResult GetMaterialPurchase([FromQuery] int qId, int dId, string name, string valuer, string order, int state = -1)
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

            if (!name.IsNullOrEmpty())
            {
                p.Add(" AND `Name` = @name");
            }

            if (state != -1)
            {
                p.Add(" AND State = @state");
            }

            if (!valuer.IsNullOrEmpty())
            {
                p.Add(" AND Valuer = @valuer");
            }

            var purchaseIds = new List<int>();
            if (!order.IsNullOrEmpty())
            {
                purchaseIds.AddRange(ServerConfig.ApiDb.Query<int>("SELECT PurchaseId FROM `material_purchase_item` " +
                                                         "WHERE MarkedDelete = 0 AND `Order` = @order GROUP BY PurchaseId;", new { order }));
                if (!purchaseIds.Any())
                {
                    return result;
                }

                if (purchaseIds.Any())
                {
                    p.Add(" AND Id IN @purchaseIds");
                }
            }
            var sql = "SELECT * FROM `material_purchase` WHERE `MarkedDelete` = 0" + p.Join("") + " ORDER BY ErpId Desc";
            var data = ServerConfig.ApiDb.Query<MaterialPurchase>(sql, new { qId, dId, name, state, valuer, purchaseIds });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialPurchaseNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialPurchase
        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="materialPurchases"></param>
        /// <returns></returns>
        [HttpPut]
        public Result PutMaterialPurchase([FromBody] IEnumerable<MaterialPurchase> materialPurchases)
        {
            if (materialPurchases == null || !materialPurchases.Any())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            var ids = materialPurchases.Select(x => x.Id);
            var oldMaterialPurchases =
                ServerConfig.ApiDb.Query<MaterialPurchase>("SELECT * FROM `material_purchase` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = ids });
            if (oldMaterialPurchases == null || !oldMaterialPurchases.Any() || oldMaterialPurchases.Count() != materialPurchases.Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }
            if (oldMaterialPurchases.Any(x => x.State != MaterialPurchaseStateEnum.订单完成))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseSateError);
            }

            var markedDateTime = DateTime.Now;
            foreach (var purchase in oldMaterialPurchases)
            {
                purchase.MarkedDateTime = markedDateTime;
                purchase.State = MaterialPurchaseStateEnum.开始采购;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State WHERE `Id` = @Id;", oldMaterialPurchases);

            TimerHelper.ErpPurchaseFunc(ids);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialPurchase
        [HttpPost]
        public Result PostMaterialPurchase([FromBody] MaterialPurchase materialPurchase)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase` WHERE Purchase = @Purchase AND MarkedDelete = 0;",
                    new { materialPurchase.Purchase }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseIsExist);
            }
            materialPurchase.CreateUserId = Request.GetIdentityInformation();
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_purchase (`CreateUserId`, `Purchase`) VALUES (@CreateUserId, @Purchase);",
              materialPurchase);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialPurchase/Complete
        [HttpPost("Complete")]
        public Result CompleteMaterialPurchase([FromBody] IEnumerable<MaterialPurchase> materialPurchase)
        {
            if (materialPurchase == null || !materialPurchase.Any())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            if (materialPurchase.Any(x => x.Id == 0))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            if (materialPurchase.Count() != materialPurchase.GroupBy(x => x.Id).Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseDuplicate);
            }

            var purchaseIds = materialPurchase.Select(y => y.Id);
            var purchases = ServerConfig.ApiDb.Query<MaterialPurchase>("SELECT * FROM `material_purchase` WHERE `MarkedDelete` = 0 AND Id IN @purchaseIds;", new { purchaseIds });
            if (purchases.Count() != purchaseIds.Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            //var oldMaterialPurchaseItems =
            //    ServerConfig.ApiDb.Query<MaterialPurchaseItem>("SELECT * FROM `material_purchase_item` WHERE PurchaseId IN @purchaseIds AND `MarkedDelete` = 0;",
            //        new { purchaseIds });

            //var markedDateTime = DateTime.Now;
            //foreach (var item in oldMaterialPurchaseItems)
            //{
            //    var materialPurchaseItem = materialPurchase.FirstOrDefault(x => x.Id == item.Id);
            //    if (materialPurchaseItem != null)
            //    {
            //        item.PurchaseId
            //        item.MarkedDateTime = markedDateTime;
            //        item.Stock += materialPurchaseItem.Count;
            //    }
            //}

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase` SET `MarkedDateTime` = now(), `State` = @State WHERE Id IN @purchaseIds;", new { state = MaterialPurchaseStateEnum.订单完成, purchaseIds });

            return Result.GenError<Result>(Error.Success);
        }
        // DELETE: api/MaterialPurchase
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialPurchase([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialPurchase/Quote
        [HttpGet("Quote")]
        public Result GetQuoteMaterialPurchase([FromQuery] int qId, int pId)
        {
            var result = new DataResult();
            var p = new List<string>();
            if (qId != 0)
            {
                p.Add(" AND Id = @qId");
            }

            if (pId != 0)
            {
                p.Add(" AND PurchaseId = @pId");
            }

            var sql = "SELECT * FROM `material_purchase_quote` WHERE `MarkedDelete` = 0" + p.Join("");
            var data = ServerConfig.ApiDb.Query<MaterialPurchaseQuote>(sql, new { qId, pId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialPurchaseNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // POST: api/MaterialPurchase/Quote
        [HttpPost("Quote")]
        public Result QuoteMaterialPurchase([FromBody] MaterialPurchase materialPurchase)
        {
            if (materialPurchase.Id == 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }
            var purchaseId = materialPurchase.Id;
            var cnt = ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase` WHERE `MarkedDelete` = 0 AND Id = @purchaseId", new
            {
                purchaseId
            }).FirstOrDefault();
            if (cnt <= 0)
            {
                return Result.GenError<DataResult>(Error.MaterialPurchaseNotExist);
            }

            var materialPurchaseQuote = materialPurchase.Items;
            if (materialPurchaseQuote == null || !materialPurchaseQuote.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE `material_purchase_quote` SET `MarkedDateTime` = NOW(), `MarkedDelete` = true WHERE `PurchaseId` = @Id;", new { materialPurchase.Id });
                return Result.GenError<Result>(Error.Success);
            }
            if (materialPurchaseQuote.Any(x => x.Illegal()))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseQuoteNotEmpty);
            }

            var oldMaterialPurchaseQuote = ServerConfig.ApiDb.Query<MaterialPurchaseQuote>("SELECT * FROM `material_purchase_quote` WHERE `MarkedDelete` = 0 AND `PurchaseId` = @PurchaseId;",
                new { purchaseId });

            var materialPurchaseItems = ServerConfig.ApiDb.Query<MaterialPurchaseQuote>("SELECT * FROM `material_purchase_item` WHERE `MarkedDelete` = 0 AND `PurchaseId` = @PurchaseId;",
                new { purchaseId });
            var markedDateTime = DateTime.Now;
            var delQuote = oldMaterialPurchaseQuote.Where(x => materialPurchaseQuote.All(y => y.Id != x.Id));
            if (delQuote.Any())
            {
                foreach (var quote in delQuote)
                {
                    quote.MarkedDateTime = markedDateTime;
                    quote.MarkedDelete = true;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE `material_purchase_quote` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete WHERE `Id` = @Id;", delQuote);
            }

            var oldQuote = materialPurchaseQuote.Where(x => x.Id != 0);
            if (oldQuote.Any())
            {
                foreach (var quote in oldQuote)
                {
                    quote.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE `material_purchase_quote` SET `MarkedDateTime` = @MarkedDateTime, `Purchase` = @Purchase, `Time` = @Time, `ItemId` = @ItemId, `PurchaseId` = @PurchaseId, " +
                    "`Code` = @Code, `Class` = @Class, `Category` = @Category, `Name` = @Name, `Supplier` = @Supplier, `Specification` = @Specification, `Number` = @Number, `Unit` = @Unit, " +
                    "`Price` = @Price, `TaxPrice` = @TaxPrice, `TaxAmount` = @TaxAmount, `TaxRate` = @TaxRate, `Order` = @Order, `Purchaser` = @Purchaser, `PurchasingCompany` = @PurchasingCompany WHERE `Id` = @Id;", oldQuote);
            }
            var newQuote = materialPurchaseQuote.Where(x => x.Id == 0);
            if (newQuote.Any())
            {
                var createUserId = Request.GetIdentityInformation();
                foreach (var quote in newQuote)
                {
                    var item = materialPurchaseItems.FirstOrDefault(x => x.Id == quote.ItemId);
                    quote.CreateUserId = createUserId;
                    quote.PurchaseId = purchaseId;
                    quote.Class = quote.Class.IsNullOrEmpty() ? item?.Class ?? "" : quote.Class;
                    quote.Category = quote.Category.IsNullOrEmpty() ? item?.Category ?? "" : quote.Category;
                    quote.Supplier = quote.Supplier.IsNullOrEmpty() ? item?.Supplier ?? "" : quote.Supplier;
                    quote.Order = quote.Order.IsNullOrEmpty() ? item?.Order ?? "" : quote.Order;
                    quote.Purchaser = quote.Purchaser.IsNullOrEmpty() ? item?.Purchaser ?? "" : quote.Purchaser;
                    quote.PurchasingCompany = quote.PurchasingCompany.IsNullOrEmpty() ? item?.PurchasingCompany ?? "" : quote.PurchasingCompany;
                }

                ServerConfig.ApiDb.Execute(
                    "INSERT INTO `material_purchase_quote` (`CreateUserId`, `Purchase`, `Time`, `ItemId`, `PurchaseId`, `Code`, `Class`, `Category`, `Name`, `Supplier`, `Specification`, `Number`, `Unit`, `Price`, `TaxPrice`, `TaxAmount`, `TaxRate`, `Order`, `Purchaser`, `PurchasingCompany`) " +
                    "VALUES (@CreateUserId, @Purchase, @Time, @ItemId, @PurchaseId, @Code, @Class, @Category, @Name, @Supplier, @Specification, @Number, @Unit, @Price, @TaxPrice, @TaxAmount, @TaxRate, @Order, @Purchaser, @PurchasingCompany);", newQuote);
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase` SET `MarkedDateTime` = now(), `IsQuote` = @IsQuote WHERE Id = @purchaseId;", new { IsQuote = true, purchaseId });

            return Result.GenError<Result>(Error.Success);
        }
    }
}