using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产任务模块
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ManufactureTaskModuleController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu">下拉框</param>
        /// <returns></returns>
        // GET: api/ManufactureTaskModule?qId=0&item=false
        [HttpGet]
        public DataResult GetManufactureTaskModule([FromQuery] int qId, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Module`, `IsCheck` FROM `manufacture_task_module` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    $"SELECT * FROM `manufacture_task_module` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<ManufactureTaskModule>(sql, new { qId });
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureTaskModuleNotExist);
            }
            return result;
        }

        // PUT: api/ManufactureTaskModule
        [HttpPut]
        public Result PutManufactureTaskModule([FromBody] ManufactureTaskModule manufactureTaskModule)
        {
            if (manufactureTaskModule.Id == 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskModuleNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var manufactureTaskModuleOld =
                ServerConfig.ApiDb.Query<ManufactureTaskModule>("SELECT * FROM `manufacture_task_module` WHERE Id = @Id AND MarkedDelete = 0;",
                    new { manufactureTaskModule.Id }).FirstOrDefault();
            if (manufactureTaskModuleOld == null)
            {
                return Result.GenError<Result>(Error.ManufactureTaskModuleNotExist);
            }

            manufactureTaskModule.Module = manufactureTaskModule.Module ?? manufactureTaskModuleOld.Module;
            if (manufactureTaskModuleOld.Module != manufactureTaskModule.Module || manufactureTaskModuleOld.IsCheck != manufactureTaskModule.IsCheck)
            {
                manufactureTaskModule.MarkedDateTime = markedDateTime;
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_task_module SET `MarkedDateTime` = @MarkedDateTime, `Module` = @Module, `IsCheck` = @IsCheck WHERE `Id` = @Id;", manufactureTaskModule);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ManufactureTaskModule
        [HttpPost]
        public Result PostManufactureTaskModule([FromBody] ManufactureTaskModule manufactureTaskModule)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task_module` WHERE `Module` = @Module AND MarkedDelete = 0;",
                    new { manufactureTaskModule.Module }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskModuleIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            manufactureTaskModule.CreateUserId = createUserId;
            manufactureTaskModule.MarkedDateTime = markedDateTime;
            manufactureTaskModule.Module = manufactureTaskModule.Module ?? "";
            ServerConfig.ApiDb.Execute("INSERT INTO manufacture_task_module (`CreateUserId`, `MarkedDateTime`, `Module`, `IsCheck`) VALUES (@CreateUserId, @MarkedDateTime, @Module, @IsCheck);", manufactureTaskModule);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ManufactureTaskModule
        /// <summary>
        /// 单个删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public Result DeleteManufactureTaskModule([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_task_module` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureTaskModuleNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_task_module` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            return Result.GenError<Result>(Error.Success);
        }
    }
}