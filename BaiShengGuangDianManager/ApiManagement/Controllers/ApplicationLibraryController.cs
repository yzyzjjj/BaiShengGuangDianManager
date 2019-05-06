﻿using System;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ApplicationLibraryController : ControllerBase
    {
        // GET: api/ApplicationLibrary
        [HttpGet]
        public DataResult GetApplicationLibrary()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<ApplicationLibrary>("SELECT * FROM `application_library` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/ApplicationLibrary/5
        [HttpGet("{id}")]
        public DataResult GetApplicationLibrary([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<ApplicationLibrary>("SELECT * FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.ApplicationLibraryNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/ApplicationLibrary/5
        [HttpPut("{id}")]
        public Result PutApplicationLibrary([FromRoute] int id, [FromBody] ApplicationLibrary processLibrary)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }

            processLibrary.Id = id;
            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE application_library SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, `ModifyId` = @ModifyId, " +
                "`ApplicationName` = @ApplicationName, `FilePath` = @FilePath, `Description` = @Description WHERE `Id` = @Id;", processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ApplicationLibrary
        [HttpPost]
        public Result PostApplicationLibrary([FromBody] ApplicationLibrary processLibrary)
        {

            processLibrary.CreateUserId = Request.GetIdentityInformation();
            processLibrary.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO application_library (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `ApplicationName`, `FilePath`, `Description`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @ApplicationName, @FilePath, @Description);",
              processLibrary);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ApplicationLibrary/5
        [HttpDelete("{id}")]
        public Result DeleteApplicationLibrary([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `application_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ApplicationLibraryNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `application_library` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}