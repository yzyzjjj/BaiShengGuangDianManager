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
using System.IO;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 货品规格
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialSpecificationController : ControllerBase
    {
        // GET: api/MaterialSpecification?categoryId=0&nameId=0&supplierId=0&qId=0
        [HttpGet]
        public DataResult GetMaterialSpecification([FromQuery] int categoryId, int nameId, int supplierId, int qId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && nameId == 0 && supplierId == 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id WHERE  a.`MarkedDelete` = 0;";
            }
            else if (nameId != 0 && supplierId == 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id WHERE a.NameId = @nameId ) b ON a.SupplierId = b.Id WHERE  a.`MarkedDelete` = 0;";
            }
            else if (supplierId != 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    "JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id WHERE a.SupplierId = @supplierId AND a.`MarkedDelete` = 0;";
            }
            else
            {
                sql =
                    "SELECT a.*, b.CategoryId, b.Category, b.NameId, b.`Name`, b.Supplier FROM `material_specification` a " +
                    "JOIN ( SELECT a.*, b.`Name`, b.CategoryId, b.Category FROM `material_supplier` a " +
                    "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                    $"JOIN `material_category` b ON a.CategoryId = b.Id ) b ON a.NameId = b.Id ) b ON a.SupplierId = b.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0;";
            }

            var data = ServerConfig.ApiDb.Query<MaterialSpecificationDetail>(sql,
                new { categoryId, nameId, supplierId, qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialSpecificationNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialSpecification
        [HttpPut]
        public Result PutMaterialSpecification([FromBody] IEnumerable<MaterialSpecification> materialSpecifications)
        {
            if (materialSpecifications == null)
            {
                return Result.GenError<Result>(Error.MaterialSpecificationNotExist);
            }

            if (materialSpecifications.Any(x => x.Specification.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialSpecificationNotEmpty);
            }

            var materialSuppliers = materialSpecifications.GroupBy(x => x.SupplierId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_supplier` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialSuppliers }).FirstOrDefault();
            if (cnt != materialSuppliers.Count())
            {
                return Result.GenError<Result>(Error.MaterialSupplierNotExist);
            }

            var ids = materialSpecifications.Select(x => x.Id);
            var uMaterialSpecifications =
                ServerConfig.ApiDb.Query<MaterialSpecification>("SELECT * FROM `material_specification` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = ids });
            if (uMaterialSpecifications.Count() != materialSpecifications.Count())
            {
                return Result.GenError<Result>(Error.MaterialSpecificationNotExist);
            }
            foreach (var materialSpecification in materialSpecifications)
            {
                uMaterialSpecifications.First(x => x.Id == materialSpecification.Id).SupplierId = materialSpecification.SupplierId;
                uMaterialSpecifications.First(x => x.Id == materialSpecification.Id).Specification = materialSpecification.Specification;
                uMaterialSpecifications.First(x => x.Id == materialSpecification.Id).Remark = materialSpecification.Remark;
            }

            if (uMaterialSpecifications.GroupBy(x => new { x.SupplierId, x.Specification }).Select(y => y.Key).Count() != uMaterialSpecifications.Count())
            {
                return Result.GenError<Result>(Error.MaterialSpecificationDuplicate);
            }

            var mSs =
                ServerConfig.ApiDb.Query<MaterialSpecification>("SELECT * FROM `material_specification` WHERE SupplierId IN @supplierId AND Id NOT IN @id AND `MarkedDelete` = 0;",
                    new { supplierId = materialSuppliers, id = ids }).ToList();

            foreach (var materialSupplier in materialSuppliers)
            {
                var mns = mSs.Where(x => x.SupplierId == materialSupplier);
                var uMns = uMaterialSpecifications.Where(x => x.SupplierId == materialSupplier);
                foreach (var materialSpecification in uMns)
                {
                    if (!mns.Any(x => x.Id == materialSpecification.Id))
                    {
                        mSs.Add(materialSpecification);
                    }
                }
            }

            if (mSs.GroupBy(x => new { x.SupplierId, x.Specification }).Select(y => y.Key).Count() != mSs.Count())
            {
                return Result.GenError<Result>(Error.MaterialSpecificationIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var materialSpecification in materialSpecifications)
            {
                materialSpecification.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE material_specification SET `MarkedDateTime` = @MarkedDateTime, `SupplierId` = @SupplierId, `Specification` = @Specification, `Remark` = @Remark WHERE `Id` = @Id;", materialSpecifications);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialSpecification
        [HttpPost]
        public DataResult PostMaterialSpecification([FromBody] IEnumerable<MaterialSpecification> materialSpecifications)
        {
            var supplierIds = materialSpecifications.GroupBy(x => x.SupplierId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_supplier` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = supplierIds }).FirstOrDefault();
            if (cnt != supplierIds.Count())
            {
                return Result.GenError<DataResult>(Error.MaterialSupplierNotExist);
            }

            var result = new DataResult();
            var sameMs = materialSpecifications.GroupBy(x => new { x.SupplierId, x.Specification }).Where(y => y.Count() > 1).Select(z => z.Key);
            if (sameMs.Any())
            {
                result.errno = Error.MaterialSpecificationIsExist;
                result.datas.AddRange(sameMs);
                return result;
            }

            var specifications = materialSpecifications.GroupBy(x => x.Specification).Select(y => y.Key);
            var sameMsOld = ServerConfig.ApiDb.Query<dynamic>("SELECT SupplierId, Specification FROM `material_specification` WHERE Specification IN @Specification AND SupplierId IN @SupplierId AND MarkedDelete = 0;",
                     new { Specification = specifications, SupplierId = supplierIds });
            if (sameMsOld.Any())
            {
                result.errno = Error.MaterialSpecificationIsExist;
                result.datas.AddRange(sameMsOld);
                return result;
            }

            var createUserId = Request.GetIdentityInformation();
            foreach (var materialSpecification in materialSpecifications)
            {
                materialSpecification.CreateUserId = createUserId;
                materialSpecification.Specification = materialSpecification.Specification ?? "";
                materialSpecification.Remark = materialSpecification.Remark ?? "";
            }
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_specification (`CreateUserId`, `SupplierId`, `Specification`, `Remark`) " +
              "VALUES (@CreateUserId, @SupplierId, @Specification, @Remark);",
              materialSpecifications);

            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/MaterialSpecification
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialSpecification([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_specification` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialSpecificationNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_specification` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}