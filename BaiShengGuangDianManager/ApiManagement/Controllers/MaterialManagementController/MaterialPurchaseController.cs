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
        public DataResult GetMaterialPurchase([FromQuery] int qId, int dId, string name, string valuer, int state = -1)
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
                p.Add((state == 0 ? "" : " AND State = @state"));
            }

            if (!valuer.IsNullOrEmpty())
            {
                p.Add(" AND Valuer = @valuer");
            }
            var sql = "SELECT * FROM `material_purchase` WHERE `MarkedDelete` = 0" + p.Join("");
            var data = ServerConfig.ApiDb.Query<MaterialPurchase>(sql, new { qId, dId, name, state, valuer });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialPurchaseNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialPurchase
        [HttpPut]
        public Result PutMaterialPurchase([FromBody] MaterialPurchase materialPurchase)
        {
            if (materialPurchase == null)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            if (materialPurchase.Purchase.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase` WHERE Id = @id AND `MarkedDelete` = 0;",
                    new { id = materialPurchase.Id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_purchase` WHERE Id != @id AND Purchase = @Purchase AND `MarkedDelete` = 0;",
                    new { id = materialPurchase.Id, materialPurchase.Purchase }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseIsExist);
            }

            var markedDateTime = DateTime.Now;
            materialPurchase.MarkedDateTime = markedDateTime;

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase` SET `MarkedDateTime` = @MarkedDateTime, `Purchase` = @Purchase, `Step` = @Step, `State` = @State, `IsDesign` = @IsDesign, `Priority` = @Priority WHERE `Id` = @Id;", materialPurchase);

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
    }
}