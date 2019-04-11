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
    public class DeviceModelController : ControllerBase
    {
        // GET: api/DeviceModels
        [HttpGet]
        public DataResult GetDeviceModel()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<DeviceModelDetail>("SELECT a.*, b.CategoryName FROM `device_model` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0;"));
            return result;
        }

        // GET: api/DeviceModels/5
        [HttpGet("{id}")]
        public DataResult GetDeviceModel([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<DeviceModelDetail>("SELECT a.*, b.CategoryName FROM `device_model` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.Id = @id AND a.MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceModelNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/DeviceModels/5
        [HttpPut("{id}")]
        public Result PutDeviceModel([FromRoute] int id, [FromBody] DeviceModel deviceModel)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            deviceModel.Id = id;
            deviceModel.CreateUserId = Request.GetIdentityInformation();
            deviceModel.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE device_model SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `DeviceCategoryId` = @DeviceCategoryId, `ModelName` = @ModelName, `Description` = @Description WHERE `Id` = @Id;", deviceModel);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceLibrary
        [HttpPost]
        public Result PostDeviceModel([FromBody] DeviceModel deviceModel)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceModel.DeviceCategoryId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceCategoryNotExist);
            }

            deviceModel.CreateUserId = Request.GetIdentityInformation();
            deviceModel.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO device_model (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCategoryId`, `ModelName`, `Description`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCategoryId, @ModelName, @Description);",
                deviceModel);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceModels/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceModel([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `device_model` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}