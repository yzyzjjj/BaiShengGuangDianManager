using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;

namespace ApiManagement.Controllers.DeviceManagementController
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]  //根据需要可添加角色和自定义授权
    public class DeviceClassController : ControllerBase
    {
        // GET: api/DeviceClass
        [HttpGet]
        public DataResult GetDeviceClass([FromQuery] bool menu, int qId)
        {
            var result = new DataResult();
            if (menu)
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, Class FROM `device_class` WHERE `MarkedDelete` = 0{(qId == 0 ? "" : " AND Id = @qId")};", new { qId }));
            }
            else
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceClass>($"SELECT * FROM `device_class` WHERE `MarkedDelete` = 0{(qId == 0 ? "" : " AND Id = @qId")};", new { qId }));
            }
            return result;
        }

        // PUT: api/DeviceClass/5
        [HttpPut("{id}")]
        public Result PutDeviceClass([FromBody] DeviceClass deviceClass)
        {
            var data =
                ServerConfig.ApiDb.Query<DeviceClass>("SELECT * FROM `device_class` WHERE Id = @Id AND MarkedDelete = 0;", new { deviceClass.Id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceClassNotExist);
            }

            if (!deviceClass.Class.IsNullOrEmpty() && data.Class != deviceClass.Class)
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_class` WHERE Id != @Id AND Class = @Class AND MarkedDelete = 0;",
                        new { deviceClass.Id, deviceClass.Class }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.DeviceClassIsExist);
                }
            }

            deviceClass.CreateUserId = Request.GetIdentityInformation();
            deviceClass.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE device_class SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `Class` = @Class, `Description` = @Description WHERE `Id` = @Id;", deviceClass);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceClass
        [HttpPost]
        public Result PostDeviceClass([FromBody] DeviceClass deviceClass)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_class` WHERE Class = @Class AND MarkedDelete = 0;", new { deviceClass.Class }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceClassIsExist);
            }
            deviceClass.CreateUserId = Request.GetIdentityInformation();
            deviceClass.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO device_class (`CreateUserId`, `MarkedDateTime`, `Class`, `Description`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Class, @Description);",
                deviceClass);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceClass/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceClass([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_class` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceClassNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE ClassId = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceModelUseDeviceClass);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `device_class` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}