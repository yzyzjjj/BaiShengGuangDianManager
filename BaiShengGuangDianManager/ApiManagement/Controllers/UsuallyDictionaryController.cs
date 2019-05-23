using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers
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
            result.datas.AddRange(ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        /// <summary>
        /// 脚本版本自增Id
        /// </summary>
        /// <param name="scriptId">自增Id</param>
        /// <returns></returns>
        // GET: api/UsuallyDictionary/5
        [HttpGet("{scriptId}")]
        public DataResult GetUsuallyDictionary([FromRoute] int scriptId)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = scriptId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.ScriptVersionNotExist);
            }
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<UsuallyDictionaryDetail>("SELECT a.Id, a.VariableNameId, b.VariableName, a.DictionaryId, IFNULL(c.Id, 0) Did, a.VariableTypeId, d.TypeName FROM ( SELECT Id, ScriptId, VariableNameId, DictionaryId,  IF ( VariableTypeId = 0, 1, VariableTypeId ) VariableTypeId FROM `usually_dictionary` WHERE ScriptId = @ScriptId AND MarkedDelete = 0 ) a LEFT JOIN usually_dictionary_type b ON a.VariableNameId = b.Id LEFT JOIN data_name_dictionary c ON a.ScriptId = c.ScriptId AND a.DictionaryId = c.PointerAddress AND a.VariableTypeId = c.VariableTypeId LEFT JOIN variable_type d ON a.VariableTypeId = d.Id ORDER BY a.Id;", new { ScriptId = scriptId }));
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryNotExist);
            }

            usuallyDictionary.Id = id;
            usuallyDictionary.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE usually_dictionary SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = " +
                "@ModifyId, `ScriptId` = @ScriptId, `VariableNameId` = @VariableNameId, `DictionaryId` = @DictionaryId, `VariableTypeId` = @VariableTypeId WHERE `Id` = @Id;",
                usuallyDictionary);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 更新 常用变量
        /// </summary>
        /// <param name="usuallyDictionaries"></param>
        /// <returns></returns>
        // PUT: api/UsuallyDictionary
        [HttpPut]
        public Result PutUsuallyDictionaryByScript([FromBody] List<UsuallyDictionary> usuallyDictionaries)
        {
            var scriptIds = usuallyDictionaries.GroupBy(x => x.ScriptId).Select(x => x.Key);
            if (scriptIds.Count() != 1)
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var scriptId = scriptIds.First();
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = scriptId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ScriptVersionNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            var exist = ServerConfig.ApiDb.Query<UsuallyDictionary>("SELECT * FROM `usually_dictionary` WHERE `MarkedDelete` = 0 AND ScriptId = @ScriptId;", new { ScriptId = scriptId });


            var add = usuallyDictionaries.Where(x => exist.All(y => y.Id == x.Id));
            foreach (var usuallyDictionary in add)
            {
                usuallyDictionary.CreateUserId = createUserId;
                usuallyDictionary.MarkedDateTime = time;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableNameId`, `DictionaryId`, `VariableTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableNameId, @DictionaryId, @VariableTypeId);",
                add);

            var update = usuallyDictionaries.Where(x => exist.Any(y => y.Id == x.Id && (y.VariableNameId != x.VariableNameId || y.DictionaryId != x.DictionaryId || y.VariableTypeId != x.VariableTypeId))).ToList();
            foreach (var usuallyDictionary in update)
            {
                usuallyDictionary.CreateUserId = createUserId;
                usuallyDictionary.MarkedDateTime = time;
            }

            ServerConfig.ApiDb.Execute(
                    "UPDATE usually_dictionary SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = " +
                    "@ModifyId, `ScriptId` = @ScriptId, `VariableNameId` = @VariableNameId, `DictionaryId` = @DictionaryId, `VariableTypeId` = @VariableTypeId WHERE `Id` = @Id;",
                update);
            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyDictionary
        [HttpPost]
        public Result PostUsuallyDictionary([FromBody] UsuallyDictionary usuallyDictionary)
        {
            usuallyDictionary.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableNameId`, `DictionaryId`, `VariableTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableNameId, @DictionaryId, @VariableTypeId);",
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
            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableNameId`, `DictionaryId`, `VariableTypeId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableNameId, @DictionaryId, @VariableTypeId);",
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `usually_dictionary` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
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