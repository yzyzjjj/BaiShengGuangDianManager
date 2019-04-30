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
    /// 脚本 常用变量对应表
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuallyDictionaryController : ControllerBase
    {
        // GET: api/UsuallyDictionary
        [HttpGet]
        public DataResult GetUsuallyDictionary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DeviceDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/UsuallyDictionary/5
        [HttpGet("{id}")]
        public DataResult GetUsuallyDictionary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.DeviceDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.UsuallyDictionaryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="usuallyDictionary"></param>
        /// <returns></returns>
        // PUT: api/UsuallyDictionary/Id/5
        [HttpPut("Id/{id}")]
        public Result PutUsuallyDictionary([FromRoute] int id, [FromBody] UsuallyDictionary usuallyDictionary)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryNotExist);
            }

            usuallyDictionary.Id = id;
            usuallyDictionary.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "UPDATE data_name_dictionary SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `ScriptId` = @ScriptId, " +
                "`VariableTypeId` = @VariableTypeId, `PointerAddress` = @PointerAddress, `VariableName` = @VariableName WHERE `Id` = @Id;", usuallyDictionary);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyDictionary
        [HttpPost]
        public Result PostUsuallyDictionary([FromBody] UsuallyDictionary usuallyDictionary)
        {
            usuallyDictionary.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName);",
                usuallyDictionary);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyDictionary/UsuallyDictionaries
        [HttpPost("UsuallyDictionaries")]
        public Result PostUsuallyDictionary([FromBody] List<UsuallyDictionary> usuallyDictionaries)
        {
            foreach (var usuallyDictionary in usuallyDictionaries)
            {
                usuallyDictionary.CreateUserId = Request.GetIdentityInformation();
                usuallyDictionary.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.DeviceDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName);",
                usuallyDictionaries);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/UsuallyDictionary/5
        [HttpDelete("{id}")]
        public Result DeleteUsuallyDictionary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.DeviceDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryNotExist);
            }

            ServerConfig.DeviceDb.Execute(
                "UPDATE `usually_dictionary` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

    }
}