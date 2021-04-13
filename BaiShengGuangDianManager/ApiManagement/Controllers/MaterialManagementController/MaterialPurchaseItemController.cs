using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelBase.Models.BaseModel;

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


        // GET: api/MaterialPurchaseItem/?qId=0
        [HttpGet("Info")]
        public DataResult GetMaterialPurchaseItemInfo([FromQuery] string ids, string pIds)
        {
            var result = new DataResult();
            var p = new List<string>();
            try
            {
                var idList = ids.IsNullOrEmpty() ? new List<int>() : ids.Split(",").Select(int.Parse);
                var pIdList = pIds.IsNullOrEmpty() ? new List<int>() : pIds.Split(",").Select(int.Parse);
                if (idList.Any())
                {
                    p.Add("a.ErpId IN @idList");
                }

                if (pIdList.Any())
                {
                    p.Add("b.ErpId IN @pIdList");
                }

                var sql = $"SELECT a.ErpId Id, a.`Name`, a.`Batch`, a.`IncreaseTime`, a.`Number`, Stock, b.ErpId PurchaseId, b.`Purchase`, a.`Order` " +
                          $"FROM `material_purchase_item` a JOIN `material_purchase` b ON a.PurchaseId = b.Id WHERE {(p.Any() ? (p.Join(" AND ") + " AND ") : "")} a.MarkedDelete = 0 AND b.MarkedDelete = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { idList, pIdList });
                result.datas.AddRange(data);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return result;
        }

        // GET: api/MaterialPurchaseItem/?qId=0
        /// <summary>
        /// erp 获取物料入库信息
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="pIds"></param>
        /// <returns></returns>
        [HttpGet("Infos")]
        public async Task<DataResult> GetMaterialPurchaseItemInfos([FromQuery] string ids, string pIds)
        {
            var result = new DataResult();
            var p = new List<string>();
            try
            {
                var idList = ids.IsNullOrEmpty() ? new List<int>() : ids.Split(",").Select(int.Parse);
                var pIdList = pIds.IsNullOrEmpty() ? new List<int>() : pIds.Split(",").Select(int.Parse);
                if (idList.Any())
                {
                    p.Add("a.ErpId IN @idList");
                }

                if (pIdList.Any())
                {
                    p.Add("b.ErpId IN @pIdList");
                }

                var sql = $"SELECT a.ErpId Id, a.`Name`, a.`Batch`, a.`IncreaseTime`, a.`Number`, Stock, b.ErpId PurchaseId, b.`Purchase`, a.`Order` " +
                          $"FROM `material_purchase_item` a JOIN `material_purchase` b ON a.PurchaseId = b.Id WHERE {(p.Any() ? (p.Join(" AND ") + " AND ") : "")} a.MarkedDelete = 0 AND b.MarkedDelete = 0;";
                var data = await ServerConfig.ApiDb.QueryAsync<dynamic>(sql, new { idList, pIdList });
                result.datas.AddRange(data);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return result;
        }

        // GET: api/MaterialPurchaseItem/?qId=0
        /// <summary>
        /// erp 修改物料信息通知
        /// </summary>
        /// <param name="ids">物料id</param>
        /// <param name="pIds">请购单id</param>
        /// <returns></returns>
        [HttpGet("Update")]
        public Result UpdatePurchase([FromQuery] string ids, string pIds)
        {
            try
            {
                var purchases = new List<int>();
                var idList = ids.IsNullOrEmpty() ? new List<int>() : ids.Split(",").Select(int.Parse);
                var pIdList = pIds.IsNullOrEmpty() ? new List<int>() : pIds.Split(",").Select(int.Parse);
                string sql;
                if (idList.Any())
                {
                    sql = "SELECT PurchaseId FROM `material_purchase_item` WHERE ErpId IN @idList AND `MarkedDelete` = 0";
                    purchases.AddRange(ServerConfig.ApiDb.Query<int>(sql, new { idList }));
                }

                if (pIdList.Any())
                {
                    sql = "SELECT Id FROM `material_purchase` WHERE ErpId IN @pIdList AND `MarkedDelete` = 0";
                    purchases.AddRange(ServerConfig.ApiDb.Query<int>(sql, new { pIdList }));
                }
                if (purchases.Any())
                {
                    TimerHelper.ErpPurchaseFunc(purchases);
                }
            }
            catch (Exception e)
            {
                Log.Error($"{ids?.Join() ?? ""}|{pIds?.Join() ?? ""}, {e}");
                return Result.GenError<Result>(Error.Fail);
            }
            return Result.GenError<Result>(Error.Success);
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
        public object IncreaseMaterialPurchaseItem([FromBody] IEnumerable<MaterialPurchaseItem> materialPurchaseItems)
        {
            if (materialPurchaseItems == null)
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }

            materialPurchaseItems = materialPurchaseItems.Where(x => x.Count > 0);
            if (!materialPurchaseItems.Any())
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

            //if (purchases.Any(x => x.State != MaterialPurchaseStateEnum.开始采购 || x.State != MaterialPurchaseStateEnum.仓库到货))
            //{
            //    return Result.GenError<Result>(Error.MaterialPurchaseIncreaseSateError);
            //}

            var oldMaterialPurchaseItems =
                ServerConfig.ApiDb.Query<MaterialPurchaseItem>("SELECT * FROM `material_purchase_item` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = materialPurchaseItems.Select(x => x.Id) });
            if (oldMaterialPurchaseItems.Count() != materialPurchaseItems.Count())
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotExist);
            }
            if (oldMaterialPurchaseItems.Any(x => x.ErpId == 0))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemNotBuy);
            }

            var markedDateTime = DateTime.Now;
            var batch = markedDateTime.ToStrFull().Substring(0, 15);
            foreach (var item in oldMaterialPurchaseItems)
            {
                var materialPurchaseItem = materialPurchaseItems.FirstOrDefault(x => x.Id == item.Id);
                if (materialPurchaseItem != null)
                {
                    if (item.Stock == 0)
                    {
                        item.Batch = batch;
                        item.IncreaseTime = markedDateTime;
                    }

                    item.MarkedDateTime = markedDateTime;
                    item.Stock += materialPurchaseItem.Count;
                    item.Count = materialPurchaseItem.Count;
                }
            }

            #region 入库

            var unknow = "未知";
            foreach (var materialPurchaseItem in oldMaterialPurchaseItems)
            {
                if ((materialPurchaseItem.Class + materialPurchaseItem.Category).IsNullOrEmpty())
                {
                    materialPurchaseItem.Category = unknow;
                }
                if (materialPurchaseItem.Name.IsNullOrEmpty())
                {
                    return Result.GenError<Result>(Error.MaterialNameNotExist);
                }
                if (materialPurchaseItem.Supplier.IsNullOrEmpty())
                {
                    materialPurchaseItem.Supplier = unknow;
                }
                if (materialPurchaseItem.Specification.IsNullOrEmpty())
                {
                    materialPurchaseItem.Specification = unknow;
                }
            }

            var erpBill = oldMaterialPurchaseItems.Select(x => new OpMaterialManagement
            {
                //借用，透传物料单id
                PlanId = x.Id,
                BillId = x.BillId,
                Code = x.Code,
                Category = x.Category.IsNullOrEmpty() ? x.Class : x.Category,
                Name = x.Name,
                Supplier = x.Supplier,
                Specification = x.Specification,
                Site = unknow,
                Price = x.Price,
                Number = x.Count,
                Unit = x.Unit,
                Remark = x.Remark,
                File = x.File,
                FileUrl = x.FileUrl,
                RelatedPerson = purchases.FirstOrDefault(y => y.Id == x.PurchaseId)?.Name ?? "",
                Purpose = $"Erp采购-{purchases.FirstOrDefault(y => y.Id == x.PurchaseId)?.ErpId.ToString() ?? ""}",
            }).ToList();

            if (erpBill.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialPurchaseItemCodeNotEmpty);
            }
            var allBill = ServerConfig.ApiDb.Query<MaterialManagementErp>(
                "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, IFNULL(c.Number, 0) Number, IF (ISNULL(c.Number), 0, 1) Exist,  c.Id MId,  " +
                "a.Id BillId FROM `material_bill` a JOIN (SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a JOIN (SELECT a.*, b.`Name`, " +
                "b.CategoryId, b.Category FROM `material_supplier` a JOIN (SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id) " +
                "b ON a.SupplierId = b.Id) b ON a.SpecificationId = b.Id LEFT JOIN `material_management` c ON a.Id = c.BillId WHERE a.`MarkedDelete` = 0;");

            foreach (var bill in erpBill)
            {
                var b = allBill.FirstOrDefault(x => x.Code.StartsWith(bill.Code)
                                                    && x.Category == bill.Category
                                                    && x.Name == bill.Name
                                                    && x.Supplier == bill.Supplier
                                                    && x.Specification == bill.Specification
                                                    && x.Price == bill.Price);
                if (b == null)
                {
                    continue;
                }

                if (b.BillId == 0)
                {
                    return Result.GenError<Result>(Error.ParamError);
                }
                bill.BillId = b.BillId;
                bill.Code = b.Code;
                bill.SiteId = b.SiteId;
                bill.SpecificationId = b.SpecificationId;
                bill.SupplierId = b.SupplierId;
                bill.NameId = b.NameId;
                bill.CategoryId = b.CategoryId;
            }

            var createUserId = Request.GetIdentityInformation();
            if (erpBill.Any(x => x.BillId == 0))
            {
                #region 新货品
                var materialBills = erpBill.Where(x => x.SiteId == 0 || x.SpecificationId == 0 || x.SupplierId == 0 || x.NameId == 0 || x.NameId == 0 || x.CategoryId == 0 || x.BillId == 0);
                if (materialBills.Any())
                {
                    #region 新位置
                    var notExistSite = materialBills.Where(x => x.SiteId == 0);
                    var allNewSite = notExistSite.GroupBy(x => x.Site).Select(y => y.Key);
                    if (allNewSite.Any())
                    {
                        var sameStr =
                            ServerConfig.ApiDb.Query<MaterialSite>("SELECT Id, Site FROM `material_site` WHERE Site IN @Site AND `MarkedDelete` = 0;", new { Site = allNewSite });
                        if (sameStr.Any())
                        {
                            foreach (var same in sameStr)
                            {
                                foreach (var bill in materialBills.Where(x => x.Site == same.Site))
                                {
                                    bill.SiteId = same.Id;
                                }
                            }
                        }
                        if (allNewSite.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                            "INSERT INTO material_site (`CreateUserId`, `Site`) VALUES (@CreateUserId, @Site);",
                            allNewSite.Select(x => new
                            {
                                CreateUserId = createUserId,
                                Site = x
                            }));
                            var siteIds =
                                ServerConfig.ApiDb.Query<MaterialSite>("SELECT Id, Site FROM `material_site` WHERE Site IN @Site AND `MarkedDelete` = 0;", new { Site = allNewSite });
                            foreach (var bill in materialBills)
                            {
                                var site = siteIds.FirstOrDefault(x => x.Site == bill.Site);
                                if (site != null)
                                {
                                    bill.SiteId = site.Id;
                                }
                            }
                        }
                    }
                    #endregion

                    #region 新类别
                    var notExistCategory = materialBills.Where(x => x.CategoryId == 0);
                    var allNewCategory = notExistCategory.GroupBy(x => x.Category).Select(y => y.Key);
                    if (allNewCategory.Any())
                    {
                        var sameStr =
                            ServerConfig.ApiDb.Query<MaterialCategory>("SELECT Id, Category FROM `material_category` WHERE Category IN @Category AND `MarkedDelete` = 0;", new { Category = allNewCategory });
                        if (sameStr.Any())
                        {
                            foreach (var same in sameStr)
                            {
                                foreach (var bill in materialBills.Where(x => x.Category == same.Category))
                                {
                                    bill.CategoryId = same.Id;
                                }
                            }
                        }

                        if (allNewCategory.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO material_category (`CreateUserId`, `Category`) VALUES (@CreateUserId, @Category);",
                                allNewCategory.Select(x => new
                                {
                                    CreateUserId = createUserId,
                                    Category = x
                                }));
                            var categoryIds =
                                ServerConfig.ApiDb.Query<MaterialCategory>("SELECT * FROM `material_category` WHERE Category IN @Category AND `MarkedDelete` = 0;", new { Category = allNewCategory });
                            foreach (var bill in materialBills)
                            {
                                var category = categoryIds.FirstOrDefault(x => x.Category == bill.Category);
                                if (category != null)
                                {
                                    bill.CategoryId = category.Id;
                                }
                            }
                        }
                    }
                    #endregion

                    #region 新名称
                    var notExistName = materialBills.Where(x => x.NameId == 0);
                    var allNewName = notExistName.GroupBy(x => new { x.CategoryId, x.Name }).Select(y => y.Key);
                    if (allNewName.Any())
                    {
                        var sameStr =
                            ServerConfig.ApiDb.Query<MaterialName>("SELECT Id, CategoryId, Name FROM `material_name` WHERE Name IN @Name AND CategoryId IN @CategoryId AND `MarkedDelete` = 0;",
                                new { Name = allNewName.Select(x => x.Name), CategoryId = allNewName.Select(x => x.CategoryId) });
                        if (sameStr.Any())
                        {
                            foreach (var same in sameStr)
                            {
                                foreach (var bill in materialBills.Where(x => x.CategoryId == same.CategoryId && x.Name == same.Name))
                                {
                                    bill.NameId = same.Id;
                                }
                            }
                        }

                        if (allNewName.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO material_name (`CreateUserId`, `CategoryId`, `Name`) " +
                                "VALUES (@CreateUserId, @CategoryId, @Name);",
                                allNewName.Select(x => new
                                {
                                    CreateUserId = createUserId,
                                    CategoryId = x.CategoryId,
                                    Name = x.Name,
                                }));

                            var nameIds =
                                ServerConfig.ApiDb.Query<MaterialName>("SELECT Id, CategoryId, Name FROM `material_name` WHERE Name IN @Name AND CategoryId IN @CategoryId AND `MarkedDelete` = 0;",
                                    new { Name = allNewName.Select(x => x.Name), CategoryId = allNewName.Select(x => x.CategoryId) });
                            foreach (var bill in materialBills)
                            {
                                var name = nameIds.FirstOrDefault(x => x.CategoryId == bill.CategoryId && x.Name == bill.Name);
                                if (name != null)
                                {
                                    bill.NameId = name.Id;
                                }
                            }
                        }
                    }

                    #endregion

                    #region 新供应商
                    var notExistSupplier = materialBills.Where(x => x.SupplierId == 0);
                    var allNewSupplier = notExistSupplier.GroupBy(x => new { x.NameId, x.Supplier }).Select(y => y.Key);
                    if (allNewSupplier.Any())
                    {
                        var sameStr =
                            ServerConfig.ApiDb.Query<MaterialSupplier>("SELECT Id, NameId, Supplier FROM `material_supplier` WHERE Supplier IN @Supplier AND NameId IN @NameId AND `MarkedDelete` = 0;",
                                new { Supplier = allNewSupplier.Select(x => x.Supplier), NameId = allNewSupplier.Select(x => x.NameId) });
                        if (sameStr.Any())
                        {
                            foreach (var same in sameStr)
                            {
                                foreach (var bill in materialBills.Where(x => x.NameId == same.NameId && x.Supplier == same.Supplier))
                                {
                                    bill.SupplierId = same.Id;
                                }
                            }
                        }

                        if (allNewSupplier.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO material_supplier (`CreateUserId`, `NameId`, `Supplier`) " +
                                "VALUES (@CreateUserId, @NameId, @Supplier);",
                                allNewSupplier.Select(x => new
                                {
                                    CreateUserId = createUserId,
                                    NameId = x.NameId,
                                    Supplier = x.Supplier,
                                }));

                            var supplierIds =
                                ServerConfig.ApiDb.Query<MaterialSupplier>("SELECT Id, NameId, Supplier FROM `material_supplier` WHERE Supplier IN @Supplier AND NameId IN @NameId AND `MarkedDelete` = 0;",
                                    new { Supplier = allNewSupplier.Select(x => x.Supplier), NameId = allNewSupplier.Select(x => x.NameId) });
                            foreach (var bill in materialBills)
                            {
                                var supplier = supplierIds.FirstOrDefault(x => x.NameId == bill.NameId && x.Supplier == bill.Supplier);
                                if (supplier != null)
                                {
                                    bill.SupplierId = supplier.Id;
                                }
                            }
                        }
                    }

                    #endregion

                    #region 新规格
                    var notExistSpecification = materialBills.Where(x => x.SpecificationId == 0);
                    var allNewSpecification = notExistSpecification.GroupBy(x => new { x.SupplierId, x.Specification }).Select(y => y.Key);
                    if (allNewSpecification.Any())
                    {
                        var sameStr =
                            ServerConfig.ApiDb.Query<MaterialSpecification>("SELECT Id, SupplierId, Specification FROM `material_specification` WHERE Specification IN @Specification AND SupplierId IN @SupplierId AND `MarkedDelete` = 0;",
                                new { Specification = allNewSpecification.Select(x => x.Specification), SupplierId = allNewSpecification.Select(x => x.SupplierId) });
                        if (sameStr.Any())
                        {
                            foreach (var same in sameStr)
                            {
                                foreach (var bill in materialBills.Where(x => x.SupplierId == same.SupplierId && x.Specification == same.Specification))
                                {
                                    bill.SpecificationId = same.Id;
                                }
                            }
                        }

                        if (allNewSpecification.Any())
                        {
                            ServerConfig.ApiDb.Execute(
                                "INSERT INTO material_specification (`CreateUserId`, `SupplierId`, `Specification`) " +
                                "VALUES (@CreateUserId, @SupplierId, @Specification);",
                                allNewSpecification.Select(x => new
                                {
                                    CreateUserId = createUserId,
                                    SupplierId = x.SupplierId,
                                    Specification = x.Specification,
                                }));

                            var specificationIds =
                                ServerConfig.ApiDb.Query<MaterialSpecification>("SELECT * FROM `material_specification` WHERE Specification IN @Specification AND SupplierId IN @SupplierId AND `MarkedDelete` = 0;",
                                new { Specification = allNewSpecification.Select(x => x.Specification), SupplierId = allNewSpecification.Select(x => x.SupplierId) });

                            foreach (var bill in materialBills)
                            {
                                var specification = specificationIds.FirstOrDefault(x => x.SupplierId == bill.SupplierId && x.Specification == bill.Specification);
                                if (specification != null)
                                {
                                    bill.SpecificationId = specification.Id;
                                }
                            }
                        }
                    }

                    #endregion

                    #region 新编号
                    var notExistCodes = materialBills.GroupBy(x => new { x.SpecificationId, x.Price, x.SiteId });
                    var newCodes = new List<string>();
                    foreach (var notExistCode in notExistCodes)
                    {
                        var code = notExistCode.First().Code;
                        var i = 0;
                        var newCode = code;
                        while (true)
                        {
                            if (allBill.Any(x => x.Code == newCode) || newCodes.Any(x => x == newCode))
                            {
                                newCode = code + "-" + i++;
                            }
                            else
                            {
                                newCodes.Add(newCode);
                                foreach (var b in materialBills.Where(x => x.SpecificationId == notExistCode.Key.SpecificationId
                                                                          && x.Price == notExistCode.Key.Price
                                                                          && x.SiteId == notExistCode.Key.SiteId))
                                {
                                    b.Code = newCode;
                                }
                                break;
                            }
                        }
                    }
                    #endregion
                    //if (materialBills.Any(x => x.BillId == 0))
                    {
                        foreach (var materialBill in materialBills)
                        {
                            materialBill.CreateUserId = createUserId;
                            materialBill.Remark = materialBill.Remark ?? "";
                            materialBill.Images = materialBill.Images ?? "[]";
                        }
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO material_bill (`CreateUserId`, `SpecificationId`, `SiteId`, `Code`, `Unit`, `Price`, `Stock`, `Images`, `Remark`) " +
                            "VALUES (@CreateUserId, @SpecificationId, @SiteId, @Code, @Unit, @Price, @Stock, @Images, @Remark);",
                            materialBills);
                    }
                }

                var billId_0 = erpBill.Where(x => x.BillId == 0);
                if (billId_0.Any())
                {
                    allBill = ServerConfig.ApiDb.Query<MaterialManagementErp>(
                        "SELECT a.*, IFNULL(b.Number, 0) Number, IF (ISNULL(b.Number), 0, 1) Exist,  b.Id MId, b.BillId FROM `material_bill` a " +
                        "LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.`Code` IN @Code AND a.MarkedDelete = 0;",
                            new { Code = erpBill.Select(x => x.Code) });
                    foreach (var bill in billId_0)
                    {
                        var materialBill = allBill.FirstOrDefault(x => x.Code == bill.Code);
                        if (materialBill != null)
                        {
                            bill.BillId = materialBill.Id;
                        }
                    }
                }
                #endregion
            }

            var logs = new List<MaterialLog>();
            #region 更新
            var existBill = allBill.Where(x => erpBill.Any(y => y.BillId == x.Id) && x.Exist).Distinct().ToList();
            if (existBill.Any())
            {
                foreach (var bill in existBill)
                {
                    bill.OldNumber = bill.Number;
                    bill.Number += erpBill.Where(x => x.BillId == bill.Id).Sum(y => y.Number);
                    bill.InTime = markedDateTime;
                }
                ServerConfig.ApiDb.Execute("UPDATE material_management SET `InTime` = @InTime, `Number` = @Number WHERE `Id` = @MId;", existBill);
            }
            #endregion

            #region 添加
            //var addBill = allBill.Where(x => erpBill.Any(y => y.BillId == x.Id) && !x.Exist).Distinct().ToList();
            var addBill = erpBill.Where(x => existBill.All(y => y.Id != x.BillId));
            if (addBill.Any())
            {
                var billIds = addBill.GroupBy(y => y.BillId);
                ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `Number`) VALUES (@BillId, @InTime, @Number);", billIds.Select(
                x => new
                {
                    BillId = x.Key,
                    InTime = markedDateTime,
                    Number = addBill.Where(z => z.BillId == x.Key).Sum(a => a.Number)
                }));
            }

            #endregion

            //allBill = ServerConfig.ApiDb.Query<MaterialManagementErp>(
            //    "SELECT * FROM `material_bill` WHERE MarkedDelete = 0 AND SpecificationId IN @SpecificationId AND SiteId IN @SiteId AND Price IN @Price;",
            //    new
            //    {
            //        SpecificationId = erpBill.Where(x => x.BillId == 0).Select(y => y.SpecificationId),
            //        SiteId = erpBill.Where(x => x.BillId == 0).Select(y => y.SiteId),
            //        Price = erpBill.Where(x => x.BillId == 0).Select(y => y.Price),
            //    });

            foreach (var eBill in erpBill)
            {
                var oldMaterialPurchaseItem = oldMaterialPurchaseItems.FirstOrDefault(x => x.Id == eBill.PlanId);
                if (oldMaterialPurchaseItem != null)
                {
                    oldMaterialPurchaseItem.BillId = eBill.BillId;
                    oldMaterialPurchaseItem.ThisCode = eBill.Code;
                }
            }
            logs.AddRange(erpBill.Select(x => new MaterialLog
            {
                Time = markedDateTime,
                BillId = x.BillId,
                Code = allBill.First(y => y.Id == x.BillId).Code,
                Type = 1,
                Purpose = x.Purpose ?? $"Erp采购",
                Number = x.Number,
                OldNumber = existBill.FirstOrDefault(y => y.BillId == x.BillId)?.OldNumber ?? 0,
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
                HMaterialHelper.InsertLog(logs.Select(x =>
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
            #endregion

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_purchase_item` SET `MarkedDateTime` = @MarkedDateTime, `Batch` = @Batch, `IncreaseTime` = @IncreaseTime, " +
                "`Stock` = @Stock, `BillId` = @BillId, `ThisCode` = @ThisCode WHERE `Id` = @Id;", oldMaterialPurchaseItems);
            //TimerHelper.MaterialRecovery(true);
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