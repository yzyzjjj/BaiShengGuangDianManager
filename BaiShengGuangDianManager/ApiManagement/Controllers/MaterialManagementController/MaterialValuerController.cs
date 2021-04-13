using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料请购单
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialValuerController : ControllerBase
    {
        // GET: api/MaterialValuer/?qId=0
        [HttpGet]
        public DataResult GetMaterialValuer([FromQuery] int qId)
        {
            var result = new DataResult();
            var p = new List<string>();
            if (qId != 0)
            {
                p.Add(" AND Id = @qId");
            }

            var sql = "SELECT * FROM `material_valuer` WHERE `MarkedDelete` = 0" + p.Join("");
            var data = ServerConfig.ApiDb.Query<MaterialValuer>(sql, new { qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialValuerNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialValuer
        [HttpPut]
        public Result PutMaterialValuer([FromBody] MaterialValuer materialValuer)
        {
            if (materialValuer == null)
            {
                return Result.GenError<Result>(Error.MaterialValuerNotExist);
            }

            if (materialValuer.Valuer.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.MaterialValuerNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_valuer` WHERE Id = @id AND `MarkedDelete` = 0;",
                    new { id = materialValuer.Id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialValuerNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_valuer` WHERE Id != @id AND Valuer = @Valuer AND `MarkedDelete` = 0;",
                    new { id = materialValuer.Id, materialValuer.Valuer }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialValuerIsExist);
            }

            var markedDateTime = DateTime.Now;
            materialValuer.MarkedDateTime = markedDateTime;

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_valuer` SET `MarkedDateTime` = @MarkedDateTime, `Valuer` = @Valuer, `Remark` = @Remark WHERE `Id` = @Id;", materialValuer);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialValuer
        [HttpPost]
        public Result PostMaterialValuer([FromBody] MaterialValuer materialValuer)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_valuer` WHERE Valuer = @Valuer AND MarkedDelete = 0;",
                    new { materialValuer.Valuer }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialValuerIsExist);
            }
            materialValuer.CreateUserId = Request.GetIdentityInformation();
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_valuer (`CreateUserId`, `Valuer`, `Remark`) VALUES (@CreateUserId, @Valuer, @Remark);",
              materialValuer);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialValuer
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialValuer([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_valuer` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialValuerNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_valuer` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}