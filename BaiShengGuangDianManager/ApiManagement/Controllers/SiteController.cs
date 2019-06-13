using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;

namespace ApiManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SiteController : ControllerBase
    {
        // GET: api/Site
        [HttpGet]
        public DataResult GetSite()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<Site>("SELECT * FROM `site` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/Site/5
        [HttpGet("{id}")]
        public DataResult GetSite([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<Site>("SELECT * FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.SiteNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/Site/5
        [HttpPut("{id}")]
        public Result PutSite([FromRoute] int id, [FromBody] Site site)
        {
            var data =
                ServerConfig.ApiDb.Query<Site>("SELECT * FROM `site` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE SiteName = @SiteName AND RegionDescription = @RegionDescription AND MarkedDelete = 0;", new { site.SiteName, site.RegionDescription }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!site.SiteName.IsNullOrEmpty() && data.SiteName != site.SiteName)
                {
                    return Result.GenError<Result>(Error.SiteIsExist);
                }
            }
            site.Id = id;
            site.CreateUserId = Request.GetIdentityInformation();
            site.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE site SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`SiteName` = @SiteName, `RegionDescription` = @RegionDescription, `Manager` = @Manager WHERE `Id` = @Id;", site);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Site
        [HttpPost]
        public Result PostSite([FromBody] Site site)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE SiteName = @SiteName AND RegionDescription = @RegionDescription AND MarkedDelete = 0;", new { site.SiteName, site.RegionDescription }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.SiteIsExist);
            }
            site.CreateUserId = Request.GetIdentityInformation();
            site.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO site (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `SiteName`, `RegionDescription`, `Manager`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @SiteName, @RegionDescription, @Manager);",
              site);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Site/5
        [HttpDelete("{id}")]
        public Result DeleteSite([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `site` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}