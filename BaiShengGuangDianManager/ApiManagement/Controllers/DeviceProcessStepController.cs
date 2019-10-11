using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Linq;

namespace ApiManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceProcessStepController : ControllerBase
    {
        // GET: api/DeviceProcessStep
        [HttpGet]
        public DataResult GetDeviceProcessStep()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<DeviceProcessStepDetail>("SELECT a.*, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 ORDER BY a.DeviceCategoryId, a.Id;"));
            return result;
        }

        // GET: api/DeviceProcessStep/5
        [HttpGet("{id}")]
        public DataResult GetDeviceProcessStep([FromRoute] int id)
        {
            var data =
                ServerConfig.ApiDb.Query<DeviceProcessStepDetail>("SELECT a.*, b.CategoryName FROM `device_process_step` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id WHERE a.MarkedDelete = 0 AND a.Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<DataResult>(Error.DeviceProcessStepNotExist);
            }
            var result = new DataResult();
            result.datas.Add(data);
            return result;
        }

        // PUT: api/DeviceProcessStep/5
        [HttpPut("{id}")]
        public Result PutDeviceProcessStep([FromRoute] int id, [FromBody] DeviceProcessStep deviceProcessStep)
        {
            var cnt =
                 ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceProcessStep.DeviceCategoryId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceCategoryNotExist);
            }

            var data =
                ServerConfig.ApiDb.Query<DeviceProcessStep>("SELECT * FROM `device_process_step` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.DeviceProcessStepNotExist);
            }

            cnt =
               ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_process_step` WHERE DeviceCategoryId = @DeviceCategoryId AND StepName = @StepName AND IsSurvey = @IsSurvey AND `MarkedDelete` = 0 AND Id != @id;",
                   new { deviceProcessStep.DeviceCategoryId, deviceProcessStep.StepName, deviceProcessStep.IsSurvey, id }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceProcessStepIsExist);
            }

            deviceProcessStep.Id = id;
            deviceProcessStep.CreateUserId = Request.GetIdentityInformation();
            deviceProcessStep.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE device_process_step SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`DeviceCategoryId` = @DeviceCategoryId, `StepName` = @StepName, `Description` = @Description, `IsSurvey` = @IsSurvey WHERE `Id` = @Id;", deviceProcessStep);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DeviceLibrary
        [HttpPost]
        public Result PostDeviceProcessStep([FromBody] DeviceProcessStep deviceProcessStep)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_category` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = deviceProcessStep.DeviceCategoryId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_process_step` WHERE DeviceCategoryId = @DeviceCategoryId AND StepName = @StepName AND IsSurvey = @IsSurvey AND `MarkedDelete` = 0;",
                    new { deviceProcessStep.DeviceCategoryId, deviceProcessStep.StepName, deviceProcessStep.IsSurvey }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.DeviceProcessStepIsExist);
            }

            deviceProcessStep.CreateUserId = Request.GetIdentityInformation();
            deviceProcessStep.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO device_process_step (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceCategoryId`, `StepName`, `Description`, `IsSurvey`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceCategoryId, @StepName, @Description, @IsSurvey);",
                deviceProcessStep);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/DeviceProcessStep/5
        [HttpDelete("{id}")]
        public Result DeleteDeviceProcessStep([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_process_step` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceProcessStepNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `device_process_step` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}