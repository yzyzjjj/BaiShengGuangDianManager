using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.UserFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SmartUserController : ControllerBase
    {
        // GET: api/SmartUser
        [HttpGet]
        public DataResult GetSmartUser([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Number`, `Account`, `Name`, `Remark` FROM `t_user` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT * FROM `t_user` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartUser>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartUserNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="smartUsers"></param>
        /// <returns></returns>
        // PUT: api/SmartUser/Id/5
        [HttpPut]
        public Result PutSmartUser([FromBody] IEnumerable<SmartUser> smartUsers)
        {
            if (smartUsers == null || !smartUsers.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartUserIds = smartUsers.Select(x => x.Id);
            var data = SmartUserHelper.Instance.GetByIds<SmartUser>(smartUserIds);
            if (data.Count() != smartUsers.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartUser in smartUsers)
            {
                smartUser.CreateUserId = createUserId;
                smartUser.MarkedDateTime = markedDateTime;
            }
            SmartUserHelper.Instance.Update(smartUsers);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartUser
        [HttpPost]
        public Result PostSmartUser([FromBody] IEnumerable<SmartUser> smartUsers)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartUser in smartUsers)
            {
                smartUser.CreateUserId = createUserId;
                smartUser.MarkedDateTime = markedDateTime;
            }
            SmartUserHelper.Instance.Add(smartUsers);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartUser
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartUser([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = SmartUserHelper.Instance.GetCountByIds(ids);
            if (count == 0)
            {
                return Result.GenError<Result>(Error.SmartUserNotExist);
            }
            SmartUserHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}