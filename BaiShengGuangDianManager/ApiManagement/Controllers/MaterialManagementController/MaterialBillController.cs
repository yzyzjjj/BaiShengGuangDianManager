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
                catch (Exception e)
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

                if (uMaterialBills.GroupBy(x => new { x.SpecificationId, x.SiteId }).Select(y => y.Key).Count() != uMaterialBills.Count())
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

                if (mBs.GroupBy(x => new { x.SpecificationId, x.SiteId }).Select(y => y.Key).Count() != mBs.Count())
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
        public DataResult PostMaterialBill([FromBody] IEnumerable<MaterialBill> materialBills)
        {
            var specificationIds = materialBills.GroupBy(x => x.SpecificationId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_specification` WHERE Id IN @Id AND `MarkedDelete` = 0;",
                    new { Id = specificationIds }).FirstOrDefault();
            if (cnt != specificationIds.Count())
            {
                return Result.GenError<DataResult>(Error.MaterialSpecificationNotExist);
            }
            var siteIds = materialBills.GroupBy(x => x.SiteId).Select(y => y.Key);
            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @Id AND `MarkedDelete` = 0;",
                    new { Id = siteIds }).FirstOrDefault();
            if (cnt != siteIds.Count())
            {
                return Result.GenError<DataResult>(Error.MaterialSiteNotExist);
            }

            var result = new DataResult();
            var sameCode = materialBills.GroupBy(x => x.Code).Where(y => y.Count() > 1).Select(z => z.Key);
            if (sameCode.Any())
            {
                result.errno = Error.MaterialBillDuplicate;
                result.datas.AddRange(sameCode);
                return result;
            }

            var sameSS = materialBills.GroupBy(x => new { x.SpecificationId, x.SiteId }).Where(y => y.Count() > 1).Select(z => z.Key);
            if (sameSS.Any())
            {
                result.errno = Error.MaterialBillSpecificationSiteDuplicate;
                result.datas.AddRange(sameSS);
                return result;
            }

            var codes = materialBills.GroupBy(x => x.Code).Select(y => y.Key);
            sameCode = ServerConfig.ApiDb.Query<string>("SELECT Code FROM `material_bill` WHERE Code IN @Code AND MarkedDelete = 0;", new { Code = codes });
            if (sameCode.Any())
            {
                result.errno = Error.MaterialBillIsExist;
                result.datas.AddRange(sameCode);
                return result;
            }

            var sameSSOld = ServerConfig.ApiDb.Query<dynamic>("SELECT SpecificationId, SiteId FROM `material_bill` WHERE SpecificationId IN @SpecificationId AND SiteId IN @SiteId AND MarkedDelete = 0;",
                new { SpecificationId = specificationIds, SiteId = siteIds });
            if (sameSSOld.Any())
            {
                result.errno = Error.MaterialBillSpecificationSiteIsExist;
                result.datas.AddRange(sameSSOld);
                return result;
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var materialBill in materialBills)
            {
                materialBill.CreateUserId = createUserId;
                materialBill.MarkedDateTime = markedDateTime;
                materialBill.Remark = materialBill.Remark ?? "";
                materialBill.Images = materialBill.Images ?? "[]";
            }
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_bill (`CreateUserId`, `MarkedDateTime`, `SpecificationId`, `SiteId`, `Code`, `Unit`, `Price`, `Stock`, `Images`, `Remark`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @SpecificationId, @SiteId, @Code, @Unit, @Price, @Stock, @Images, @Remark);",
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