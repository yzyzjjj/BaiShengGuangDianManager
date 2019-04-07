using ApiDataDictionaryManagement.Base.Server;
using ApiDataDictionaryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.ServerConfig.Enum;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiDataDictionaryManagement.Controllers
{
    /// <summary>
    /// 变量配置
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataNameDictionaryController : ControllerBase
    {
        // GET: api/DataNameDictionary
        [HttpGet]
        public DataResult GetDataNameDictionary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.DictionaryDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary`;"));
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
                ServerConfig.DictionaryDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE Id = @id;", new { id }).FirstOrDefault();
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
            result.datas.AddRange(ServerConfig.DictionaryDb.Query<DataNameDictionary>("SELECT * FROM `data_name_dictionary` WHERE ScriptId = @id;", new { id }));
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
                ServerConfig.DictionaryDb.Query<int>("SELECT COUNT(1) FROM `data_name_dictionary` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DataNameDictionaryNotExist);
            }

            dataNameDictionary.Id = id;
            dataNameDictionary.CreateUserId = Request.GetIdentityInformation();
            dataNameDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.DictionaryDb.Execute(
                "UPDATE data_name_dictionary SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ScriptId` = @ScriptId, `VariableTypeId` = @VariableTypeId, `PointerAddress` = @PointerAddress, `VariableName` = @VariableName WHERE `Id` = @Id;", dataNameDictionary);

            return Result.GenError<Result>(Error.Success);
        }





        // POST: api/DataNameDictionary
        [HttpPost]
        public Result PostDataNameDictionary([FromBody] DataNameDictionary dataNameDictionary)
        {
            dataNameDictionary.CreateUserId = Request.GetIdentityInformation();
            dataNameDictionary.MarkedDateTime = DateTime.Now;
            ServerConfig.DictionaryDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName);",
                dataNameDictionary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/DataNameDictionary/DataNameDictionarys
        [HttpPost("DataNameDictionarys")]
        public Result PostDataNameDictionary([FromBody] List<DataNameDictionary> dataNameDictionarys)
        {
            foreach (var dataNameDictionary in dataNameDictionarys)
            {
                dataNameDictionary.CreateUserId = Request.GetIdentityInformation();
                dataNameDictionary.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.DictionaryDb.Execute(
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName);",
                dataNameDictionarys);

            return Result.GenError<Result>(Error.Success);
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
                ServerConfig.DictionaryDb.Query<int>("SELECT COUNT(1) FROM `data_name_dictionary` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.DataNameDictionaryNotExist);
            }

            ServerConfig.DictionaryDb.Execute(
                "UPDATE `data_name_dictionary` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}