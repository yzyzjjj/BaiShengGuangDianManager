using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 脚本
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ScriptVersionController : ControllerBase
    {
        // GET: api/ScriptVersion
        [HttpGet]
        public DataResult GetScriptVersion()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/ScriptVersion/5
        [HttpGet("{id}")]
        public DataResult GetScriptVersion([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ScriptVersionNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 根据设备型号
        /// </summary>
        /// <param name="deviceModelId">根据设备型号</param>
        /// <returns></returns>
        // GET: api/ScriptVersion/DeviceModel/5
        [HttpGet("DeviceModel/{deviceModelId}")]
        public DataResult GetScriptVersionByDeviceModel([FromRoute] int deviceModelId)
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE FIND_IN_SET(@deviceModelId, DeviceModelId) AND `MarkedDelete` = 0;", new { deviceModelId }));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="scriptVersion"></param>
        /// <returns></returns>
        // PUT: api/ScriptVersion/Id/5
        [HttpPut("Id/{id}")]
        public Result PutScriptVersion([FromRoute] int id, [FromBody] ScriptVersion scriptVersion)
        {
            var data =
                ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE ScriptName = @ScriptName AND MarkedDelete = 0;", new { scriptVersion.ScriptName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!scriptVersion.ScriptName.IsNullOrEmpty() && data.ScriptName != scriptVersion.ScriptName)
                {
                    return Result.GenError<Result>(Error.ScriptVersionIsExist);
                }
            }

            scriptVersion.Id = id;
            scriptVersion.CreateUserId = Request.GetIdentityInformation();
            scriptVersion.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE script_version SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `DeviceModelId` = @DeviceModelId, `ScriptName` = @ScriptName, " +
                "`ValueNumber` = @ValueNumber, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `HeartPacket` = @HeartPacket WHERE `Id` = @Id;", scriptVersion);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ScriptVersion
        [HttpPost]
        public Result PostScriptVersion([FromBody] ScriptVersion scriptVersion)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE ScriptName = @ScriptName AND MarkedDelete = 0;", new { scriptVersion.ScriptName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            scriptVersion.CreateUserId = createUserId;
            scriptVersion.MarkedDateTime = time;
            var valN = scriptVersion.ValueNumber == 0 ? 300 : scriptVersion.ValueNumber;
            var inN = scriptVersion.InputNumber == 0 ? 255 : scriptVersion.InputNumber;
            var outN = scriptVersion.OutputNumber == 0 ? 255 : scriptVersion.OutputNumber;
            var msg = new DeviceInfoMessagePacket(valN, inN, outN);
            scriptVersion.HeartPacket = msg.Serialize();
            var index = ServerConfig.ApiDb.Query<int>(
                 "INSERT INTO script_version (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`) " +
                 "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket);SELECT LAST_INSERT_ID();",
                 scriptVersion).FirstOrDefault();

            var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT a.Id VariableNameId, IFNULL(b.DictionaryId, 0) DictionaryId, IFNULL(b.VariableTypeId, 0) VariableTypeId FROM `usually_dictionary_type` a LEFT JOIN (SELECT * FROM `usually_dictionary` WHERE ScriptId = 0) b ON a.Id = b.VariableNameId;");

            foreach (var usuallyDictionary in usuallyDictionaries)
            {
                usuallyDictionary.ScriptId = index;
                usuallyDictionary.CreateUserId = createUserId;
                usuallyDictionary.MarkedDateTime = time;
            }

            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableNameId`, `DictionaryId`, `VariableTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableNameId, @DictionaryId, @VariableTypeId);",
                usuallyDictionaries);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ScriptVersion/ScriptVersions
        [HttpPost("ScriptVersions")]
        public Result PostScriptVersion([FromBody] List<ScriptVersion> scriptVersions)
        {

            var names = scriptVersions.GroupBy(x => x.ScriptName).Select(x => x.Key);
            if (names.Any())
            {
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE ScriptName IN @ScriptName AND MarkedDelete = 0;", new { ScriptName = names }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.ScriptVersionIsExist);
                }
            }


            foreach (var scriptVersion in scriptVersions)
            {
                scriptVersion.CreateUserId = Request.GetIdentityInformation();
                scriptVersion.MarkedDateTime = DateTime.Now;
                var valN = scriptVersion.ValueNumber == 0 ? 300 : scriptVersion.ValueNumber;
                var inN = scriptVersion.InputNumber == 0 ? 255 : scriptVersion.InputNumber;
                var outN = scriptVersion.OutputNumber == 0 ? 255 : scriptVersion.OutputNumber;
                var msg = new DeviceInfoMessagePacket(valN, inN, outN);
                scriptVersion.HeartPacket = msg.Serialize();
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO script_version (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket);",
                scriptVersions);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ScriptVersion/5
        [HttpDelete("{id}")]
        public Result DeleteScriptVersion([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `script_version` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `usually_dictionary` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `ScriptId`= @Id;", new
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