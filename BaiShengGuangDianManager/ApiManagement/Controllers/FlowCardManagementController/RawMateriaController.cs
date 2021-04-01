using ApiManagement.Base.Server;
using ApiManagement.Models.FlowCardManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.FlowCardManagementController
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
        public DataResult GetRawMateria([FromQuery]bool menu, int qId, DateTime startTime, DateTime endTime)
        {
            var result = new DataResult();
            if (menu)
            {
                result.datas.AddRange(RawMateriaHelper.GetMenu(qId));
            }
            else
            {
                //if (!rawMateriaName.IsNullOrEmpty() && startTime != default(DateTime) && endTime != default(DateTime))
                //{
                //    sql =
                //        "SELECT * FROM `raw_materia` WHERE MarkedDelete = 0 AND RawMateriaName = @RawMateriaName AND MarkedDateTime >= @StartTime AND MarkedDateTime <= @EndTime;";
                //}
                //else if (!rawMateriaName.IsNullOrEmpty())
                //{
                //    sql =
                //        "SELECT * FROM `raw_materia` WHERE MarkedDelete = 0 AND RawMateriaName = @RawMateriaName;";
                //}
                //else if (startTime != default(DateTime) && endTime != default(DateTime))
                //{
                //    sql =
                //        "SELECT * FROM `raw_materia` WHERE MarkedDelete = 0 AND MarkedDateTime >= @StartTime AND MarkedDateTime <= @EndTime;";
                //}
                //else
                //{
                //    sql =
                //        "SELECT * FROM `raw_materia` WHERE MarkedDelete = 0;";
                //}

                //var rawMaterias = ServerConfig.ApiDb.Query<RawMateria>(sql, new
                //{
                //    RawMateriaName = rawMateriaName,
                //    StartTime = startTime,
                //    EndTime = endTime
                //}).OrderByDescending(x => x.MarkedDateTime);
                //var rawMateriaSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` WHERE MarkedDelete = 0;");
                //foreach (var rawMateria in rawMaterias)
                //{
                //    var specifications = rawMateriaSpecifications.Where(x => x.RawMateriaId == rawMateria.Id);
                //    rawMateria.RawMateriaSpecifications.AddRange(specifications);
                //}
                var rawMaterias = RawMateriaHelper.GetDetail(qId, startTime, endTime);
                if (qId != 0 && rawMaterias.Any())
                {
                    var rawMateria = rawMaterias.First();
                    rawMateria.Specifications.AddRange(ServerConfig.ApiDb.Query<RawMateriaSpecification>(
                        "SELECT * FROM `raw_materia_specification` WHERE MarkedDelete = 0 AND RawMateriaId = @Id;", new { rawMateria.Id }));
                }
                result.datas.AddRange(rawMaterias);
            }
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.RawMaterialNotExist;
                return result;
            }

            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="rawMateria"></param>
        /// <returns></returns>
        // PUT: api/RawMateria/
        [HttpPut]
        public Result PutRawMateria([FromBody] RawMateria rawMateria)
        {
            if (rawMateria == null)
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (rawMateria.RawMateriaName.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.RawMaterialNotExist);
            }

            var sames = new List<string> { rawMateria.RawMateriaName };
            var ids = new List<int> { rawMateria.Id };
            if (RawMateriaHelper.GetHaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.RawMaterialIsExist);
            }

            var data = RawMateriaHelper.Instance.Get<RawMateria>(rawMateria.Id);
            if (data == null)
            {
                return Result.GenError<Result>(Error.RawMaterialNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            var change = false;
            //if (rawMateria.RawMateriaSpecifications.Any())
            {
                var specifications = rawMateria.Specifications;
                foreach (var specification in specifications)
                {
                    specification.RawMateriaId = rawMateria.Id;
                    specification.CreateUserId = createUserId;
                    specification.MarkedDateTime = time;
                }
                if (specifications.Any(x => x.Id == 0))
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                    "INSERT INTO raw_materia_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaId`, `SpecificationName`, `SpecificationValue`) " +
                    "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaId, @SpecificationName, @SpecificationValue);",
                    specifications.Where(x => x.Id == 0));
                }

                var existSpecifications = ServerConfig.ApiDb.Query<RawMateriaSpecification>("SELECT * FROM `raw_materia_specification` " +
                                                                                            "WHERE MarkedDelete = 0 AND RawMateriaId = @RawMateriaId;", new { RawMateriaId = rawMateria.Id });
                var updateSpecifications = specifications.Where(x => x.Id != 0
                    && existSpecifications.Any(y => y.Id == x.Id && (y.SpecificationName != x.SpecificationName || y.SpecificationValue != x.SpecificationValue))).ToList();
                updateSpecifications.AddRange(existSpecifications.Where(x => specifications.All(y => x.Id != y.Id)).Select(x =>
                {
                    x.MarkedDateTime = DateTime.Now;
                    x.MarkedDelete = true;
                    return x;
                }));

                if (updateSpecifications.Any())
                {
                    change = true;
                    ServerConfig.ApiDb.Execute(
                    "UPDATE raw_materia_specification SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                    "`RawMateriaId` = @RawMateriaId, `SpecificationName` = @SpecificationName, `SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", updateSpecifications);
                }
            }

            if (change || ClassExtension.HaveChange(rawMateria, data))
            {
                rawMateria.MarkedDateTime = time;
                RawMateriaHelper.Instance.Update(rawMateria);
            }
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
                return Result.GenError<Result>(Error.RawMaterialIsExist);
            }

            rawMateria.CreateUserId = createUserId;
            rawMateria.MarkedDateTime = time;
            var index = ServerConfig.ApiDb.Query<int>(
                 "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaName`) " +
                 "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaName);SELECT LAST_INSERT_ID();",
                 rawMateria).FirstOrDefault();

            if (rawMateria.Specifications.Any())
            {
                var rawMateriaRawMateriaSpecifications = rawMateria.Specifications;
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

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/RawMateria/Id/5
        [HttpDelete("{id}")]
        public Result DeleteRawMateria([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.RawMaterialNotExist);
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
    }
}