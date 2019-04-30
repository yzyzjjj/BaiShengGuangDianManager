//using ApiFlowCardManagement.Base.Server;
//using ApiFlowCardManagement.Models;
//using Microsoft.AspNetCore.Mvc;
//using ModelBase.Base.EnumConfig;
//using ModelBase.Models.Result;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using ModelBase.Base.Utils;

//namespace ApiFlowCardManagement.Controllers
//{
//    /// <summary>
//    /// 原材料规格
//    /// </summary>
//    [Route("api/[controller]")]
//    [ApiController]
//    //[Authorize]
//    public class RawMateriaSpecificationController : ControllerBase
//    {

//        // GET: api/RawMateriaSpecification
//        [HttpGet]
//        public DataResult GetRawMateriaSpecification()
//        {
//            var result = new DataResult();
//            result.datas.AddRange(ServerConfig.FlowCardDb.Query<dynamic>("SELECT a.*, b.RawMateriaName FROM `raw_materia_specification` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id WHERE a.MarkedDelete = 0;"));
//            return result;
//        }

//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id">自增Id</param>
//        /// <returns></returns>
//        // GET: api/RawMateriaSpecification/Id/5
//        [HttpGet("Id/{id}")]
//        public DataResult GetRawMateriaSpecification([FromRoute] int id)
//        {
//            var result = new DataResult();
//            var data =
//                ServerConfig.FlowCardDb.Query<dynamic>("SELECT a.*, b.RawMateriaName FROM `raw_materia_specification` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id WHERE a.Id = @id AND a.MarkedDelete = 0;", new { id }).FirstOrDefault();
//            if (data == null)
//            {
//                result.errno = Error.RawMateriaSpecificationNotExist;
//                return result;
//            }
//            result.datas.Add(data);
//            return result;
//        }

//        /// <summary>
//        /// 原料批号
//        /// </summary>
//        /// <param name="rawMateriaName">原料批号</param>
//        /// <returns></returns>
//        // GET: api/RawMateriaSpecification/5
//        [HttpGet("RawMateriaName/{rawMateriaName}")]
//        public DataResult GetRawMateriaSpecification([FromRoute] string rawMateriaName)
//        {
//            var rawMateria =
//                ServerConfig.FlowCardDb.Query<RawMateria>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
//            if (rawMateria == null)
//            {
//                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
//            }

//            var result = new DataResult();
//            var data =
//                ServerConfig.FlowCardDb.Query<dynamic>("SELECT a.*, b.RawMateriaName FROM `raw_materia_specification` a JOIN `raw_materia` b ON a.RawMateriaId = b.Id WHERE a.RawMateriaId = @Id AND a.MarkedDelete = 0;", new { rawMateria.Id });
//            result.datas.Add(data);
//            return result;
//        }

//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id">自增Id</param>
//        /// <param name="rawMateriaSpecification"></param>
//        /// <returns></returns>
//        // PUT: api/RawMateriaSpecification/5
//        [HttpPut("{id}")]
//        public Result PutRawMateriaSpecification([FromRoute] int id, [FromBody] RawMateriaSpecification rawMateriaSpecification)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia_specification` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.RawMateriaSpecificationNotExist);
//            }

//            cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id = @RawMateriaId AND MarkedDelete = 0;", new { rawMateriaSpecification.RawMateriaId }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
//            }

//            rawMateriaSpecification.Id = id;
//            rawMateriaSpecification.CreateUserId = Request.GetIdentityInformation();
//            rawMateriaSpecification.MarkedDateTime = DateTime.Now;
//            ServerConfig.FlowCardDb.Execute(
//                "UPDATE raw_materia_specification SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
//                "`RawMateriaId` = @RawMateriaId, `SpecificationName` = @SpecificationName, `SpecificationValue` = @SpecificationValue WHERE `Id` = @Id;", rawMateriaSpecification);

//            return Result.GenError<Result>(Error.Success);
//        }

//        // POST: api/RawMateriaSpecification
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="rawMateriaSpecification"></param>
//        /// <returns></returns>
//        [HttpPost]
//        public Result PostRawMateriaSpecification([FromBody] RawMateriaSpecification rawMateriaSpecification)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id = @RawMateriaId AND MarkedDelete = 0;", new { rawMateriaSpecification.RawMateriaId }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
//            }

