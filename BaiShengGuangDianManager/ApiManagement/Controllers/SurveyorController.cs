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
    /// 检验员
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SurveyorController : ControllerBase
    {
        // GET: api/Surveyor
        [HttpGet]
        public DataResult GetSurveyor()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE MarkedDelete = 0;"));
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
                ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE Id = @id AND MarkedDelete = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.SurveyorNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        /// <summary>
        /// account
        /// </summary>
        /// <param name="account">account</param>
        /// <param name="surveyor"></param>
        /// <returns></returns>
        // PUT: api/Surveyor/Id/5
        [HttpPut("{account}")]
        public Result PutSurveyor([FromRoute] string account, [FromBody] Surveyor surveyor)
        {
            var data =
                ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE Account = @Account;", new { Account = account }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.SurveyorNotExist);
            }

            //var cnt =
            //    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `surveyor` WHERE SurveyorName = @SurveyorName;", new { surveyor.SurveyorName }).FirstOrDefault();
            //if (cnt > 0)
            //{
            //    if (!surveyor.SurveyorName.IsNullOrEmpty() && data.SurveyorName != surveyor.SurveyorName)
            //    {
            //        return Result.GenError<Result>(Error.SurveyorIsExist);
            //    }
            //}

            surveyor.Id = data.Id;
            surveyor.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE surveyor SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `SurveyorName` = @SurveyorName WHERE `Id` = @Id;", surveyor);

            return Result.GenError<Result>(Error.Success);
        }



        // POST: api/Surveyor
        [HttpPost]
        public Result PostSurveyor([FromBody] Surveyor surveyor)
        {
            surveyor.CreateUserId = Request.GetIdentityInformation();
            surveyor.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "INSERT INTO surveyor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `SurveyorName`, `Account`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @SurveyorName, @Account);",
                surveyor);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Surveyor/Surveyors
        [HttpPost("Surveyors")]
        public Result PostSurveyor([FromBody] List<Surveyor> surveyors)
        {
            var createUserId = Request.GetIdentityInformation();
            var time = DateTime.Now;
            foreach (var surveyor in surveyors)
            {
                surveyor.CreateUserId = createUserId;
                surveyor.MarkedDateTime = time;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO surveyor (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `SurveyorName`, `Account`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @SurveyorName, @Account);",
                surveyors);

            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// account
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        // DELETE: api/Surveyor/Id/5
        [HttpDelete("{account}")]
        public Result DeleteSurveyor([FromRoute] string account)
        {
            var data =
                ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE Account = @Account AND MarkedDelete = 0;", new { Account = account }).FirstOrDefault();
            if (data == null)
            {
                return Result.GenError<Result>(Error.SurveyorIsExist);
            }
            ServerConfig.ApiDb.Execute(
                "UPDATE `surveyor` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = data.Id
                });
            return Result.GenError<Result>(Error.Success);
        }

    }
}