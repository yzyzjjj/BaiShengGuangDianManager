using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 货品供应商
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialSupplierController : ControllerBase
    {
        // GET: api/MaterialSupplier?categoryId=0&nameId=0&qid=0
        [HttpGet]
        public DataResult GetMaterialSupplier([FromQuery] int categoryId, int nameId, int qId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && nameId == 0 && qId == 0)
            {
                sql = "SELECT a.*, b.CategoryId, b.Category, b.`Name` FROM `material_supplier` a " +
                      "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                      "JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId) b ON a.NameId = b.Id WHERE a.`MarkedDelete` = 0;";
            }
            else if (nameId != 0 && qId == 0)
            {
                sql = "SELECT a.*, b.CategoryId, b.Category, b.`Name` FROM `material_supplier` a " +
                      "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                      "JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id WHERE a.NameId = @nameId AND a.`MarkedDelete` = 0;";
            }
            else
            {
                sql = "SELECT a.*, b.CategoryId, b.Category, b.`Name` FROM `material_supplier` a " +
                      "JOIN ( SELECT a.*, b.Category FROM `material_name` a " +
                      $"JOIN `material_category` b ON a.CategoryId = b.Id) b ON a.NameId = b.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0;";
            }

            var data = ServerConfig.ApiDb.Query<MaterialSupplierDetail>(sql, new { categoryId, nameId, qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialSupplierNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialSupplier
        [HttpPut]
        public Result PutMaterialSupplier([FromBody] IEnumerable<MaterialSupplier> materialSuppliers)
        {
            if (materialSuppliers == null)
            {
                return Result.GenError<Result>(Error.MaterialSupplierNotExist);
            }

            if (materialSuppliers.Any(x => x.Supplier.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialSupplierNotEmpty);
            }

            var materialNames = materialSuppliers.GroupBy(x => x.NameId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_name` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialNames }).FirstOrDefault();
            if (cnt != materialNames.Count())
            {
                return Result.GenError<Result>(Error.MaterialNameNotExist);
            }

            var ids = materialSuppliers.Select(x => x.Id);
            var uMaterialSuppliers =
                ServerConfig.ApiDb.Query<MaterialSupplier>("SELECT * FROM `material_supplier` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = ids });
            if (uMaterialSuppliers.Count() != materialSuppliers.Count())
            {
                return Result.GenError<Result>(Error.MaterialSupplierNotExist);
            }
            foreach (var materialSupplier in materialSuppliers)
            {
                uMaterialSuppliers.First(x => x.Id == materialSupplier.Id).NameId = materialSupplier.NameId;
                uMaterialSuppliers.First(x => x.Id == materialSupplier.Id).Supplier = materialSupplier.Supplier;
                uMaterialSuppliers.First(x => x.Id == materialSupplier.Id).Remark = materialSupplier.Remark;
            }

            if (uMaterialSuppliers.GroupBy(x => new { x.NameId, x.Supplier }).Select(y => y.Key).Count() != uMaterialSuppliers.Count())
            {
                return Result.GenError<Result>(Error.MaterialSupplierDuplicate);
            }

            var mNs =
                ServerConfig.ApiDb.Query<MaterialSupplier>("SELECT * FROM `material_supplier` WHERE NameId IN @nameId AND Id NOT IN @id AND `MarkedDelete` = 0;",
                    new { nameId = materialNames, id = ids }).ToList();

            foreach (var materialName in materialNames)
            {
                var mns = mNs.Where(x => x.NameId == materialName);
                var uMns = uMaterialSuppliers.Where(x => x.NameId == materialName);
                foreach (var materialSupplier in uMns)
                {
                    if (!mns.Any(x => x.Id == materialSupplier.Id))
                    {
                        mNs.Add(materialSupplier);
                    }
                }
            }

            if (mNs.GroupBy(x => new { x.NameId, x.Supplier }).Select(y => y.Key).Count() != mNs.Count())
            {
                return Result.GenError<Result>(Error.MaterialSupplierIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var materialSupplier in materialSuppliers)
            {
                materialSupplier.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE material_supplier SET `MarkedDateTime` = @MarkedDateTime, `NameId` = @NameId, `Supplier` = @Supplier, `Remark` = @Remark WHERE `Id` = @Id;", materialSuppliers);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialSupplier
        [HttpPost]
        public Result PostMaterialSupplier([FromBody] MaterialSupplier materialSupplier)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_name` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = materialSupplier.NameId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialNameNotExist);
            }

            cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_supplier` WHERE Supplier = @Supplier AND NameId = @NameId AND MarkedDelete = 0;",
                   new { materialSupplier.Supplier, materialSupplier.NameId }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialSupplierIsExist);
            }
            materialSupplier.CreateUserId = Request.GetIdentityInformation();
            materialSupplier.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_supplier (`CreateUserId`, `MarkedDateTime`, `NameId`, `Supplier`, `Remark`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @NameId, @Supplier, @Remark);",
              materialSupplier);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialSupplier
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialSupplier([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_supplier` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialSupplierNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_supplier` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}