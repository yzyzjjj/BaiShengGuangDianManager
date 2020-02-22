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
    /// 货品类别
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialCategoryController : ControllerBase
    {
        // GET: api/MaterialCategory?qId=0
        [HttpGet]
        public DataResult GetMaterialCategory([FromQuery] int qId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<MaterialCategory>($"SELECT * FROM `material_category` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;",
                new { qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialCategoryNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialCategory
        [HttpPut]
        public Result PutMaterialCategory([FromBody] IEnumerable<MaterialCategory> materialCategories)
        {
            if (materialCategories == null)
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotExist);
            }

            if (materialCategories.Any(x => x.Category.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = materialCategories.Select(x => x.Id) }).FirstOrDefault();
            if (cnt != materialCategories.Count())
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotExist);
            }

            if (materialCategories.Count() != materialCategories.GroupBy(x => x.Category).Count())
            {
                return Result.GenError<Result>(Error.MaterialCategoryDuplicate);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id NOT IN @id AND Category IN @category AND `MarkedDelete` = 0;",
                    new { id = materialCategories.Select(x => x.Id), category = materialCategories.Select(x => x.Category) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialCategoryIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var materialCategory in materialCategories)
            {
                materialCategory.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE material_category SET `MarkedDateTime` = @MarkedDateTime, `Category` = @Category, `Remark` = @Remark WHERE `Id` = @Id;", materialCategories);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialCategory
        [HttpPost]
        public Result PostMaterialCategory([FromBody] MaterialCategory materialCategory)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Category = @Category AND MarkedDelete = 0;",
                    new { materialCategory.Category }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialCategoryIsExist);
            }
            materialCategory.CreateUserId = Request.GetIdentityInformation();
            materialCategory.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_category (`CreateUserId`, `MarkedDateTime`, `Category`, `Remark`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @Category, @Remark);",
              materialCategory);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialCategory
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialCategory([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_category` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialCategoryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_category` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}