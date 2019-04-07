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
    public class ProcessLibraryController : ControllerBase
    {
        // GET: api/ProcessLibrary
        [HttpGet]
        public DataResult GetProcessLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<ProcessLibrary>("SELECT * FROM `process_library`;"));
            return result;
        }

        // GET: api/ProcessLibrary/5
        [HttpGet("{id}")]
        public DataResult GetProcessLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<ProcessLibrary>("SELECT * FROM `process_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ProcessLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/ProcessLibrary/5
        [HttpPut("{id}")]
        public Result PutProcessLibrary([FromRoute] int id, [FromBody] ProcessLibrary processLibrary)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `process_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessLibraryNotExist);
            }

            processLibrary.Id = id;
            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE process_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ProcessName` = @ProcessName, `FilePath` = @FilePath, `Description` = @Description WHERE `Id` = @Id;", processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ProcessLibrary
        [HttpPost]
        public Result PostProcessLibrary([FromBody] ProcessLibrary processLibrary)
        {

            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
              "INSERT INTO process_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ProcessName`, `FilePath`, `Description`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ProcessName, @FilePath, @Description);",
              processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ProcessLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteProcessLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `process_library` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ProcessLibraryNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `process_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}