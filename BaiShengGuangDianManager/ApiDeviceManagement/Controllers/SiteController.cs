using ApiDeviceManagement.Base.Server;
using ApiDeviceManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.ServerConfig.Enum;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiDeviceManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SiteController : ControllerBase
    {
        // GET: api/Site
        [HttpGet]
        public DataResult GetSite()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<Site>("SELECT * FROM `site` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/Site/5
        [HttpGet("{id}")]
        public DataResult GetSite([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<Site>("SELECT * FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
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
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }

            site.Id = id;
            site.CreateUserId = Request.GetIdentityInformation();
            site.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE site SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`SiteName` = @SiteName, `RegionDescription` = @RegionDescription, `Manager` = @Manager WHERE `Id` = @Id;", site);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Site
        [HttpPost]
        public Result PostSite([FromBody] Site site)
        {

            site.CreateUserId = Request.GetIdentityInformation();
            site.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
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
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `site` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SiteNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `site` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}