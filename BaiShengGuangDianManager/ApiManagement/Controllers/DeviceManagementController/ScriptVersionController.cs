using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Control;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.DeviceManagementController
{
    /// <summary>
    /// 脚本
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ScriptVersionController : ControllerBase
    {
        // GET: api/ScriptVersion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="deviceModelId">根据设备型号</param>
        /// <param name="qId"></param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetScriptVersion([FromQuery] bool menu, int deviceModelId, int qId)
        {
            var result = new DataResult();
            if (deviceModelId != 0)
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE FIND_IN_SET(@deviceModelId, DeviceModelId) AND `MarkedDelete` = 0;", new { deviceModelId }));
                return result;
            }

            if (menu)
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<dynamic>($"SELECT Id, ScriptName FROM `script_version` WHERE {(qId == 0 ? "" : "Id = @qId AND ")} MarkedDelete = 0 ORDER BY Id;;"));
            }
            else
            {
                result.datas.AddRange(ServerConfig.ApiDb.Query<ScriptVersion>($"SELECT * FROM `script_version` WHERE {(qId == 0 ? "" : "Id = @qId AND ")} MarkedDelete = 0 ORDER BY Id;;"));
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.ScriptVersionNotExist;
                return result;
            }
            return result;
        }

        ///// <summary>
        ///// 自增Id
        ///// </summary>
        ///// <param name="id">自增Id</param>
        ///// <returns></returns>
        //// GET: api/ScriptVersion/5
        //[HttpGet("{id}")]
        //public DataResult GetScriptVersion([FromRoute] int id)
        //{
        //    var result = new DataResult();
        //    var data =
        //        ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
        //    if (data == null)
        //    {
        //        result.errno = Error.ScriptVersionNotExist;
        //        return result;
        //    }
        //    result.datas.Add(data);
        //    return result;
        //}

        /// <summary>
        /// 根据设备型号
        /// </summary>
        /// <param name="deviceModelId">根据设备型号</param>
        /// <returns></returns>
        // GET: api/ScriptVersion/DeviceModel/5
        //[HttpGet("DeviceModel/{deviceModelId}")]
        //public DataResult GetScriptVersionByDeviceModel([FromRoute] int deviceModelId)
        //{
        //    var result = new DataResult();
        //    result.datas.AddRange(ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE FIND_IN_SET(@deviceModelId, DeviceModelId) AND `MarkedDelete` = 0;", new { deviceModelId }));
        //    return result;
        //}

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
            scriptVersion.ScriptFile = scriptVersion.ScriptFile ?? "";
            ServerConfig.ApiDb.Execute(
                "UPDATE script_version SET `DeviceModelId` = @DeviceModelId, `ScriptName` = @ScriptName, `ScriptFile` = @ScriptFile WHERE `Id` = @Id;", scriptVersion);

            CheckScriptVersion(id);
            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        private void CheckScriptVersion(int scriptId)
        {
            var scriptVersion =
                ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = scriptId }).FirstOrDefault();
            if (scriptVersion == null)
            {
                return;
            }
            var dataNameDictionaries =
                ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE ScriptId = @ScriptId AND `MarkedDelete` = 0;", new { ScriptId = scriptId });
            if (!dataNameDictionaries.Any())
            {
                return;
            }

            var group = dataNameDictionaries.GroupBy(x => x.VariableTypeId).ToDictionary(x => x.Key, x => x.Count());
            var valueNumber = group.ContainsKey(1) ? group[1] : 0;
            var inputNumber = group.ContainsKey(2) ? group[2] : 0;
            var outputNumber = group.ContainsKey(3) ? group[3] : 0;
            scriptVersion.ValueNumber = scriptVersion.ValueNumber < valueNumber ? valueNumber : scriptVersion.ValueNumber;
            scriptVersion.InputNumber = scriptVersion.InputNumber < inputNumber ? inputNumber : scriptVersion.InputNumber;
            scriptVersion.OutputNumber = scriptVersion.OutputNumber < outputNumber ? outputNumber : scriptVersion.OutputNumber;

            var groupPA = dataNameDictionaries.GroupBy(x => x.VariableTypeId).ToDictionary(x => x.Key, x => x.Max(y => y.PointerAddress));
            var maxValuePointerAddress = groupPA.ContainsKey(1) ? groupPA[1] : 0;
            var maxInputPointerAddress = groupPA.ContainsKey(2) ? groupPA[2] : 0;
            var maxOutputPointerAddress = groupPA.ContainsKey(3) ? groupPA[3] : 0;
            scriptVersion.MaxValuePointerAddress = maxValuePointerAddress;
            scriptVersion.MaxInputPointerAddress = maxInputPointerAddress;
            scriptVersion.MaxOutputPointerAddress = maxOutputPointerAddress;

            var valN = maxValuePointerAddress < 300 ? 300 : maxValuePointerAddress;
            var inN = maxInputPointerAddress < 255 ? 255 : maxInputPointerAddress;
            var outN = maxOutputPointerAddress < 255 ? 255 : maxOutputPointerAddress;
            var msg = new DeviceInfoMessagePacket(valN, inN, outN);
            var heartPacket = msg.Serialize();
            var oldHeartPacket = scriptVersion.HeartPacket;
            var notify = heartPacket != scriptVersion.HeartPacket;
            scriptVersion.HeartPacket = heartPacket;

            scriptVersion.CreateUserId = Request.GetIdentityInformation();
            scriptVersion.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE script_version SET `ValueNumber` = @ValueNumber, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `MaxValuePointerAddress` = @MaxValuePointerAddress, " +
                "`MaxInputPointerAddress` = @MaxInputPointerAddress, `MaxOutputPointerAddress` = @MaxOutputPointerAddress, `HeartPacket` = @HeartPacket WHERE `Id` = @Id;", scriptVersion);

            ServerConfig.ApiDb.Execute(
                "UPDATE npc_proxy_link SET `Instruction` = @HeartPacket WHERE `Instruction` = @oldHeartPacket;", new { oldHeartPacket, scriptVersion.HeartPacket });
            ServerConfig.RedisHelper.PublishToTable();
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
            var valN = scriptVersion.ValueNumber == 0 ? 300 : scriptVersion.ValueNumber;
            var inN = scriptVersion.InputNumber == 0 ? 255 : scriptVersion.InputNumber;
            var outN = scriptVersion.OutputNumber == 0 ? 255 : scriptVersion.OutputNumber;
            var msg = new DeviceInfoMessagePacket(valN, inN, outN);
            scriptVersion.HeartPacket = msg.Serialize();
            scriptVersion.ScriptFile = scriptVersion.ScriptFile ?? "";
            var index = ServerConfig.ApiDb.Query<int>(
                "INSERT INTO script_version (`CreateUserId`, `MarkedDelete`, `ModifyId`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`, `ScriptFile`) " +
                 "VALUES (@CreateUserId, @MarkedDelete, @ModifyId, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket, @ScriptFile);SELECT LAST_INSERT_ID();",
                 scriptVersion).FirstOrDefault();

            var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT a.Id VariableNameId, IFNULL(b.DictionaryId, 0) DictionaryId, IFNULL(b.VariableTypeId, 0) VariableTypeId FROM `usually_dictionary_type` a LEFT JOIN (SELECT * FROM `usually_dictionary` WHERE ScriptId = 0) b ON a.Id = b.VariableNameId;");

            foreach (var usuallyDictionary in usuallyDictionaries)
            {
                usuallyDictionary.ScriptId = index;
                usuallyDictionary.CreateUserId = createUserId;
            }

            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableNameId`, `DictionaryId`, `VariableTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDelete, @ModifyId, @ScriptId, @VariableNameId, @DictionaryId, @VariableTypeId);",
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
                var valN = scriptVersion.ValueNumber == 0 ? 300 : scriptVersion.ValueNumber;
                var inN = scriptVersion.InputNumber == 0 ? 255 : scriptVersion.InputNumber;
                var outN = scriptVersion.OutputNumber == 0 ? 255 : scriptVersion.OutputNumber;
                var msg = new DeviceInfoMessagePacket(valN, inN, outN);
                scriptVersion.HeartPacket = msg.Serialize();
                scriptVersion.ScriptFile = scriptVersion.ScriptFile ?? "";
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO script_version (`CreateUserId`, `MarkedDelete`, `ModifyId`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`, `ScriptFile`) " +
                "VALUES (@CreateUserId, @MarkedDelete, @ModifyId, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket, @ScriptFile);",
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
                "UPDATE `script_version` SET `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `usually_dictionary` SET `MarkedDelete`= @MarkedDelete WHERE `ScriptId`= @Id;", new
                {
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

    }
}