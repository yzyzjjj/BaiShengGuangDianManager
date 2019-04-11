using ApiDeviceManagement.Base.Server;
using ApiDeviceManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiDeviceManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class HardwareLibraryController : ControllerBase
    {

        // GET: api/HardwareLibrary
        [HttpGet]
        public DataResult GetHardwareLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<HardwareLibrary>("SELECT * FROM `hardware_library` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/HardwareLibrary/5
        [HttpGet("{id}")]
        public DataResult GetHardwareLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<HardwareLibrary>("SELECT * FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.HardwareLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/HardwareLibrary/5
        [HttpPut("{id}")]
        public Result PutHardwareLibrary([FromRoute] int id, [FromBody] HardwareLibrary hardwareLibrary)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT * FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }

            hardwareLibrary.Id = id;
            hardwareLibrary.CreateUserId = Request.GetIdentityInformation();
            hardwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE hardware_library SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `HardwareName` = @HardwareName, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `DacNumber` = @DacNumber, " +
                "`AdcNumber` = @AdcNumber, `AxisNumber` = @AxisNumber, `ComNumber` = @ComNumber, `Description` = @Description WHERE `Id` = @Id;", hardwareLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/HardwareLibrary
        [HttpPost]
        public Result PostHardwareLibrary([FromBody] HardwareLibrary hardwareLibrary)
        {
            hardwareLibrary.CreateUserId = Request.GetIdentityInformation();
            hardwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
              "INSERT INTO hardware_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `HardwareName`, `InputNumber`, `OutputNumber`, " +
              "`DacNumber`, `AdcNumber`, `AxisNumber`, `ComNumber`, `Description`) VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @HardwareName, " +
              "@InputNumber, @OutputNumber, @DacNumber, @AdcNumber, @AxisNumber, @ComNumber, @Description);",
              hardwareLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/HardwareLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteHardwareLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `hardware_library` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}