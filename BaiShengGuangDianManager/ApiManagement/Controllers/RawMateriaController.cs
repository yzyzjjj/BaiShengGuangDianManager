using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers
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
            var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE MarkedDelete = 0;");
            var rawMateriaSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` WHERE MarkedDelete = 0;");
            foreach (var rawMateria in rawMaterias)
            {
                var specifications = rawMateriaSpecifications.Where(x => x.RawMateriaId == rawMateria.Id);
                rawMateria.RawMateriaSpecifications.AddRange(specifications);
            }

            result.datas.AddRange(rawMaterias);
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
                ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.RawMateriaNotExist;
                return result;
            }

            var rawMateriaSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` WHERE MarkedDelete = 0 AND RawMateriaId = @id;", new { id });
            data.RawMateriaSpecifications.AddRange(rawMateriaSpecifications);
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
                ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.RawMateriaNotExist;
                return result;
            }

            var rawMateriaSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` WHERE MarkedDelete = 0 AND RawMateriaId = @id;", new { id = data.Id });
            data.RawMateriaSpecifications.AddRange(rawMateriaSpecifications);
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
                ServerConfig.ApiDb.Query<RawMateria>("SELECT * FROM `raw_materia` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName AND MarkedDelete = 0;", new { rawMateria.RawMateriaName }).FirstOrDefault();
            if (cnt > 0)
            {
                if (!rawMateria.RawMateriaName.IsNullOrEmpty() && data.RawMateriaName != rawMateria.RawMateriaName)
                {
                    return Result.GenError<Result>(Error.RawMateriaIsExist);
                }
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            rawMateria.Id = id;
            rawMateria.CreateUserId = createUserId;
            rawMateria.MarkedDateTime = time;
            ServerConfig.ApiDb.Execute(
                "UPDATE raw_materia SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `RawMateriaName` = @RawMateriaName WHERE `Id` = @Id;", rawMateria);

            if (rawMateria.RawMateriaSpecifications.Any())
            {
                var rawMateriaSpecifications = rawMateria.RawMateriaSpecifications;
                foreach (var rawMateriaSpecification in rawMateriaSpecifications)
                {
                    rawMateriaSpecification.RawMateriaId = id;
                    rawMateriaSpecification.CreateUserId = createUserId;
                    rawMateriaSpecification.MarkedDateTime = time;
                }
                var existRawMateriaSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` " +
                                                                                                           "WHERE MarkedDelete = 0 AND RawMateriaId = @RawMateriaId;", new { RawMateriaId = id });
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO raw_materia_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaId, @SpecificationName, @SpecificationValue);",
                    rawMateriaSpecifications.Where(x => x.Id == 0));

                var updateRawMateriaSpecifications = rawMateriaSpecifications.Where(x => x.Id != 0
                    && existRawMateriaSpecifications.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                updateRawMateriaSpecifications.AddRange(existRawMateriaSpecifications.Where(x => rawMateriaSpecifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));

                ServerConfig.ApiDb.Execute(
                    "UPDATE raw_materia_specification SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                    "`RawMateriaId` = @RawMateriaId, `SpecificationName` = @SpecificationName, `SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", updateRawMateriaSpecifications);
            }

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
                ServerConfig.ApiDb.Query<RawMateria>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName AND MarkedDelete = 0;", new { rawMateria.RawMateriaName }).FirstOrDefault();
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
            ServerConfig.ApiDb.Execute(
                "UPDATE raw_materia SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `RawMateriaName` = @RawMateriaName WHERE `Id` = @Id;", rawMateria);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RawMateria
        [HttpPost]
        public Result PostRawMateria([FromBody] RawMateria rawMateria)
        {
            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @RawMateriaName AND MarkedDelete = 0;", new { rawMateria.RawMateriaName }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.RawMateriaIsExist);
            }

            rawMateria.CreateUserId = createUserId;
            rawMateria.MarkedDateTime = time;
            var index = ServerConfig.ApiDb.Query<int>(
                 "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
                 "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);SELECT LAST_INSERT_ID();",
                 rawMateria).FirstOrDefault();

            if (rawMateria.RawMateriaSpecifications.Any())
            {
                var rawMateriaRawMateriaSpecifications = rawMateria.RawMateriaSpecifications;
                foreach (var rawMateriaSpecification in rawMateriaRawMateriaSpecifications)
                {
                    rawMateriaSpecification.RawMateriaId = index;
                    rawMateriaSpecification.CreateUserId = createUserId;
                    rawMateriaSpecification.MarkedDateTime = time;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO raw_materia_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaId, @SpecificationName, @SpecificationValue);",
                    rawMateriaRawMateriaSpecifications);
            }
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/RawMateria/RawMaterias
        [HttpPost("RawMaterias")]
        public Result PostRawMateria([FromBody] List<RawMateria> rawMaterias)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName IN @rawmateriaIds AND MarkedDelete = 0;", new
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
            ServerConfig.ApiDb.Execute(
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `raw_materia` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `raw_materia_specification` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `RawMateriaId`= @RawMateriaId;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    RawMateriaId = id
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
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMateriaNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `raw_materia` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `RawMateriaName`= @rawMateriaName;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    rawMateriaName
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}