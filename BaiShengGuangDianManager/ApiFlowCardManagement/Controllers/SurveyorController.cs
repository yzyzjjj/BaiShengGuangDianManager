using ApiFlowCardManagement.Base.Server;
using ApiFlowCardManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiFlowCardManagement.Controllers
{
    /// <summary>
    /// 检验员
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyorController : ControllerBase
    {
        // GET: api/Surveyor
        [HttpGet]
        public DataResult GetSurveyor()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.FlowcardDb.Query<Surveyor>("SELECT * FROM `surveyor`;"));
            return result;
        }
        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <returns></returns>
        // GET: api/Surveyor/5
        [HttpGet("{id}")]
        public DataResult GetSurveyor([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.FlowcardDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.SurveyorNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id">自增Id</param>
        /// <param name="surveyor"></param>
        /// <returns></returns>
        // PUT: api/Surveyor/Id/5
        [HttpPut("{id}")]
        public Result PutSurveyor([FromRoute] int id, [FromBody] Surveyor surveyor)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SurveyorNotExist);
            }

            surveyor.Id = id;
            surveyor.CreateUserId = Request.GetIdentityInformation();
            surveyor.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "UPDATE surveyor SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `SurveyorName` = @SurveyorName WHERE `Id` = @Id;", surveyor);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Surveyor
        [HttpPost]
        public Result PostSurveyor([FromBody] Surveyor surveyor)
        {
            surveyor.CreateUserId = Request.GetIdentityInformation();
            surveyor.MarkedDateTime = DateTime.Now;
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO surveyor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `SurveyorName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @SurveyorName);",
                surveyor);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Surveyor/Surveyors
        [HttpPost("Surveyors")]
        public Result PostSurveyor([FromBody] List<Surveyor> surveyors)
        {
            foreach (var surveyor in surveyors)
            {
                surveyor.CreateUserId = Request.GetIdentityInformation();
                surveyor.MarkedDateTime = DateTime.Now;
            }
            ServerConfig.FlowcardDb.Execute(
                "INSERT INTO surveyor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `SurveyorName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @SurveyorName);",
                surveyors);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/Surveyor/Id/5
        [HttpDelete("{id}")]
        public Result DeleteSurveyor([FromRoute] int id)
        {
            var cnt =
                ServerConfig.FlowcardDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE Id = @id;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SurveyorNotExist);
            }

            ServerConfig.FlowcardDb.Execute(
                "UPDATE `surveyor` SET  `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}