//            rawMateriaSpecification.CreateUserId = Request.GetIdentityInformation();
//            rawMateriaSpecification.MarkedDateTime = DateTime.Now;
//            ServerConfig.FlowCardDb.Execute(
//                "INSERT INTO raw_materia_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaId`, `SpecificationName`, `SpecificationValue`) " +
//                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaId, @SpecificationName, @SpecificationValue);",
//                rawMateriaSpecification);

//            return Result.GenError<Result>(Error.Success);
//        }

//        /// <summary>
//        /// 批量添加
//        /// </summary>
//        /// <param name="rawMateriaSpecifications"></param>
//        /// <returns></returns>
//        // POST: api/RawMateriaSpecification/RawMateriaSpecifications
//        [HttpPost("RawMateriaSpecifications")]
//        public Result PostRawMateriaSpecification([FromBody] List<RawMateriaSpecification> rawMateriaSpecifications)
//        {
//            var rawmateriaIds = rawMateriaSpecifications.GroupBy(x => x.RawMateriaId);
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia` WHERE Id IN @rawMateriaIds AND MarkedDelete = 0;", new
//                {
//                    rawMateriaIds = rawmateriaIds.Select(x => x.Key)
//                }).FirstOrDefault();
//            if (cnt != rawmateriaIds.Count())
//            {
//                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
//            }

//            foreach (var rawmateriaSpecification in rawMateriaSpecifications)
//            {
//                rawmateriaSpecification.CreateUserId = Request.GetIdentityInformation();
//                rawmateriaSpecification.MarkedDateTime = DateTime.Now;
//            }
//            ServerConfig.FlowCardDb.Execute(
//                "INSERT INTO raw_materia_specification (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `RawMateriaId`, `SpecificationName`, `SpecificationValue`) " +
//                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @RawMateriaId, @SpecificationName, @SpecificationValue);",
//                rawMateriaSpecifications);

//            return Result.GenError<Result>(Error.Success);
//        }

//        /// <summary>
//        /// 自增Id
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        // DELETE: api/RawMateriaSpecification/Id/5
//        [HttpDelete("Id/{id}")]
//        public Result DeleteRawMateriaSpecification([FromRoute] int id)
//        {
//            var cnt =
//                ServerConfig.FlowCardDb.Query<int>("SELECT COUNT(1) FROM `raw_materia_specification` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
//            if (cnt == 0)
//            {
//                return Result.GenError<Result>(Error.RawMateriaSpecificationNotExist);
//            }

//            ServerConfig.FlowCardDb.Execute(
//                "UPDATE `raw_materia_specification` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
//                {
//                    MarkedDateTime = DateTime.Now,
//                    MarkedDelete = true,
//                    Id = id
//                });
//            return Result.GenError<Result>(Error.Success);
//        }

//        /// <summary>
//        /// 原料批号
//        /// </summary>
//        /// <param name="rawMateriaName">原料批号</param>
//        /// <returns></returns>
//        // DELETE: api/RawMateriaSpecification/RawMateriaName/5
//        [HttpDelete("RawMateriaName/{rawMateriaName}")]
//        public Result DeleteRawMateriaSpecification([FromRoute] string rawMateriaName)
//        {
//            var data =
//                ServerConfig.FlowCardDb.Query<RawMateria>("SELECT `Id` FROM `raw_materia` WHERE RawMateriaName = @rawMateriaName AND MarkedDelete = 0;", new { rawMateriaName }).FirstOrDefault();
//            if (data == null)
//            {
//                return Result.GenError<DataResult>(Error.RawMateriaNotExist);
//            }

//            ServerConfig.FlowCardDb.Execute(
//                "UPDATE `raw_materia_specification` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `RawMateriaId`= @RawMateriaId;", new
//                {
//                    MarkedDateTime = DateTime.Now,
//                    MarkedDelete = true,
//                    RawMateriaId = data.Id
//                });
//            return Result.GenError<Result>(Error.Success);
//        }
//    }
//}