using System;
using System.Collections.Generic;
using System.Linq;
using ApiDeviceManagement.Base.Server;
using ApiDeviceManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiDeviceManagement.Controllers
{
    /// <summary>
    /// 变量类型
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class VariableTypeController : ControllerBase
    {
        // GET: api/VariableType
        [HttpGet]
        public DataResult GetVariableType()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<VariableType>("SELECT * FROM `variable_type` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/VariableType/5
        [HttpGet("{id}")]
        public DataResult GetVariableType([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<VariableType>("SELECT * FROM `variable_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.VariableTypeNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="variableType"></param>
        /// <returns></returns>
        // PUT: api/VariableType/Id/5
        [HttpPut("Id/{id}")]
        public Result PutVariableType([FromRoute] int id, [FromBody] VariableType variableType)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `variable_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.VariableTypeNotExist);
            }

            variableType.Id = id;
            variableType.CreateUserId = Request.GetIdentityInformation();
            variableType.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE variable_type SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`TypeName` = @TypeName WHERE `Id` = @Id;", variableType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/VariableType
        [HttpPost]
        public Result PostVariableType([FromBody] VariableType variableType)
        {
            variableType.CreateUserId = Request.GetIdentityInformation();
            variableType.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO variable_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `TypeName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @TypeName);",
                variableType);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/VariableType/VariableTypes
        [HttpPost("VariableTypes")]
        public Result PostVariableType([FromBody] List<VariableType> variableTypes)
        {
            foreach (var variableType in variableTypes)
            {
                variableType.CreateUserId = Request.GetIdentityInformation();
                variableType.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO variable_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `TypeName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @TypeName);",
                variableTypes);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/VariableType/5
        [HttpDelete("{id}")]
        public Result DeleteVariableType([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `variable_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.VariableTypeNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `variable_type` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}