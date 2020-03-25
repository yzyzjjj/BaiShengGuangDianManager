using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产检验配置单
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureCheckController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu">下拉框</param>
        /// <returns></returns>
        // GET: api/ManufactureCheck?qId=0&item=false
        [HttpGet]
        public DataResult GetManufactureCheck([FromQuery] int qId, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Check` FROM `manufacture_check` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    $"SELECT * FROM `manufacture_check` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<ManufactureCheck>(sql, new { qId });
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureCheckNotExist);
            }
            return result;
        }

        // PUT: api/ManufactureCheck
        [HttpPut]
        public Result PutManufactureCheck([FromBody] ManufactureCheck manufactureCheck)
        {
            if (manufactureCheck.Id == 0)
            {
                return Result.GenError<Result>(Error.ManufactureCheckNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var manufactureCheckOld =
                ServerConfig.ApiDb.Query<ManufactureCheck>("SELECT * FROM `manufacture_check` WHERE Id = @Id AND MarkedDelete = 0;",
                    new { manufactureCheck.Id }).FirstOrDefault();
            if (manufactureCheckOld == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureCheckNotExist);
            }
            manufactureCheck.Check = manufactureCheck.Check ?? manufactureCheckOld.Check;
            if (manufactureCheckOld.Check != manufactureCheck.Check)
            {
                if (manufactureCheck.Check.IsNullOrEmpty())
                {
                    return Result.GenError<Result>(Error.ManufactureCheckNotEmpty);
                }
                manufactureCheck.MarkedDateTime = markedDateTime;
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_check SET `MarkedDateTime` = @MarkedDateTime, `Check` = @Check WHERE `Id` = @Id;", manufactureCheck);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ManufactureCheck
        [HttpPost]
        public DataResult PostManufactureCheck([FromBody] ManufactureCheckCopy manufactureCheck)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check` WHERE `Check` = @Check AND MarkedDelete = 0;",
                    new { manufactureCheck.Check }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureCheckIsExist);
            }

            IEnumerable<ManufactureCheckItem> manufactureCheckItems = null;
            if (manufactureCheck.CopyId != 0)
            {
                cnt =
                   ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check` WHERE `Id` = @CopyId AND MarkedDelete = 0;",
                       new { manufactureCheck.CopyId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.ManufactureCheckNotExist);
                }
                manufactureCheckItems =
                    ServerConfig.ApiDb.Query<ManufactureCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @CopyId AND `MarkedDelete` = 0;", new { manufactureCheck.CopyId });
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            manufactureCheck.CreateUserId = createUserId;
            manufactureCheck.MarkedDateTime = markedDateTime;
            manufactureCheck.Check = manufactureCheck.Check ?? "";
            manufactureCheck.Id = ServerConfig.ApiDb.Query<int>(
                "INSERT INTO manufacture_check (`CreateUserId`, `MarkedDateTime`, `Check`) VALUES (@CreateUserId, @MarkedDateTime, @Check);SELECT LAST_INSERT_ID();", manufactureCheck).FirstOrDefault();

            if (manufactureCheckItems != null && manufactureCheckItems.Any())
            {
                foreach (var manufactureCheckItem in manufactureCheckItems)
                {
                    manufactureCheckItem.CreateUserId = createUserId;
                    manufactureCheckItem.MarkedDateTime = markedDateTime;
                    manufactureCheckItem.CheckId = manufactureCheck.Id;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO manufacture_check_item (`CreateUserId`, `MarkedDateTime`, `CheckId`, `Item`, `Method`) VALUES (@CreateUserId, @MarkedDateTime, @CheckId, @Item, @Method);",
                    manufactureCheckItems);
            }

            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/ManufactureCheck
        /// <summary>
        /// 单个删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public Result DeleteManufactureCheck([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureCheckNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_check` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_check_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `CheckId` = @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            return Result.GenError<Result>(Error.Success);
        }
    }
}