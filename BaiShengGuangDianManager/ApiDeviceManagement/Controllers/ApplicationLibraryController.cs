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
    public class ApplicationLibraryController : ControllerBase
    {
        // GET: api/ApplicationLibrary
        [HttpGet]
        public DataResult GetApplicationLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<ApplicationLibrary>("SELECT * FROM `application_library` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/ApplicationLibrary/5
        [HttpGet("{id}")]
        public DataResult GetApplicationLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<ApplicationLibrary>("SELECT * FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ApplicationLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/ApplicationLibrary/5
        [HttpPut("{id}")]
        public Result PutApplicationLibrary([FromRoute] int id, [FromBody] ApplicationLibrary processLibrary)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }

            processLibrary.Id = id;
            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE application_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ApplicationName` = @ApplicationName, `FilePath` = @FilePath, `Description` = @Description WHERE `Id` = @Id;", processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ApplicationLibrary
        [HttpPost]
        public Result PostApplicationLibrary([FromBody] ApplicationLibrary processLibrary)
        {

            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
              "INSERT INTO application_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ApplicationName`, `FilePath`, `Description`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ApplicationName, @FilePath, @Description);",
              processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ApplicationLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteApplicationLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `application_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}