using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Models.Result;
using System;
using System.Linq;
using ModelBase.Base.Utils;

namespace ApiManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkshopController : ControllerBase
    {
        // GET: api/Workshop
        [HttpGet]
        public DataResult GetWorkshop()
        {
            var result = new DataResult();
            result.datas.AddRange(ServerConfig.ApiDb.Query<Workshop>("SELECT * FROM `workshop` WHERE `MarkedDelete` = 0;"));
            return result;
        }

        // GET: api/Workshop/5
        [HttpGet("{id}")]
        public DataResult GetWorkshop([FromRoute] int id)
        {
            var result = new DataResult();
            var data =
                ServerConfig.ApiDb.Query<Workshop>("SELECT * FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (data == null)
            {
                result.errno = Error.WorkshopNotExist;
                return result;
            }
            result.datas.Add(data);
            return result;
        }

        // PUT: api/Workshop/5
        [HttpPut("{id}")]
        public Result PutWorkshop([FromRoute] int id, [FromBody] Workshop workshop)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.WorkshopNotExist);
            }

            workshop.Id = id;
            workshop.CreateUserId = Request.GetIdentityInformation();
            workshop.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
                "UPDATE workshop SET `CreateUserId` = @CreateUserId, `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete, " +
                "`ModifyId` = @ModifyId, `WorkshopName` = @WorkshopName, `Abbre` = @Abbre WHERE `Id` = @Id;", workshop);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Workshop
        [HttpPost]
        public Result PostWorkshop([FromBody] Workshop workshop)
        {

            workshop.CreateUserId = Request.GetIdentityInformation();
            workshop.MarkedDateTime = DateTime.Now;
            ServerConfig.ApiDb.Execute(
              "INSERT INTO workshop (`CreateUserId`, `MarkedDateTime`, `MarkedDelete`, `ModifyId`, `WorkshopName`, `Abbre`) " +
              "VALUES (@CreateUserId, @MarkedDateTime, @MarkedDelete, @ModifyId, @WorkshopName, @Abbre);",
              workshop);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Workshop/5
        [HttpDelete("{id}")]
        public Result DeleteWorkshop([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `workshop` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.WorkshopNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `workshop` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}