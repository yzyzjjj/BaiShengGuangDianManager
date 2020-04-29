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
    /// 货品名称
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialNameController : ControllerBase
    {
        /// <summary>
        /// 根据类别获取 0 表示所有
        /// </summary>
        /// <returns></returns>
        // GET: api/MaterialName
        [HttpGet]
        public DataResult GetMaterialName([FromQuery] int categoryId, int qId)
        {
            var result = new DataResult();
            string sql;
            if (categoryId != 0 && qId == 0)
            {
                sql =
                    "SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id WHERE a.CategoryId = @categoryId AND a.`MarkedDelete` = 0;";
            }
            else
            {
                sql =
                    $"SELECT a.*, b.Category FROM `material_name` a JOIN `material_category` b ON a.CategoryId = b.Id WHERE {(qId == 0 ? "" : "a.Id = @qId AND ")}a.`MarkedDelete` = 0;";
            }

            var data = ServerConfig.ApiDb.Query<MaterialNameDetail>(sql, new { categoryId, qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialNameNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialName
        [HttpPut]
        public Result PutMaterialName([FromBody] IEnumerable<MaterialName> materialNames)
        {
            if (materialNames == null)
            {
                return Result.GenError<Result>(Error.MaterialNameNotExist);
            }

            if (materialNames.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialNameNotEmpty);
            }

            var materialCategories = materialNames.GroupBy(x => x.CategoryId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id IN @Id AND `MarkedDelete` = 0;", new { Id = materialCategories }).FirstOrDefault();
            if (cnt != materialCategories.Count())
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotExist);
            }

            var ids = materialNames.Select(x => x.Id);
            var uMaterialNames =
                ServerConfig.ApiDb.Query<MaterialName>("SELECT * FROM `material_name` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = ids });
            if (uMaterialNames.Count() != materialNames.Count())
            {
                return Result.GenError<Result>(Error.MaterialNameNotExist);
            }
            foreach (var materialName in materialNames)
            {
                uMaterialNames.First(x => x.Id == materialName.Id).CategoryId = materialName.CategoryId;
                uMaterialNames.First(x => x.Id == materialName.Id).Name = materialName.Name;
                uMaterialNames.First(x => x.Id == materialName.Id).Remark = materialName.Remark;
            }

            if (uMaterialNames.GroupBy(x => new { x.CategoryId, x.Name }).Select(y => y.Key).Count() != uMaterialNames.Count())
            {
                return Result.GenError<Result>(Error.MaterialNameDuplicate);
            }

            var mNs =
                ServerConfig.ApiDb.Query<MaterialName>("SELECT * FROM `material_name` WHERE CategoryId IN @categoryId AND Id NOT IN @id AND `MarkedDelete` = 0;",
                    new { categoryId = materialCategories, id = ids }).ToList();

            foreach (var materialCategory in materialCategories)
            {
                var mns = mNs.Where(x => x.CategoryId == materialCategory);
                var uMns = uMaterialNames.Where(x => x.CategoryId == materialCategory);
                foreach (var materialName in uMns)
                {
                    if (!mns.Any(x => x.Id == materialName.Id))
                    {
                        mNs.Add(materialName);
                    }
                }
            }

            if (mNs.GroupBy(x => new { x.CategoryId, x.Name }).Select(y => y.Key).Count() != mNs.Count())
            {
                return Result.GenError<Result>(Error.MaterialNameIsExist);
            }
            
            ServerConfig.ApiDb.Execute(
                "UPDATE material_name SET `CategoryId` = @CategoryId, `Name` = @Name, `Remark` = @Remark WHERE `Id` = @Id;", materialNames);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialName
        [HttpPost]
        public Result PostMaterialName([FromBody] MaterialName materialName)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = materialName.CategoryId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotExist);
            }

            cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_name` WHERE Name = @Name AND CategoryId = @CategoryId AND MarkedDelete = 0;",
                   new { materialName.Name, materialName.CategoryId }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialNameIsExist);
            }
            materialName.CreateUserId = Request.GetIdentityInformation();
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_name (`CreateUserId`, `CategoryId`, `Name`, `Remark`) " +
              "VALUES (@CreateUserId, @CategoryId, @Name, @Remark);",
              materialName);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialName
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialName([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_name` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialNameNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_name` SET `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}