using ApiDeviceManagement.Base.Server;
using ApiDeviceManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;

namespace ApiDeviceManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]  //根据需要可添加角色和自定义授权
    public class DeviceCategoryController : ControllerBase
    {
        // GET: api/DeviceCategory
        [HttpGet]
        public DataResult GetDeviceCategory()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<DeviceCategory>("SELECT * FROM `device_category` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/DeviceCategory/5
        [HttpGet("{id}")]
        public DataResult GetDeviceCategory([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<DeviceCategory>("SELECT * FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DeviceCategoryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/DeviceCategory/5
        [HttpPut("{id}")]
        public Result PutDeviceCategory([FromRoute] int id, [FromBody] DeviceCategory deviceCategory)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceCategoryNotExist);
            }

            deviceCategory.Id = id;
            deviceCategory.CreateUserId = Request.GetIdentityInformation();
            deviceCategory.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE device_category SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `CategoryName` = @CategoryName, `Description` = @Description WHERE `Id` = @Id;", deviceCategory);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceCategory
        [HttpPost]
        public Result PostDeviceCategory([FromBody] DeviceCategory deviceCategory)
        {
            deviceCategory.CreateUserId = Request.GetIdentityInformation();
            deviceCategory.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO device_category (`Id`, `CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `CategoryName`, `Description`) " +
                "VALUES (@Id, @CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @CategoryName, @Description);",
                deviceCategory);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceCategory/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceCategory([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceCategoryNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `device_category` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}