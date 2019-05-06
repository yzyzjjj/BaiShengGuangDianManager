using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class FirmwareLibraryController : ControllerBase
    {
        // GET: api/FirmwareLibrary
        [HttpGet]
        public DataResult GetFirmwareLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<FirmwareLibrary>("SELECT * FROM `firmware_library` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/FirmwareLibrary/5
        [HttpGet("{id}")]
        public DataResult GetFirmwareLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<FirmwareLibrary>("SELECT * FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.FirmwareLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/FirmwareLibrary/5
        [HttpPut("{id}")]
        public Result PutFirmwareLibrary([FromRoute] int id, [FromBody] FirmwareLibrary firmwareLibrary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            firmwareLibrary.Id = id;
            firmwareLibrary.CreateUserId = Request.GetIdentityInformation();
            firmwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE firmware_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`FirmwareName` = @FirmwareName, `VarNumber` = @VarNumber, `CommunicationProtocol` = @CommunicationProtocol, `FilePath` = @FilePath, `Description` = " +
                "@Description WHERE `Id` = @Id;", firmwareLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/FirmwareLibrary
        [HttpPost]
        public Result PostFirmwareLibrary([FromBody] FirmwareLibrary firmwareLibrary)
        {
            firmwareLibrary.CreateUserId = Request.GetIdentityInformation();
            firmwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO firmware_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `FirmwareName`, `VarNumber`, `CommunicationProtocol`, `FilePath`, `Description`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @FirmwareName, @VarNumber, @CommunicationProtocol, @FilePath, @Description);",
                firmwareLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/FirmwareLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteFirmwareLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `firmware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.FirmwareLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `firmware_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}