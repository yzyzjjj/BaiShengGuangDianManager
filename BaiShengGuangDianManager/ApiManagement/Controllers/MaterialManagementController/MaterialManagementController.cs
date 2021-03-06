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
    /// 物料管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialManagementController : ControllerBase
    {
        // GET: api/MaterialManagement/Choose?categoryId=0&nameId=0&supplierId=0&specificationId=0&qId=0&siteId
        [HttpGet("Choose")]
        public MaterialChooseResult GetMaterialManagementChoose([FromQuery] int categoryId, int nameId, int supplierId, int specificationId, int siteId)
        {
            var result = new MaterialChooseResult();
            if (categoryId != 0 && nameId == 0 && supplierId == 0 && specificationId == 0)
            {
                //if (result.Categories.Any())
                //{
                result.Names.AddRange(
                ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0 AND CategoryId = @Id;", new { Id = categoryId }));
                //}
                if (result.Names.Any())
                {
                    result.Suppliers.AddRange(
                    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND NameId IN @Id;", new { Id = result.Names.Select(x => x.Id) }));
                }

                if (result.Suppliers.Any())
                {
                    result.Specifications.AddRange(
                    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId IN @Id;", new { Id = result.Suppliers.Select(x => x.Id) }));
                }
            }
            else if (nameId != 0 && supplierId == 0 && specificationId == 0)
            {
                //if (result.Names.Any())
                //{
                result.Suppliers.AddRange(
                ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND NameId = @Id;", new { Id = nameId }));
                //}

                if (result.Suppliers.Any())
                {
                    result.Specifications.AddRange(
                    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId IN @Id;", new { Id = result.Suppliers.Select(x => x.Id) }));
                }
            }
            else if (supplierId != 0 && specificationId == 0)
            {
                //if (result.Suppliers.Any())
                //{
                result.Specifications.AddRange(
                ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId = @Id;", new { Id = supplierId }));
                //}
            }
            else if (specificationId != 0)
            {

            }
            else
            {
                result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
                result.Names.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0;"));
                result.Suppliers.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0;"));
                result.Specifications.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0;"));
            }
            if (siteId != -1)
            {
                result.Sites.AddRange(
                ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, `Site` FROM `material_site` WHERE `MarkedDelete` = 0{(siteId == 0 ? "" : " AND Id = @siteId")};", new { siteId }));
            }

            return result;
        }
        //// GET: api/MaterialManagement/Choose?categoryId=0&nameId=0&supplierId=0&specificationId=0&qId=0&siteId
        //[HttpGet("Choose")]
        //public MaterialChooseResult GetMaterialManagementChoose([FromQuery] int categoryId, int nameId, int supplierId, int specificationId, int siteId)
        //{
        //    var result = new MaterialChooseResult();
        //    if (categoryId != 0 && nameId == 0 && supplierId == 0 && specificationId == 0)
        //    {
        //        //result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
        //        //result.Categories.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = categoryId }));
        //        if (result.Categories.Any())
        //        {
        //            result.Names.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0 AND CategoryId IN @Id;", new { Id = result.Categories.Select(x => x.Id) }));
        //        }
        //        if (result.Names.Any())
        //        {
        //            result.Suppliers.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND NameId IN @Id;", new { Id = result.Names.Select(x => x.Id) }));
        //        }

        //        if (result.Suppliers.Any())
        //        {
        //            result.Specifications.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId IN @Id;", new { Id = result.Suppliers.Select(x => x.Id) }));
        //        }
        //    }
        //    else if (nameId != 0 && supplierId == 0 && specificationId == 0)
        //    {
        //        //result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
        //        //result.Names.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0;"));
        //        //result.Names.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = nameId }));
        //        if (result.Names.Any())
        //        {
        //            result.Suppliers.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND NameId IN @Id;", new { Id = result.Names.Select(x => x.Id) }));
        //        }

        //        if (result.Suppliers.Any())
        //        {
        //            result.Specifications.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId IN @Id;", new { Id = result.Suppliers.Select(x => x.Id) }));
        //        }

        //        //if (result.Names.Any())
        //        //{
        //        //    result.Categories.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Names.FirstOrDefault()?.CategoryId ?? 0 }));
        //        //}
        //    }
        //    else if (supplierId != 0 && specificationId == 0)
        //    {
        //        //result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
        //        //result.Names.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0;"));
        //        //result.Suppliers.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0;"));
        //        //result.Suppliers.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = supplierId }));
        //        if (result.Suppliers.Any())
        //        {
        //            result.Specifications.AddRange(
        //            ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND SupplierId IN @Id;", new { Id = result.Suppliers.Select(x => x.Id) }));
        //        }

        //        //if (result.Suppliers.Any())
        //        //{
        //        //    result.Names.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Suppliers.FirstOrDefault()?.NameId ?? 0 }));
        //        //}

        //        //if (result.Names.Any())
        //        //{
        //        //    result.Categories.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Names.FirstOrDefault()?.CategoryId ?? 0 }));
        //        //}
        //    }
        //    else if (specificationId != 0)
        //    {
        //        result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
        //        result.Names.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0;"));
        //        result.Suppliers.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0;"));
        //        result.Specifications.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0;"));
        //        //result.Specifications.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = specificationId }));

        //        //if (result.Specifications.Any())
        //        //{
        //        //    result.Suppliers.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Specifications.FirstOrDefault()?.SupplierId ?? 0 }));
        //        //}

        //        //if (result.Suppliers.Any())
        //        //{
        //        //    result.Names.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Suppliers.FirstOrDefault()?.NameId ?? 0 }));
        //        //}

        //        //if (result.Names.Any())
        //        //{
        //        //    result.Categories.AddRange(
        //        //    ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { Id = result.Names.FirstOrDefault()?.CategoryId ?? 0 }));
        //        //}
        //    }
        //    else
        //    {
        //        result.Categories.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Category FROM `material_category` WHERE `MarkedDelete` = 0;"));
        //        result.Names.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, CategoryId, `Name` FROM `material_name` WHERE `MarkedDelete` = 0;"));
        //        result.Suppliers.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, NameId, `Supplier` FROM `material_supplier` WHERE `MarkedDelete` = 0;"));
        //        result.Specifications.AddRange(ServerConfig.ApiDb.Query<dynamic>("SELECT Id, SupplierId, `Specification` FROM `material_specification` WHERE `MarkedDelete` = 0;"));
        //    }
        //    result.Sites.AddRange(
        //        ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, `Site` FROM `material_site` WHERE `MarkedDelete` = 0{(siteId == 0 ? "" : " AND Id = @siteId")};", new { siteId }));
        //    return result;
        //}

        // GET: api/MaterialManagement?categoryId=0&nameId=0&supplierId=0&specificationId=0&qId=0&siteId
        [HttpGet]
        public MaterialDataResult GetMaterialManagement([FromQuery] int categoryId, int nameId, int supplierId, int specificationId, int qId, int siteId)
        {
            var result = new MaterialDataResult();
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

            var data = ServerConfig.ApiDb.Query<MaterialManagementDetail>(sql, new { categoryId, nameId, supplierId, specificationId, qId, siteId }).OrderBy(x => x.Id);
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<MaterialDataResult>(Error.MaterialBillNotExist);
            }

            result.Count = data.Sum(x => x.Number);
            result.Sum = data.Sum(x => x.Number * x.Price);
            result.datas.AddRange(data);
            return result;
        }

        // POST: api/MaterialManagement/Increase
        [HttpPost("Increase")]
        public object IncreaseMaterial([FromBody] IncreaseMaterialManagementDetail materialManagement)
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

            var result = new DataResult();
            var createUserId = Request.GetIdentityInformation();
            #region 新货品
            var materialBills = materialManagement.Bill.Where(x => x.SiteId == 0 || x.SpecificationId == 0 || x.SupplierId == 0 || x.NameId == 0 || x.NameId == 0 || x.CategoryId == 0 || x.BillId == 0);
            if (materialBills.Any())
            {
                var eCodes = materialBills.GroupBy(x => x.Code).Where(y => y.GroupBy(z => new { z.SpecificationId, z.SiteId, z.Price }).Count() > 1).Select(c => c.Key);
                if (eCodes.Any())
                {
                    result.errno = Error.MaterialBillDuplicate;
                    result.datas.AddRange(eCodes);
                    return result;
                }

                var codes = materialBills.GroupBy(x => x.Code).Select(y => y.Key);
                var sameCode = ServerConfig.ApiDb.Query<string>("SELECT Code FROM `material_bill` WHERE Code IN @Code AND MarkedDelete = 0;", new { Code = codes });
                if (sameCode.Any())
                {
                    result.errno = Error.MaterialBillIsExist;
                    result.datas.AddRange(sameCode);
                    return result;
                }

                foreach (var bill in materialBills)
                {
                    bill.BillId = 0;
                }
                if (materialBills.Any(x => x.SiteId == 0 && x.Site.IsNullOrEmpty()) ||
                    materialBills.Any(x => x.SpecificationId == 0 && x.Specification.IsNullOrEmpty()) ||
                    materialBills.Any(x => x.SupplierId == 0 && x.Supplier.IsNullOrEmpty()) ||
                    materialBills.Any(x => x.NameId == 0 && x.Name.IsNullOrEmpty()) ||
                    materialBills.Any(x => x.CategoryId == 0 && x.Category.IsNullOrEmpty()))
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }

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

            }

            materialBills = materialManagement.Bill.Where(x => x.BillId == 0);
            if (materialBills.Any())
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

            var billId_0 = materialManagement.Bill.Where(x => x.BillId == 0);
            if (billId_0.Any())
            {
                var materialBillIds =
                    ServerConfig.ApiDb.Query<MaterialBill>("SELECT * FROM `material_bill` WHERE Code IN @Code AND `MarkedDelete` = 0;",
                        new { Code = billId_0.Select(x => x.Code) });
                foreach (var bill in billId_0)
                {
                    var materialBill = materialBillIds.FirstOrDefault(x => x.Code == bill.Code);
                    if (materialBill != null)
                    {
                        bill.BillId = materialBill.Id;
                    }
                }
            }
            #endregion

            var mBill = materialManagement.Bill.GroupBy(x => x.BillId).Select(x => x.Key);
            var allBill = ServerConfig.ApiDb.Query<ProductionPlanBillStockDetail>("SELECT a.*, IFNULL(b.Number, 0) Number FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.MarkedDelete = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            var planBill = materialManagement.Bill.Where(x => x.PlanId != 0).GroupBy(y => new { y.PlanId, y.BillId }).Select(z => z.Key);
            var actualPlanBill = new List<OpMaterialManagement>();
            actualPlanBill.AddRange(planBill.Select(x => new OpMaterialManagement
            {
                BillId = x.BillId,
                PlanId = x.PlanId,
                Number = materialManagement.Bill.Where(y => y.PlanId == x.PlanId && y.BillId == x.BillId).Sum(y => y.Number),
                Purpose = materialManagement.Bill.FirstOrDefault(y => y.PlanId == x.PlanId && y.BillId == x.BillId)?.Purpose ?? "",
                RelatedPerson = materialManagement.Bill.FirstOrDefault(y => y.PlanId == x.PlanId && y.BillId == x.BillId)?.RelatedPerson ?? "",
            }));
            var logs = new List<MaterialLog>();
            var markedDateTime = DateTime.Now;
            if (planBill.Any())
            {
                var allPlanBill = ServerConfig.ApiDb.Query<ProductionPlanBill>("SELECT Id, PlanId, BillId, ActualConsumption FROM `production_plan_bill` WHERE PlanId IN @PlanId AND BillId IN @BillId AND `MarkedDelete` = 0;",
                    new { PlanId = planBill.Select(x => x.PlanId), BillId = planBill.Select(x => x.BillId) });
                Dictionary<dynamic, dynamic> plans = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Plan FROM  `production_plan` WHERE Id IN @PlanID;",
                    new { PlanId = planBill.Select(x => x.PlanId) }).ToDictionary(x => x.Id);
                Dictionary<dynamic, dynamic> billCode = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Code FROM  `material_bill` WHERE Id IN @BillId;",
                    new { BillId = planBill.Select(x => x.BillId) }).ToDictionary(x => x.Id);
                if (allPlanBill.Count() != planBill.Count())
                {
                    result = new DataResult { errno = Error.ProductionPlanBillNotExist };
                    var notExist = planBill.Where(x =>
                        allPlanBill.All(y => y.PlanId != x.PlanId && y.BillId != x.BillId));
                    //plans = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Plan FROM  `production_plan` WHERE Id IN @PlanID;",
                    //    new { PlanId = planBill.Select(x => x.PlanId) }).ToDictionary(x => x.Id);
                    //billCode = ServerConfig.ApiDb.Query<dynamic>("SELECT Id, Code FROM  `material_bill` WHERE Id IN @BillId;",
                    //   new { BillId = planBill.Select(x => x.BillId) }).ToDictionary(x => x.Id);
                    result.datas.AddRange(notExist.Where(x => plans.ContainsKey(x.PlanId) && billCode.ContainsKey(x.BillId)).Select(y => new { Plan = plans[y.PlanId], Code = billCode[y.BillId] }));
                    return result;
                }

                var notEnough = actualPlanBill.Where(x =>
                    allPlanBill.First(y => y.PlanId == x.PlanId && y.BillId == x.BillId).ActualConsumption < x.Number);
                if (notEnough.Any())
                {
                    result = new DataResult { errno = Error.ProductionPlanBillActualConsumeLess };
                    result.datas.AddRange(notEnough.Where(x => plans.ContainsKey(x.PlanId) && billCode.ContainsKey(x.BillId)).Select(y => new { Plan = plans[y.PlanId], Code = billCode[y.BillId] }));
                    return result;
                }

                foreach (var bill in allPlanBill)
                {
                    bill.ActualConsumption -=
                        actualPlanBill.FirstOrDefault(y => y.PlanId == bill.PlanId && y.BillId == bill.BillId)
                            ?.Number ?? 0;
                    bill.MarkedDateTime = markedDateTime;
                }

                ServerConfig.ApiDb.Execute(
                    "UPDATE production_plan_bill SET `MarkedDateTime` = @MarkedDateTime, `ActualConsumption` = @ActualConsumption WHERE `Id` = @Id;", allPlanBill);
                logs.AddRange(actualPlanBill.Select(x => new MaterialLog
                {
                    Time = markedDateTime,
                    BillId = x.BillId,
                    Code = allBill.First(y => y.Id == x.BillId).Code,
                    Type = 1,
                    PlanId = x.PlanId,
                    Plan = plans == null ? "" : plans[x.PlanId].Plan,
                    Purpose = plans == null ? "" : plans[x.PlanId].Plan,
                    Number = x.Number,
                    OldNumber = allBill.First(y => y.Id == x.BillId).Number,
                    RelatedPerson = x.RelatedPerson,
                    Manager = createUserId
                }));
            }

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
            ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `Number`) VALUES (@BillId, @InTime, @Number);", addBill.GroupBy(y => y.BillId).Select(
                x => new
                {
                    BillId = x.Key,
                    InTime = markedDateTime,
                    Number = addBill.Where(z => z.BillId == x.Key).Sum(a => a.Number)
                }));

            #endregion
            logs.AddRange(materialManagement.Bill.Where(y => y.PlanId == 0).Select(x => new MaterialLog
            {
                Time = markedDateTime,
                BillId = x.BillId,
                Code = allBill.First(y => y.Id == x.BillId).Code,
                Type = 1,
                Purpose = x.Purpose,
                Number = x.Number,
                OldNumber = allBill.First(y => y.Id == x.BillId).Number,
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
            var allBill = ServerConfig.ApiDb.Query<ProductionPlanBillStockDetail>("SELECT  a.*, IFNULL(b.Number, 0) Number FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.MarkedDelete = 0;", new { ids = mBill });
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
                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO production_plan_bill (`CreateUserId`, `MarkedDateTime`, `PlanId`, `BillId`, `ActualConsumption`, `Extra`) " +
                            "VALUES (@CreateUserId, @MarkedDateTime, @PlanId, @BillId, @Number, 1);",
                            extraBill.Select(x =>
                            {
                                x.CreateUserId = createUserId;
                                x.MarkedDateTime = markedDateTime;
                                x.PlanId = materialManagement.PlanId;
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

                var logs = materialManagement.Bill.Select(x => new MaterialLog
                {
                    Time = markedDateTime,
                    BillId = x.BillId,
                    Code = allBill.First(y => y.Id == x.BillId).Code,
                    Type = 2,
                    Mode = 1,
                    Purpose = materialManagement.PlanId != 0 ? materialManagement.Plan : x.Purpose,
                    PlanId = materialManagement.PlanId,
                    Number = x.Number,
                    OldNumber = allBill.First(y => y.Id == x.BillId).Number,
                    RelatedPerson = x.RelatedPerson,
                    Manager = createUserId
                });

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
            return result;
        }

        // POST: api/MaterialManagement/Correct
        [HttpPost("Correct")]
        public object CorrectMaterial([FromBody] IncreaseMaterialManagementDetail materialManagement)
        {
            if (materialManagement.Bill == null)
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }

            if (!materialManagement.Bill.Any())
            {
                return Result.GenError<Result>(Error.MaterialManagementNotEmpty);
            }
            var mBill = materialManagement.Bill.GroupBy(x => x.BillId).Select(x => x.Key);
            var allBill = ServerConfig.ApiDb.Query<MaterialManagement>("SELECT a.Id, a.`Code`, IFNULL(b.Number, 0) Number, IFNULL(b.BillId, 0) BillId FROM `material_bill` a LEFT JOIN `material_management` b ON a.Id = b.BillId WHERE a.Id IN @ids AND a.`MarkedDelete` = 0;", new { ids = mBill });
            if (allBill.Count() != mBill.Count())
            {
                return Result.GenError<Result>(Error.MaterialBillNotExist);
            }

            var updateBill = materialManagement.Bill.GroupBy(y => y.BillId).Select(z => z.Key);
            var actualBill = new List<OpMaterialManagement>();
            actualBill.AddRange(updateBill.Select(x => new OpMaterialManagement
            {
                BillId = x,
                Number = materialManagement.Bill.Where(y => y.BillId == x).Sum(y => y.Number),
                Remark = materialManagement.Bill.FirstOrDefault(y => y.BillId == x)?.Remark ?? "",
                RelatedPerson = materialManagement.Bill.FirstOrDefault(y => y.BillId == x)?.RelatedPerson ?? "",
            }));
            var logs = new List<MaterialLog>();
            var markedDateTime = DateTime.Now;
            var createUserId = Request.GetIdentityInformation();
            #region 更新

            var existBill = allBill.Where(x => x.BillId != 0);
            foreach (var bill in existBill)
            {
                var d = actualBill.FirstOrDefault(x => x.BillId == bill.BillId);
                if (d != null && d.Number != bill.Number)
                {
                    logs.Add(new MaterialLog
                    {
                        Time = markedDateTime,
                        BillId = bill.BillId,
                        Code = bill.Code,
                        Type = 3,
                        Mode = d.Number > bill.Number ? 0 : 1,
                        Purpose = d.Remark,
                        Number = d.Number,
                        OldNumber = bill.Number,
                        RelatedPerson = d.RelatedPerson,
                        Manager = createUserId
                    });
                    bill.Number = d.Number;
                }
            }

            ServerConfig.ApiDb.Execute("UPDATE material_management SET `Number` = @Number WHERE `BillId` = @BillId;", existBill);
            #endregion

            #region 添加
            var addBill = actualBill.Where(x => existBill.All(y => y.BillId != x.BillId));
            ServerConfig.ApiDb.Execute("INSERT INTO material_management (`BillId`, `InTime`, `Number`) VALUES (@BillId, @InTime, @Number);", addBill.Select(
                x =>
                    {
                        x.CreateUserId = createUserId;
                        x.InTime = markedDateTime;
                        return x;
                    }));
            #endregion
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

            return Result.GenError<Result>(Error.Success);
        }
    }
}