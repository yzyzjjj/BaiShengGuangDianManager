using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Linq;
namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 6s分组
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureGroupController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="menu">下拉框</param>
        /// <returns></returns>
        // GET: api/ManufactureGroup?qId=0&item=false&accountId=0
        [HttpGet]
        public DataResult GetManufactureGroup([FromQuery] int qId, bool menu)
        {
            var result = new DataResult();
            string sql;
            if (menu)
            {
                sql =
                    $"SELECT Id, `Group` FROM `manufacture_group` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<dynamic>(sql, new { qId });
                result.datas.AddRange(data);
            }
            else
            {
                sql =
                    $"SELECT * FROM `manufacture_group` WHERE {(qId == 0 ? "" : "Id = @qId AND ")}`MarkedDelete` = 0;";
                var data = ServerConfig.ApiDb.Query<ManufactureGroup>(sql, new { qId });
                result.datas.AddRange(data);
            }
            if (qId != 0 && !result.datas.Any())
            {
                return Result.GenError<DataResult>(Error.ManufactureGroupNotExist);
            }
            return result;
        }

        // PUT: api/ManufactureGroup
        [HttpPut]
        public Result PutManufactureGroup([FromBody] ManufactureGroup manufactureGroup)
        {
            if (manufactureGroup.Id == 0)
            {
                return Result.GenError<Result>(Error.ManufactureGroupNotExist);
            }

            var manufactureGroupOld =
                ServerConfig.ApiDb.Query<ManufactureGroup>("SELECT * FROM `manufacture_group` WHERE Id = @Id AND MarkedDelete = 0;",
                    new { manufactureGroup.Id }).FirstOrDefault();
            if (manufactureGroupOld == null)
            {
                return Result.GenError<DataResult>(Error.ManufactureGroupNotExist);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            var change = false;
            if (manufactureGroup.IsName)
            {
                var cnt = ServerConfig.ApiDb.Query<int>("SELECT * FROM `manufacture_group` WHERE `Group` = @Group AND MarkedDelete = 0;",
                      new { manufactureGroup.Group }).FirstOrDefault();
                if (cnt > 0)
                {
                    return Result.GenError<DataResult>(Error.ManufactureGroupIsExist);
                }
                manufactureGroup.Interval = manufactureGroupOld.Interval;
                manufactureGroup.ScoreTime = manufactureGroupOld.ScoreTime;
            }
            else
            {
                manufactureGroup.Group = manufactureGroupOld.Group;
                switch (manufactureGroup.Interval)
                {
                    case 0: break;
                    case 1: manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 7 + 1; break;
                    case 2: manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 28 + 1; break;
                    default: return Result.GenError<DataResult>(Error.ParamError);
                }
            }
            if (manufactureGroup.Group != null || manufactureGroup.Interval != manufactureGroupOld.Interval || manufactureGroup.ScoreTime != manufactureGroupOld.ScoreTime)
            {
                if (manufactureGroup.Group.IsNullOrEmpty())
                {
                    return Result.GenError<DataResult>(Error.ManufactureGroupNotEmpty);
                }
                if (manufactureGroupOld.Group != manufactureGroup.Group || manufactureGroupOld.Interval != manufactureGroup.Interval || manufactureGroupOld.ScoreTime != manufactureGroup.ScoreTime)
                {
                    change = true;
                    manufactureGroup.MarkedDateTime = markedDateTime;
                }
            }

            if (change)
            {
                ServerConfig.ApiDb.Execute(
                    "UPDATE manufacture_group SET `MarkedDateTime` = @MarkedDateTime, `Group` = @Group, `ScoreTime` = @ScoreTime, `Interval` = @Interval WHERE `Id` = @Id;", manufactureGroup);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ManufactureGroup
        [HttpPost]
        public DataResult PostManufactureGroup([FromBody] ManufactureGroup manufactureGroup)
        {
            if (manufactureGroup.Group.IsNullOrEmpty())
            {
                return Result.GenError<DataResult>(Error.ManufactureGroupNotEmpty);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_group` WHERE `Group` = @Group AND MarkedDelete = 0;",
                    new { manufactureGroup.Group }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error.ManufactureGroupIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            manufactureGroup.CreateUserId = createUserId;
            manufactureGroup.MarkedDateTime = markedDateTime;
            manufactureGroup.ScoreTime = manufactureGroup.ScoreTime != 0 ? manufactureGroup.ScoreTime : 1;
            switch (manufactureGroup.Interval)
            {
                case 0: break;
                case 1: manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 7 + 1; break;
                case 2: manufactureGroup.ScoreTime = (manufactureGroup.ScoreTime - 1) % 28 + 1; break;
                default: return Result.GenError<DataResult>(Error.ParamError);
            }
            ServerConfig.ApiDb.Execute("INSERT INTO manufacture_group (`CreateUserId`, `MarkedDateTime`, `Group`, `ScoreTime`, `Interval`) " +
                                       "VALUES (@CreateUserId, @MarkedDateTime, @Group, @ScoreTime, @Interval);", manufactureGroup);

            return Result.GenError<DataResult>(Error.Success);
        }

        // DELETE: api/ManufactureGroup
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteManufactureGroup([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_group` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureGroupNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_group` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_processor` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `GroupId` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}