using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.HomeController
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkshopController : ControllerBase
    {
        // GET: api/Workshop
        [HttpGet]
        public DataResult GetWorkshop()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<Site>("SELECT Id, SiteName FROM `site` WHERE MarkedDelete = 0 GROUP BY SiteName ORDER BY Id;"));
            return result;
        }

        // GET: api/Workshop/Device
        [HttpGet("Device")]
        public DataResult GetWorkshopDevice([FromQuery]string workshopName)
        {
            var result = new DataResult();
            if (workshopName != "")
            {
                var siteIds = ServerConfig.ApiDb.Query<int>(
                    "SELECT Id FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                    {
                        SiteName = workshopName
                    });
                if (siteIds.Any())
                {
                    result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0 AND SiteId IN @SiteId;", new
                    {
                        SiteId = siteIds
                    }));
                }
            }
            else
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT * FROM `device_library` WHERE MarkedDelete = 0"));
            }
            return result;
        }


        //// GET: api/Workshop/5
        //[HttpGet("{id}")]
        //public DataResult GetWorkshop([FromRoute] int id)
        //{
        //    var result = new DataResult();
        //    var data =
        //        ServerConfig.ApiDb.Query<Workshop>("SELECT * FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
        //    if (data == null)
        //    {
        //        result.errno = Error.WorkshopNotExist;
        //        return result;
        //    }
        //    result.datas.Add(data);
        //    return result;
        //}

        //// PUT: api/Workshop/5
        //[HttpPut("{id}")]
        //public Result PutWorkshop([FromRoute] int id, [FromBody] Workshop workshop)
        //{
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
        //    if (cnt == 0)
        //    {
        //        return Result.GenError<Result>(Error.WorkshopNotExist);
        //    }

        //    workshop.Id = id;
        //    workshop.CreateUserId = Request.GetIdentityInformation();
        //    workshop.MarkedDateTime = DateTime.Now;
        //    ServerConfig.ApiDb.Execute(
        //        "UPDATE workshop SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
        //        "`ModifyId` = @ModifyId, `WorkshopName` = @WorkshopName, `Abbre` = @Abbre WHERE `Id` = @Id;", workshop);

        //    return Result.GenError<Result>(Error.Success);
        //}

        //// POST: api/Workshop
        //[HttpPost]
        //public Result PostWorkshop([FromBody] Workshop workshop)
        //{
        //    workshop.CreateUserId = Request.GetIdentityInformation();
        //    workshop.MarkedDateTime = DateTime.Now;
        //    ServerConfig.ApiDb.Execute(
        //      "INSERT INTO workshop (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `WorkshopName`, `Abbre`) " +
        //      "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @WorkshopName, @Abbre);",
        //      workshop);

        //    return Result.GenError<Result>(Error.Success);
        //}

        //// DELETE: api/Workshop/5
        //[HttpDelete("{id}")]
        //public Result DeleteWorkshop([FromRoute] int id)
        //{
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
        //    if (cnt == 0)
        //    {
        //        return Result.GenError<Result>(Error.WorkshopNotExist);
        //    }

        //    ServerConfig.ApiDb.Execute(
        //        "UPDATE `workshop` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
        //        {
        //            MarkedDateTime = DateTime.Now,
        //            MarkedDelete = true,
        //            Id = id
        //        });
        //    return Result.GenError<Result>(Error.Success);
        //}
    }
}