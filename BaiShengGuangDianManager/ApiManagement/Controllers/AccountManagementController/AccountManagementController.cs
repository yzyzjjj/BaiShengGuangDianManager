using ApiManagement.Base.Helper;
using ApiManagement.Models.AccountModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.AccountManagementController
{
    /// <summary>
    /// 用户管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class AccountManagementController : ControllerBase
    {
        /// <summary>
        /// 获取用户
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public DataResult List([FromQuery]int qId, bool menu, bool all, string ids, string eIds)
        {
            var result = new DataResult();
            var idList = !ids.IsNullOrEmpty() ? ids.Split(",").Select(int.Parse) : new List<int>();
            var eIdList = !eIds.IsNullOrEmpty() ? eIds.Split(",").Select(int.Parse) : new List<int>();
            var data = menu
                ? AccountInfoHelper.GetMenu(all, qId)
                : AccountInfoHelper.GetAccountInfos(qId, all);
            if (idList.Any())
            {
                result.datas.AddRange(data.Where(x => idList.Contains(x.Id)));
            }
            else if (eIdList.Any())
            {
                result.datas.AddRange(data.Where(x => !eIdList.Contains(x.Id)));
            }
            else
            {
                result.datas.AddRange(data);
            }
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.AccountNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public Result Update([FromBody] IEnumerable<AccountInfo> accountInfos)
        {
            if (accountInfos == null || !accountInfos.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (accountInfos.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.AccountNameNotEmpty);
            }

            if (accountInfos.Any(x => x.Default))
            {
                return Result.GenError<Result>(Error.AccountNotOperate);
            }

            if (accountInfos.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.AccountDuplicate);
            }

            //var sames = accountInfos.Select(x => x.Account);
            var ids = accountInfos.Select(x => x.Id);
            //if (AccountInfoHelper.GetHaveSame(sames, ids))
            //{
            //    return Result.GenError<Result>(Error.AccountIsExist);
            //}

            var oldAccountInfos = AccountInfoHelper.Instance.GetByIds<AccountInfo>(ids);
            if (oldAccountInfos.Count() != accountInfos.Count())
            {
                return Result.GenError<Result>(Error.AccountNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var accountInfo in accountInfos)
            {
                var old = oldAccountInfos.FirstOrDefault(x => x.Id == accountInfo.Id);
                accountInfo.MarkedDateTime = markedDateTime;
                accountInfo.EmailAddress = accountInfo.EmailAddress ?? "";
                accountInfo.DeviceIds = accountInfo.DeviceIds ?? "";
                accountInfo.Password = !accountInfo.NewPassword.IsNullOrEmpty()
                    ? AccountInfoHelper.GenAccountPwdByOriginalPwd(old?.Account ?? "", accountInfo.NewPassword)
                    : old?.Password ?? "";

                var roleInfos = RoleInfoHelper.Instance.GetByIds<RoleInfo>(accountInfo.RoleList);
                var rolePermissionsList = roleInfos.SelectMany(x => x.PermissionsList).Distinct();
                accountInfo.SelfPermissions = accountInfo.PermissionsList.Distinct().Where(x => !rolePermissionsList.Contains(x)).Join(",");
            }

            AccountInfoHelper.Instance.Update(accountInfos);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result Add([FromBody] IEnumerable<AccountInfo> accountInfos)
        {
            if (accountInfos == null || !accountInfos.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (accountInfos.Any(x => x.Account.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.AccountNotEmpty);
            }
            if (accountInfos.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.AccountNameNotEmpty);
            }

            if (accountInfos.Any(x => x.Default))
            {
                return Result.GenError<Result>(Error.AccountNotOperate);
            }

            if (accountInfos.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.AccountDuplicate);
            }

            var sames = accountInfos.Select(x => x.Account);
            if (AccountInfoHelper.GetHaveSame(sames))
            {
                return Result.GenError<Result>(Error.AccountIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var accountInfo in accountInfos)
            {
                accountInfo.CreateUserId = userId;
                accountInfo.MarkedDateTime = markedDateTime;
                accountInfo.EmailAddress = accountInfo.EmailAddress ?? "";
                accountInfo.Password = !accountInfo.Password.IsNullOrEmpty() ? AccountInfoHelper.GenAccountPwdByOriginalPwd(accountInfo.Account, accountInfo.Password) : "";

                var roleInfos = RoleInfoHelper.Instance.GetByIds<RoleInfo>(accountInfo.RoleList);
                var rolePermissionsList = roleInfos.SelectMany(x => x.PermissionsList).Distinct();
                accountInfo.SelfPermissions = accountInfo.PermissionsList.Distinct().Where(x => !rolePermissionsList.Contains(x)).Join(",");
            }

            AccountInfoHelper.Instance.Add(accountInfos);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 删除用户 根据账号ID
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result Delete([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var accountInfos = AccountInfoHelper.Instance.GetByIds<AccountInfo>(ids);
            if (accountInfos.Count() != ids.Count())
            {
                return Result.GenError<Result>(Error.AccountNotExist);
            }

            if (accountInfos.Any(x => x.Default))
            {
                return Result.GenError<Result>(Error.AccountNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var accountInfo = AccountInfoHelper.GetAccountInfo(createUserId);
            if (accountInfo == null)
            {
                return Result.GenError<CommonResult>(Error.AccountNotExist);
            }

            if (accountInfos.Any(x => x.Id == accountInfo.Id))
            {
                return Result.GenError<Result>(Error.OperateNotSafe);
            }
            AccountInfoHelper.Instance.Delete(ids);
            WorkFlowHelper.Instance.OnAccountInfoDeleted(accountInfos);
            return Result.GenError<Result>(Error.Success);
        }

        [HttpGet("EmailType")]
        public DataResult EmailType()
        {
            var result = new DataResult();
            result.datas.AddRange(EmailHelper.GetTypes()); ;
            return result;
        }
    }
}