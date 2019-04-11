using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiFlowCardManagement.Controllers
{
    /// <summary>
    /// 原材料
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class RawMateriaController : ControllerBase
    {

        // GET: api/RawMateria
        [HttpGet]
        public DataResult GetRawMateria()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.FlowcardDb.Query<RawMateria>("SELECT * FROM `raw_materia`;"));
            return result;
        }
        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/RawMateria/Id/5
        [HttpGet("Id/{id}")]
        public DataResult GetRawMateria([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowcardDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.RawMateriaNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName">原料批号</param>
        /// <returns></returns>
        // GET: api/RawMateria/RawMateriaName/5
        [HttpGet("RawMateriaName/{rawMateriaName}")]
        public DataResult GetRawMateria([FromRoute] string rawMateriaName)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowcardDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.RawMateriaNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="rawMateria"></param>
        /// <returns></returns>
        // PUT: api/RawMateria/Id/5
        [HttpPut("Id/{id}")]
        public Result PutRawMateria([FromRoute] int id, [FromBody] RawMateria rawMateria)
        {
            var data =
                ServerConfig.FlowcardDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName;", new { rawMateria.RawMateriaName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!rawMateria.RawMateriaName.IsNullOrEmpty() && data.RawMateriaName != rawMateria.RawMateriaName)
                {
                    return Result.GenError<Result>(Error.RawMateriaIsExist);
                }
            }

            rawMateria.Id = id;
            rawMateria.CreateUserId = Request.GetIdentityInformation();
            rawMateria.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE raw_materia SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `RawMateriaName` = @RawMateriaName WHERE `Id` = @Id;", rawMateria);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName">原料批号</param>
        /// <param name="rawMateria"></param>
        /// <returns></returns>
        // PUT: api/RawMateria/RawMateriaName/5
        [HttpPut("RawMateriaName/{rawMateriaName}")]
        public Result PutRawMateria([FromRoute] string rawMateriaName, [FromBody] RawMateria rawMateria)
        {
            var data =
                ServerConfig.FlowcardDb.Query<RawMateria>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName;", new { rawMateria.RawMateriaName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!rawMateria.RawMateriaName.IsNullOrEmpty() && data.RawMateriaName != rawMateria.RawMateriaName)
                {
                    return Result.GenError<Result>(Error.RawMateriaIsExist);
                }
            }
            rawMateria.Id = data.Id;
            rawMateria.CreateUserId = Request.GetIdentityInformation();
            rawMateria.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE raw_materia SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `RawMateriaName` = @RawMateriaName WHERE `Id` = @Id;", rawMateria);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RawMateria
        [HttpPost]
        public Result PostRawMateria([FromBody] RawMateria rawMateria)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName;", new { rawMateria.RawMateriaName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.RawMateriaIsExist);
            }

            rawMateria.CreateUserId = Request.GetIdentityInformation();
            rawMateria.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);",
                rawMateria);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RawMateria/RawMaterias
        [HttpPost("RawMaterias")]
        public Result PostRawMateria([FromBody] List<RawMateria> rawMaterias)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName IN @rawmateriaIds;", new
                {
                    rawmateriaIds = rawMaterias.Select(x => x.RawMateriaName)
                }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<DataResult>(Error.RawMateriaIsExist);
            }

            foreach (var rawMateria in rawMaterias)
            {
                rawMateria.CreateUserId = Request.GetIdentityInformation();
                rawMateria.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);",
                rawMaterias);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/RawMateria/Id/5
        [HttpDelete("Id/{id}")]
        public Result DeleteRawMateria([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `raw_materia` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 原料批号
        /// </summary>
        /// <param name="rawMateriaName">原料批号</param>
        /// <returns></returns>
        // DELETE: api/RawMateria/RawMateriaName/5
        [HttpDelete("RawMateriaName/{rawMateriaName}")]
        public Result DeleteRawMateria([FromRoute] string rawMateriaName)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName;", new { rawMateriaName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `raw_materia` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `RawMateriaName`= @rawMateriaName;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    rawMateriaName
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}