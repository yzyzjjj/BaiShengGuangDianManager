﻿using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 脚本 常用变量对应类型表
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuallyDictionaryTypeController : ControllerBase
    {
        // GET: api/UsuallyDictionaryType
        [HttpGet]
        public DataResult GetUsuallyDictionaryType()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<UsuallyDictionaryType>("SELECT * FROM `usually_dictionary_type` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/UsuallyDictionaryType/Statistic
        [HttpGet("Statistic")]
        public DataResult GetUsuallyDictionaryStatistics()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<UsuallyDictionaryType>("SELECT * FROM `usually_dictionary_type` WHERE `MarkedDelete` = 0 AND IsStatistic = 1;"));
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/UsuallyDictionaryType/5
        [HttpGet("{id}")]
        public DataResult GetUsuallyDictionaryType([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<UsuallyDictionaryType>("SELECT * FROM `usually_dictionary_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.UsuallyDictionaryTypeNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="usuallyDictionaryType"></param>
        /// <returns></returns>
        // PUT: api/UsuallyDictionaryType/Id/5
        [HttpPut("Id/{id}")]
        public Result PutUsuallyDictionaryType([FromRoute] int id, [FromBody] UsuallyDictionaryType usuallyDictionaryType)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryTypeNotExist);
            }

            usuallyDictionaryType.Id = id;
            usuallyDictionaryType.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionaryType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE usually_dictionary_type SET `MarkedDateTime` = @MarkedDateTime, " +
                "`MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, `VariableName` = @VariableName WHERE `Id` = @Id;", usuallyDictionaryType);
            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/UsuallyDictionaryType
        [HttpPost]
        public Result PostUsuallyDictionaryType([FromBody] UsuallyDictionaryType usuallyDictionaryType)
        {
            usuallyDictionaryType.CreateUserId = Request.GetIdentityInformation();
            usuallyDictionaryType.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO usually_dictionary_type (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `VariableName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @VariableName);",
                usuallyDictionaryType);

            ServerConfig.RedisHelper.PublishToTable();
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/UsuallyDictionaryType/5
        [HttpDelete("{id}")]
        public Result DeleteUsuallyDictionaryType([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `usually_dictionary_type` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.UsuallyDictionaryTypeNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `usually_dictionary_type` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
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