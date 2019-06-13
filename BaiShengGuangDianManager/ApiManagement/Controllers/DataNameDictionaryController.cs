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
    /// 变量配置
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class DataNameDictionaryController : ControllerBase
    {
        // GET: api/DataNameDictionary
        [HttpGet]
        public DataResult GetDataNameDictionary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/DataNameDictionary/5
        [HttpGet("{id}")]
        public DataResult GetDataNameDictionary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.DataNameDictionaryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 脚本版本
        /// </summary>
        /// <returns></returns>
        // GET: api/DataNameDictionary/ScriptId/5
        [HttpGet("ScriptId/{id}")]
        public DataResult GetDataNameDictionaryByScriptId([FromRoute] int id)
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE ScriptId = @id AND `MarkedDelete` = 0;", new { id }));
            return result;
        }


        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="dataNameDictionary"></param>
        /// <returns></returns>
        // PUT: api/DataNameDictionary/Id/5
        [HttpPut("Id/{id}")]
        public Result PutDataNameDictionary([FromRoute] int id, [FromBody] DataNameDictionary dataNameDictionary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `data_name_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DataNameDictionaryNotExist);
            }

            dataNameDictionary.Id = id;
            dataNameDictionary.CreateUserId = Request.GetIdentityInformation();
            dataNameDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE data_name_dictionary SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ScriptId` = @ScriptId, `VariableTypeId` = @VariableTypeId, `PointerAddress` = @PointerAddress, `VariableName` = @VariableName, `Remark` = @Remark WHERE `Id` = @Id;", dataNameDictionary);

            return Result.GenError<Result>(Error.Success);
        }





        // POST: api/DataNameDictionary
        [HttpPost]
        public Result PostDataNameDictionary([FromBody] DataNameDictionary dataNameDictionary)
        {
            dataNameDictionary.CreateUserId = Request.GetIdentityInformation();
            dataNameDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName, @Remark);",
                dataNameDictionary);
            CheckScriptVersion(dataNameDictionary.ScriptId);
            return Result.GenError<Result>(Error.Success);
        }

        public class AddDataNameDictionariey
        {
            //0 输入  1  json  2 excel  3 sql
            public int Type;
            //设备型号
            public string DeviceModelId;
            //数据类型 1 变量 2 输入口 3 输出口
            public int VariableType;

            //0 输入 脚本ID
            public int ScriptId;
            public string ScriptName;
            public List<DataNameDictionary> DataNameDictionaries;
        }
        // POST: api/DataNameDictionary/DataNameDictionarys
        [HttpPost("DataNameDictionarys")]
        public Result PostDataNameDictionary([FromBody] AddDataNameDictionariey data)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_model` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = data.DeviceModelId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DeviceModelNotExist);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `variable_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = data.VariableType }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.VariableTypeNotExist);
            }

            switch (data.Type)
            {
                case 0:
                    cnt =
                        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = data.ScriptId }).FirstOrDefault();
                    if (cnt == 0)
                    {
                        return Result.GenError<Result>(Error.ScriptVersionNotExist);
                    }

                    foreach (var dataNameDictionary in data.DataNameDictionaries)
                    {
                        dataNameDictionary.ScriptId = data.ScriptId;
                        dataNameDictionary.VariableTypeId = data.VariableType;
                    }

                    break;
                case 1:
                    if (data.ScriptName.IsNullOrEmpty())
                    {
                        return Result.GenError<Result>(Error.ParamError);
                    }
                    var scv =
                        ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE ScriptName = @ScriptName AND `MarkedDelete` = 0;", new { data.ScriptName }).FirstOrDefault();
                    int index;
                    if (scv == null)
                    {
                        var scriptVersion = new ScriptVersion
                        {
                            CreateUserId = Request.GetIdentityInformation(),
                            MarkedDateTime = DateTime.Now,
                            DeviceModelId = data.DeviceModelId,
                            ScriptName = data.ScriptName
                        };
                        index = ServerConfig.ApiDb.Query<int>(
                          "INSERT INTO script_version (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`) " +
                          "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket);SELECT LAST_INSERT_ID();",
                          scriptVersion).FirstOrDefault();
                    }
                    else
                    {
                        index = scv.Id;
                    }

                    if (index == 0)
                    {
                        return Result.GenError<Result>(Error.ParamError);
                    }
                    data.ScriptId = index;
                    break;
                case 2: break;
                case 3: break;
                default:
                    return Result.GenError<Result>(Error.Fail);
            }

            var dataNameDictionaries = data.DataNameDictionaries;
            if (!dataNameDictionaries.Any())
            {
                return Result.GenError<Result>(Error.Success);
            }

            var createUserId = Request.GetIdentityInformation();
            foreach (var dataNameDictionary in dataNameDictionaries)
            {
                dataNameDictionary.CreateUserId = createUserId;
                dataNameDictionary.MarkedDateTime = DateTime.Now;
                dataNameDictionary.ScriptId = data.ScriptId;
                dataNameDictionary.VariableTypeId = data.VariableType;
            }

            var doublePa = dataNameDictionaries.GroupBy(x => x.PointerAddress).Count(x => x.Count() > 1);
            if (doublePa > 0)
            {
                return Result.GenError<Result>(Error.PointerAddressIsExist);
            }

            var dna = dataNameDictionaries.Select(x => x.PointerAddress);
            if (dna.Any())
            {
                cnt = ServerConfig.ApiDb.Query<int>(
                    "SELECT COUNT(1) FROM `data_name_dictionary` WHERE ScriptId = @ScriptId AND VariableTypeId = @VariableTypeId AND PointerAddress IN @PointerAddress;",
                    new
                    {
                        ScriptId = data.ScriptId,
                        VariableTypeId = data.VariableType,
                        PointerAddress = dna
                    }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<Result>(Error.PointerAddressIsExist);
                }
            }

            ServerConfig.ApiDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName, @Remark);",
                dataNameDictionaries);

            CheckScriptVersion(data.ScriptId);
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

            var valN = scriptVersion.ValueNumber < 300 ? 300 : scriptVersion.ValueNumber;
            var inN = scriptVersion.InputNumber < 255 ? 255 : scriptVersion.InputNumber;
            var outN = scriptVersion.OutputNumber < 255 ? 255 : scriptVersion.OutputNumber;
            var msg = new DeviceInfoMessagePacket(valN, inN, outN);
            var heartPacket = msg.Serialize();
            var oldHeartPacket = scriptVersion.HeartPacket;
            var notify = heartPacket != scriptVersion.HeartPacket;
            scriptVersion.HeartPacket = heartPacket;

            scriptVersion.CreateUserId = Request.GetIdentityInformation();
            scriptVersion.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE script_version SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `DeviceModelId` = @DeviceModelId, `ScriptName` = @ScriptName, " +
                "`ValueNumber` = @ValueNumber, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `HeartPacket` = @HeartPacket WHERE `Id` = @Id;", scriptVersion);

            ServerConfig.ApiDb.Execute(
                "UPDATE npc_proxy_link SET `Instruction` = @HeartPacket WHERE `Instruction` = @oldHeartPacket;", new { oldHeartPacket, scriptVersion.HeartPacket });
            ServerConfig.RedisHelper.PublishToTable();
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/DataNameDictionary/5
        [HttpDelete("{id}")]
        public Result DeleteDataNameDictionary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `data_name_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DataNameDictionaryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `data_name_dictionary` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}