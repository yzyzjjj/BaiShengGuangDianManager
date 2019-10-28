using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;
using ServiceStack;

namespace ApiManagement.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class HardwareLibraryController : ControllerBase
    {

        // GET: api/HardwareLibrary
        [HttpGet]
        public DataResult GetHardwareLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<HardwareLibrary>("SELECT * FROM `hardware_library` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/HardwareLibrary/5
        [HttpGet("{id}")]
        public DataResult GetHardwareLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<HardwareLibrary>("SELECT * FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
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
            var data =
                ServerConfig.ApiDb.Query<HardwareLibrary>("SELECT * FROM `hardware_library` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE HardwareName = @HardwareName AND MarkedDelete = 0;", new { hardwareLibrary.HardwareName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!hardwareLibrary.HardwareName.IsNullOrEmpty() && data.HardwareName != hardwareLibrary.HardwareName)
                {
                    return Result.GenError<Result>(Error.HardwareLibraryIsExist);
                }
            }

            hardwareLibrary.Id = id;
            hardwareLibrary.CreateUserId = Request.GetIdentityInformation();
            hardwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE hardware_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `HardwareName` = @HardwareName, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `DacNumber` = @DacNumber, " +
                "`AdcNumber` = @AdcNumber, `AxisNumber` = @AxisNumber, `ComNumber` = @ComNumber, `Description` = @Description WHERE `Id` = @Id;", hardwareLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/HardwareLibrary
        [HttpPost]
        public Result PostHardwareLibrary([FromBody] HardwareLibrary hardwareLibrary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE HardwareName = @HardwareName AND MarkedDelete = 0;", new { hardwareLibrary.HardwareName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryIsExist);
            }
            hardwareLibrary.CreateUserId = Request.GetIdentityInformation();
            hardwareLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `hardware_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.HardwareLibraryNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE HardwareId = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceLibraryUseHardware);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `hardware_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}