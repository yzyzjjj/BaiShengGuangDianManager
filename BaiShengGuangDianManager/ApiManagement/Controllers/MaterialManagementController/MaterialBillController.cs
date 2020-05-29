using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 货品管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialBillController : ControllerBase
    {
        // GET: api/MaterialBill?categoryId=0&nameId=0&supplierId=0&specificationId=0&qId=0&qId=0&siteId
        [HttpGet]
        public DataResult GetMaterialBill([FromQuery] int categoryId, int nameId, int supplierId, int specificationId, int qId, int siteId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && nameId == 0 && supplierId == 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    $"JOIN `material_site` c ON a.SiteId = c.Id WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (nameId != 0 && supplierId == 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id WHERE a.NameId = @nameId ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    $"JOIN `material_site` c ON a.SiteId = c.Id WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (supplierId != 0 && specificationId == 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id WHERE a.SupplierId = @supplierId ) b ON a.SpecificationId = b.Id " +
                    $"JOIN `material_site` c ON a.SiteId = c.Id WHERE a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else if (specificationId != 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    $"JOIN `material_site` c ON a.SiteId = c.Id WHERE a.SpecificationId = @specificationId AND a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }
            else
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.SupplierId, b.Supplier, b.Specification, c.Site FROM `material_bill` a " +
                    "JOIN ( SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id ) b ON a.SpecificationId = b.Id " +
                    $"JOIN `material_site` c ON a.SiteId = c.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0{(siteId == 0 ? "" : " AND a.SiteId = @siteId")};";
            }

            var data = ServerConfig.ApiDb.Query<MaterialBillDetail>(sql,
                new { categoryId, nameId, supplierId, specificationId, qId, siteId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialBillNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialBill
        [HttpPut]
        public Result PutMaterialBill([FromBody] IEnumerable<MaterialBill> materialBills)
        {
            if (materialBills == null)
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            if (materialBills.Any(x => x.UpdateImage))
            {
                if (materialBills.Count() != 1)
                {
                    return Result.GenError<Result>(Error.ParamError);
                }
                var markedDateTime = DateTime.Now;
                try
                {
                    foreach (var materialBill in materialBills)
                    {
                        var imageList = JsonConvert.DeserializeObject<IEnumerable<string>>(materialBill.Images);
                        materialBill.Images = imageList.ToJSON();
                        materialBill.MarkedDateTime = markedDateTime;
                    }

                    ServerConfig.ApiDb.Execute(
                        "UPDATE material_bill SET `MarkedDateTime` = @MarkedDateTime, `Images` = @Images WHERE `Id` = @Id;", materialBills);
                }
                catch (Exception)
                {
                    return Result.GenError<Result>(Error.ParamError);
                }
            }
            else
            {
                if (materialBills.Any(x => x.Code.IsNullOrEmpty()))
                {
                    return Result.GenError<Result>(Error.MaterialBillNotEmpty);
                }

                var materialSpecification = materialBills.GroupBy(x => x.SpecificationId).Select(y => y.Key);
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_specification` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialSpecification }).FirstOrDefault();
                if (cnt != materialSpecification.Count())
                {
                    return Result.GenError<Result>(Error.MaterialSpecificationNotExist);
                }

                var materialSite = materialBills.GroupBy(x => x.SiteId).Select(y => y.Key);
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialSite }).FirstOrDefault();
                if (cnt != materialSite.Count())
                {
                    return Result.GenError<Result>(Error.MaterialSiteNotExist);
                }

                var ids = materialBills.Select(x => x.Id);
                var uMaterialBills =
                    ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM `material_bill` WHERE Id IN @id AND `MarkedDelete` = 0;",
                        new { id = ids });
                if (uMaterialBills.Count() != materialBills.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillNotExist);
                }
                foreach (var materialBill in materialBills)
                {
                    uMaterialBills.First(x => x.Id == materialBill.Id).SpecificationId = materialBill.SpecificationId;
                    uMaterialBills.First(x => x.Id == materialBill.Id).SiteId = materialBill.SiteId;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Code = materialBill.Code;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Unit = materialBill.Unit;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Price = materialBill.Price;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Stock = materialBill.Stock;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Images = materialBill.Images;
                    uMaterialBills.First(x => x.Id == materialBill.Id).Remark = materialBill.Remark;
                }

                if (uMaterialBills.GroupBy(x => new { x.SpecificationId, x.SiteId, x.Price }).Select(y => y.Key).Count() != uMaterialBills.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillDuplicate);
                }

                if (uMaterialBills.GroupBy(x => x.Code).Select(y => y.Key).Count() != uMaterialBills.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillDuplicate);
                }

                var mBs =
                    ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM `material_bill` WHERE Id NOT IN @id AND SpecificationId IN @specificationId AND SiteId IN @siteId AND `MarkedDelete` = 0;",
                        new { id = ids, specificationId = materialSpecification, siteId = materialSite }).ToList();

                //foreach (var materialCode in materialSpecification)
                //{
                //    var mns = mBs.Where(x => x.SpecificationId == materialCode);
                //    var uMns = uMaterialBills.Where(x => x.SpecificationId == materialCode);
                //    foreach (var materialBill in uMns)
                //    {
                //        if (!mns.Any(x => x.Id == materialBill.Id))
                //        {
                //            mBs.Add(materialBill);
                //        }
                //    }
                //}

                foreach (var materialCode in uMaterialBills)
                {
                    if (mBs.All(x => x.Id != materialCode.Id))
                    {
                        mBs.Add(materialCode);
                    }
                }

                if (mBs.GroupBy(x => new { x.SpecificationId, x.SiteId, x.Price }).Select(y => y.Key).Count() != mBs.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillIsExist);
                }

                if (mBs.GroupBy(x => x.Code).Select(y => y.Key).Count() != mBs.Count())
                {
                    return Result.GenError<Result>(Error.MaterialBillIsExist);
                }

                var markedDateTime = DateTime.Now;
                foreach (var materialBill in materialBills)
                {
                    materialBill.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE material_bill SET `MarkedDateTime` = @MarkedDateTime, `SpecificationId` = @SpecificationId, `SiteId` = @SiteId, `Code` = @Code, `Unit` = @Unit, `Price` = @Price, `Stock` = @Stock, `Images` = @Images, `Remark` = @Remark WHERE `Id` = @Id;", materialBills);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialBill
        [HttpPost]
        public DataResult PostMaterialBill([FromBody] IEnumerable<MaterialBillDetail> materialBills)
        {
            if (materialBills == null || !materialBills.Any())
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }

            if (materialBills.Any(x => x.SiteId == 0 && x.Site.IsNullOrEmpty()) ||
                materialBills.Any(x => x.SpecificationId == 0 && x.Specification.IsNullOrEmpty()) ||
                materialBills.Any(x => x.SupplierId == 0 && x.Supplier.IsNullOrEmpty()) ||
                materialBills.Any(x => x.NameId == 0 && x.Name.IsNullOrEmpty()) ||
                materialBills.Any(x => x.CategoryId == 0 && x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }

            var result = new DataResult();
            var eCodes = materialBills.GroupBy(x => x.Code).Where(y => y.GroupBy(z => new { z.SpecificationId, z.SiteId, z.Price }).Count() > 1).Select(c => c.Key);
            if (eCodes.Any())
            {
                result.errno = Error.MaterialBillIsExist;
                result.datas.AddRange(eCodes);
                return result;
            }
            var codes = materialBills.GroupBy(x => x.Code).Select(y => y.Key);
            var sameCode = materialBills.GroupBy(x => x.Code).Where(y => y.Count() > 1).Select(z => z.Key);
            if (sameCode.Any())
            {
                result.errno = Error.MaterialBillDuplicate;
                result.datas.AddRange(sameCode);
                return result;
            }

            sameCode = ServerConfig.ApiDb.Query<string>("SELECT Code FROM `material_bill` WHERE Code IN @Code AND MarkedDelete = 0;", new { Code = codes });
            if (sameCode.Any())
            {
                result.errno = Error.MaterialBillIsExist;
                result.datas.AddRange(sameCode);
                return result;
            }

            #region 规格 位置 存在
            var existBills = materialBills.Where(x => x.SpecificationId != 0 && x.SiteId != 0);
            if (existBills.Any())
            {
                var specificationIds = existBills.GroupBy(x => x.SpecificationId).Select(y => y.Key);
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_specification` WHERE Id IN @Id AND `MarkedDelete` = 0;",
                        new { Id = specificationIds }).FirstOrDefault();
                if (cnt != specificationIds.Count())
                {
                    return Result.GenError<DataResult>(Error.MaterialSpecificationNotExist);
                }
                var siteIds = existBills.GroupBy(x => x.SiteId).Select(y => y.Key);
                cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @Id AND `MarkedDelete` = 0;",
                        new { Id = siteIds }).FirstOrDefault();
                if (cnt != siteIds.Count())
                {
                    return Result.GenError<DataResult>(Error.MaterialSiteNotExist);
                }

                var sameSS = existBills.GroupBy(x => new { x.SpecificationId, x.SiteId, x.Price }).Where(y => y.Count() > 1).Select(z => z.Key);
                if (sameSS.Any())
                {
                    result.errno = Error.MaterialBillSpecificationPriceSiteDuplicate;
                    result.datas.AddRange(sameSS);
                    return result;
                }

                var prices = existBills.GroupBy(x => x.Price).Select(y => y.Key);
                var sameSSOld = ServerConfig.ApiDb.Query<dynamic>("SELECT SpecificationId, SiteId FROM `material_bill` " +
                                                                  "WHERE SpecificationId IN @SpecificationId AND SiteId IN @SiteId AND Price IN @Price AND MarkedDelete = 0;",
                    new { SpecificationId = specificationIds, SiteId = siteIds, Price = prices });
                if (sameSSOld.Any())
                {
                    result.errno = Error.MaterialBillSpecificationPriceSiteIsExist;
                    result.datas.AddRange(sameSSOld);
                    return result;
                }
            }
            #endregion

            #region 新位置
            var notExistSite = materialBills.Where(x => x.SiteId == 0);
            var newSite = notExistSite.GroupBy(x => x.Site).Select(y => y.Key);
            if (newSite.Any())
            {
                var sameStr =
                    ServerConfig.ApiDb.Query<string>("SELECT Site FROM `material_site` WHERE Site IN @Site AND `MarkedDelete` = 0;", new { Site = newSite });
                if (sameStr.Any())
                {
                    result.errno = Error.MaterialSiteIsExist;
                    result.datas.AddRange(sameStr);
                    return result;
                }
            }
            #endregion

            #region 新类别
            var notExistCategory = materialBills.Where(x => x.CategoryId == 0);
            var newCategory = notExistCategory.GroupBy(x => x.Category).Select(y => y.Key);
            if (newCategory.Any())
            {
                var sameStr =
                    ServerConfig.ApiDb.Query<string>("SELECT Category FROM `material_category` WHERE Category IN @Category AND `MarkedDelete` = 0;", new { Category = newCategory });
                if (sameStr.Any())
                {
                    result.errno = Error.MaterialCategoryIsExist;
                    result.datas.AddRange(sameStr);
                    return result;
                }
            }
            #endregion

            #region 新名称
            var notExistName = materialBills.Where(x => x.NameId == 0);
            var categoryExist = notExistName.Where(x => x.CategoryId != 0);
            if (categoryExist.Any())
            {
                var sameStr =
                    ServerConfig.ApiDb.Query<dynamic>("SELECT CategoryId, Name FROM `material_name` WHERE Name IN @Name AND CategoryId IN @CategoryId AND `MarkedDelete` = 0;",
                        new { Name = categoryExist.Select(x => x.Name), CategoryId = categoryExist.Select(x => x.CategoryId) });
                if (sameStr.Any())
                {
                    result.errno = Error.MaterialNameIsExist;
                    result.datas.AddRange(sameStr);
                    return result;
                }
            }
            //var categoryNotExist = notExistName.Where(x => x.CategoryId == 0);
            //if (categoryNotExist.Any())
            //{
            //    var g = categoryNotExist.GroupBy(x => new { x.Category, x.Name });
            //    if (g.Any(y => y.Count() > 1))
            //    {
            //        result.errno = Error.MaterialNameDuplicate;
            //        result.datas.AddRange(g.Where(x => x.Count() > 1).Select(y => y.Key));
            //        return result;
            //    }
            //}
            #endregion

            #region 新供应商
            var notExistSupplier = materialBills.Where(x => x.SupplierId == 0);
            var nameExist = notExistSupplier.Where(x => x.NameId != 0);
            if (nameExist.Any())
            {
                var sameStr =
                    ServerConfig.ApiDb.Query<dynamic>("SELECT NameId, Supplier FROM `material_supplier` WHERE Supplier IN @Supplier AND NameId IN @NameId AND `MarkedDelete` = 0;",
                        new { Supplier = nameExist.Select(x => x.Supplier), NameId = nameExist.Select(x => x.NameId) });
                if (sameStr.Any())
                {
                    result.errno = Error.MaterialSupplierIsExist;
                    result.datas.AddRange(sameStr);
                    return result;
                }
            }
            //var nameNotExist = notExistSupplier.Where(x => x.NameId == 0);
            //if (nameNotExist.Any())
            //{
            //    var g = nameNotExist.GroupBy(x => new { x.Name, x.Supplier });
            //    if (g.Any(y => y.Count() > 1))
            //    {
            //        result.errno = Error.MaterialSupplierDuplicate;
            //        result.datas.AddRange(g.Where(x => x.Count() > 1).Select(y => y.Key));
            //        return result;
            //    }
            //}
            #endregion

            #region 新规格
            var notExistSpecification = materialBills.Where(x => x.SpecificationId == 0);
            var supplierExist = notExistSpecification.Where(x => x.SupplierId != 0);
            if (supplierExist.Any())
            {
                var sameStr =
                    ServerConfig.ApiDb.Query<dynamic>("SELECT SupplierId, Specification FROM `material_specification` WHERE Specification IN @Specification AND SupplierId IN @SupplierId AND `MarkedDelete` = 0;",
                        new { Specification = supplierExist.Select(x => x.Specification), SupplierId = supplierExist.Select(x => x.SupplierId) });
                if (sameStr.Any())
                {
                    result.errno = Error.MaterialSpecificationIsExist;
                    result.datas.AddRange(sameStr);
                    return result;
                }
            }
            //var supplierNotExist = notExistSpecification.Where(x => x.SupplierId == 0);
            //if (supplierNotExist.Any())
            //{
            //    var g = supplierNotExist.GroupBy(x => new { x.Supplier, x.Specification });
            //    if (g.Any(y => y.Count() > 1))
            //    {
            //        result.errno = Error.MaterialSpecificationDuplicate;
            //        result.datas.AddRange(g.Where(x => x.Count() > 1).Select(y => y.Key));
            //        return result;
            //    }
            //}
            #endregion

            #region 新单价
            if (materialBills.GroupBy(x => new { x.Category, x.Name, x.Supplier, x.Specification, x.Price, x.Site }).Any(y => y.Count() > 1))
            {
                result.errno = Error.MaterialBillSpecificationPriceSiteDuplicate;
                return result;
            }

            #endregion

            var createUserId = Request.GetIdentityInformation();
            if (newSite.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO material_site (`CreateUserId`, `Site`) VALUES (@CreateUserId, @Site);",
                    newSite.Select(x => new
                    {
                        CreateUserId = createUserId,
                        Site = x
                    }));
                var siteIds =
                    ServerConfig.ApiDb.Query<MaterialSite>("SELECT * FROM `material_site` WHERE Site IN @Site AND `MarkedDelete` = 0;", new { Site = newSite });
                foreach (var bill in materialBills)
                {
                    var site = siteIds.FirstOrDefault(x => x.Site == bill.Site);
                    if (site != null)
                    {
                        bill.SiteId = site.Id;
                    }
                }
            }

            if (newCategory.Any())
            {
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO material_category (`CreateUserId`, `Category`) VALUES (@CreateUserId, @Category);",
                    newCategory.Select(x => new
                    {
                        CreateUserId = createUserId,
                        Category = x
                    }));
                var categoryIds =
                    ServerConfig.ApiDb.Query<MaterialCategory>("SELECT * FROM `material_category` WHERE Category IN @Category AND `MarkedDelete` = 0;", new { Category = newCategory });
                foreach (var bill in materialBills)
                {
                    var category = categoryIds.FirstOrDefault(x => x.Category == bill.Category);
                    if (category != null)
                    {
                        bill.CategoryId = category.Id;
                    }
                }
            }

            if (notExistName.Any())
            {
                var g = notExistName.GroupBy(x => new { x.CategoryId, x.Name });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO material_name (`CreateUserId`, `CategoryId`, `Name`) " +
                    "VALUES (@CreateUserId, @CategoryId, @Name);",
                    g.Select(x => new
                    {
                        CreateUserId = createUserId,
                        CategoryId = x.Key.CategoryId,
                        Name = x.Key.Name,
                    }));

                var nameIds =
                    ServerConfig.ApiDb.Query<MaterialName>("SELECT * FROM `material_name` WHERE Name IN @Name AND CategoryId IN @CategoryId AND `MarkedDelete` = 0;",
                        new { Name = g.Select(x => x.Key.Name), CategoryId = g.Select(x => x.Key.CategoryId) });
                foreach (var bill in materialBills)
                {
                    var name = nameIds.FirstOrDefault(x => x.CategoryId == bill.CategoryId && x.Name == bill.Name);
                    if (name != null)
                    {
                        bill.NameId = name.Id;
                    }
                }
            }

            if (notExistSupplier.Any())
            {
                var g = notExistSupplier.GroupBy(x => new { x.NameId, x.Supplier });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO material_supplier (`CreateUserId`, `NameId`, `Supplier`) " +
                    "VALUES (@CreateUserId, @NameId, @Supplier);",
                    g.Select(x => new
                    {
                        CreateUserId = createUserId,
                        NameId = x.Key.NameId,
                        Supplier = x.Key.Supplier,
                    }));

                var supplierIds =
                    ServerConfig.ApiDb.Query<MaterialSupplier>("SELECT * FROM `material_supplier` WHERE Supplier IN @Supplier AND NameId IN @NameId AND `MarkedDelete` = 0;",
                        new { Supplier = g.Select(x => x.Key.Supplier), NameId = g.Select(x => x.Key.NameId) });
                foreach (var bill in materialBills)
                {
                    var supplier = supplierIds.FirstOrDefault(x => x.NameId == bill.NameId && x.Supplier == bill.Supplier);
                    if (supplier != null)
                    {
                        bill.SupplierId = supplier.Id;
                    }
                }
            }

            if (notExistSpecification.Any())
            {
                var g = notExistSpecification.GroupBy(x => new { x.SupplierId, x.Specification });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO material_specification (`CreateUserId`, `SupplierId`, `Specification`) " +
                    "VALUES (@CreateUserId, @SupplierId, @Specification);",
                    g.Select(x => new
                    {
                        CreateUserId = createUserId,
                        SupplierId = x.Key.SupplierId,
                        Specification = x.Key.Specification,
                    }));

                var specificationIds =
                    ServerConfig.ApiDb.Query<MaterialSpecification>("SELECT * FROM `material_specification` WHERE Specification IN @Specification AND SupplierId IN @SupplierId AND `MarkedDelete` = 0;",
                        new { Specification = g.Select(x => x.Key.Specification), SupplierId = g.Select(x => x.Key.SupplierId) });
                foreach (var bill in materialBills)
                {
                    var specification = specificationIds.FirstOrDefault(x => x.SupplierId == bill.SupplierId && x.Specification == bill.Specification);
                    if (specification != null)
                    {
                        bill.SpecificationId = specification.Id;
                    }
                }
            }

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

            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/MaterialBill
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialBill([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_bill` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_bill` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}