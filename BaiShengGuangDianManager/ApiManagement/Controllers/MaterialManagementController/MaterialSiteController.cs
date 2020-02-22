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
    /// 货品场地
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialSiteController : ControllerBase
    {
        // GET: api/MaterialSite/?qId=0
        [HttpGet]
        public DataResult GetMaterialSite([FromQuery] int qId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<MaterialSite>($"SELECT * FROM `material_site` WHERE {(qId == 0 ? "" : "Id = @id AND ")}`MarkedDelete` = 0;",
                new { id = qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialSiteNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialSite
        [HttpPut]
        public Result PutMaterialSite([FromBody] IEnumerable<MaterialSite> materialSites)
        {
            if (materialSites == null)
            {
                return Result.GenError<Result>(Error.MaterialSiteNotExist);
            }

            if (materialSites.Any(x => x.Site.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialSiteNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = materialSites.Select(x => x.Id) }).FirstOrDefault();
            if (cnt != materialSites.Count())
            {
                return Result.GenError<Result>(Error.MaterialSiteNotExist);
            }

            if (materialSites.Count() != materialSites.GroupBy(x => x.Site).Count())
            {
                return Result.GenError<Result>(Error.MaterialSiteDuplicate);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id NOT IN @id AND Site IN @site AND `MarkedDelete` = 0;",
                    new { id = materialSites.Select(x => x.Id), site = materialSites.Select(x => x.Site) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialSiteIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var materialSite in materialSites)
            {
                materialSite.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE material_site SET `MarkedDateTime` = @MarkedDateTime, `Site` = @Site, `Remark` = @Remark WHERE `Id` = @Id;", materialSites);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialSite
        [HttpPost]
        public Result PostMaterialSite([FromBody] MaterialSite materialSite)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Site = @Site AND MarkedDelete = 0;",
                    new { materialSite.Site }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialSiteIsExist);
            }
            materialSite.CreateUserId = Request.GetIdentityInformation();
            materialSite.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_site (`CreateUserId`, `MarkedDateTime`, `Site`, `Remark`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @Site, @Remark);",
              materialSite);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialSite
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialSite([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialSiteNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_site` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}