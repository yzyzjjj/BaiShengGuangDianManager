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
    /// 角色管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class RoleManagementController : ControllerBase
    {
        /// <summary>
        /// 获取角色
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public DataResult List([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? RoleInfoHelper.GetMenu(qId)
                : RoleInfoHelper.GetDetail(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.RoleNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public Result Update([FromBody] IEnumerable<RoleInfo> roleInfos)
        {
            if (roleInfos == null || !roleInfos.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (roleInfos.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.RoleNotEmpty);
            }

            if (roleInfos.Any(x => x.Default))
            {
                return Result.GenError<Result>(Error.RoleNotOperate);
            }

            if (roleInfos.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.RoleDuplicate);
            }

            var sames = roleInfos.Select(x => x.Name);
            var ids = roleInfos.Select(x => x.Id);
            if (RoleInfoHelper.GetHaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.RoleIsExist);
            }

            var cnt = RoleInfoHelper.Instance.GetCountByIds(ids);
            if (cnt != roleInfos.Count())
            {
                return Result.GenError<Result>(Error.RoleNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var roleInfo in roleInfos)
            {
                roleInfo.MarkedDateTime = markedDateTime;
                roleInfo.Name = roleInfo.Name ?? "";
                roleInfo.Permissions = roleInfo.Permissions ?? "";
            }

            RoleInfoHelper.Instance.Update(roleInfos);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result Add([FromBody] IEnumerable<RoleInfo> roleInfos)
        {
            if (roleInfos == null || !roleInfos.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (roleInfos.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.RoleNotEmpty);
            }

            if (roleInfos.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.RoleDuplicate);
            }

            var sames = roleInfos.Select(x => x.Name);
            if (RoleInfoHelper.GetHaveSame(sames))
            {
                return Result.GenError<Result>(Error.RoleIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var roleInfo in roleInfos)
            {
                roleInfo.CreateUserId = userId;
                roleInfo.MarkedDateTime = markedDateTime;
                roleInfo.Name = roleInfo.Name ?? "";
                roleInfo.Permissions = roleInfo.Permissions ?? "";
            }

            RoleInfoHelper.Instance.Add(roleInfos);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result Delete([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var roleInfos = RoleInfoHelper.Instance.GetByIds<RoleInfo>(ids);
            if (roleInfos.Count() < ids.Count())
            {
                return Result.GenError<Result>(Error.RoleNotExist);
            }

            if (roleInfos.Any(x => x.Default))
            {
                return Result.GenError<Result>(Error.RoleNotOperate);
            }

            if (RoleInfoHelper.GetUseRoleCount(ids) > 0)
            {
                return Result.GenError<Result>(Error.AccountUseRole);
            }

            RoleInfoHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}