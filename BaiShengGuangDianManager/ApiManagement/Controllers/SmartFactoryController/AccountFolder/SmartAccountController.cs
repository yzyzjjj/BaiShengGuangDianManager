using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.AccountFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartAccountController : ControllerBase
    {
        // GET: api/SmartAccount
        [HttpGet]
        public DataResult GetSmartAccount([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Number`, `Account`, `Name`, `Remark` FROM `t_user` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};"
                : $"SELECT * FROM `t_user` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")};";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<AccountInfo>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.AccountNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="smartUsers"></param>
        /// <returns></returns>
        // PUT: api/SmartAccount/Id/5
        [HttpPut]
        public Result PutSmartAccount([FromBody] IEnumerable<AccountInfo> smartUsers)
        {
            if (smartUsers == null || !smartUsers.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartUserIds = smartUsers.Select(x => x.Id);
            var data = AccountInfoHelper.Instance.GetByIds<AccountInfo>(smartUserIds);
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
            AccountInfoHelper.Instance.Update(smartUsers);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartAccount
        [HttpPost]
        public Result PostSmartAccount([FromBody] IEnumerable<AccountInfo> smartUsers)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartUser in smartUsers)
            {
                smartUser.CreateUserId = createUserId;
                smartUser.MarkedDateTime = markedDateTime;
            }
            AccountInfoHelper.Instance.Add(smartUsers);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartAccount
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartAccount([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var count = AccountInfoHelper.Instance.GetCountByIds(ids);
            if (count == 0)
            {
                return Result.GenError<Result>(Error.AccountNotExist);
            }
            AccountInfoHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}