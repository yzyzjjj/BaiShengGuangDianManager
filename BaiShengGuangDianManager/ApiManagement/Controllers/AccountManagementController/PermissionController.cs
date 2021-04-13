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
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class PermissionController : ControllerBase
    {
        /// <summary>
        /// GET: api/Permission
        /// </summary>
        /// <param name="qId">设备型号ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetPermission([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? PermissionHelper.GetMenu(qId)
                : PermissionHelper.GetDetail(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.PermissionNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/Permission
        [HttpPut]
        public object PutPermission([FromBody] IEnumerable<Permission> permissions)
        {
            if (permissions == null || !permissions.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (permissions.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.PermissionNotEmpty);
            }
            if (permissions.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.PermissionDuplicate);
            }

            var sames = permissions.Select(x => x.Name);
            var ids = permissions.Select(x => x.Id);
            if (PermissionHelper.GetHaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.PermissionIsExist);
            }

            var cnt = PermissionHelper.Instance.GetCountByIds(ids);
            if (cnt != permissions.Count())
            {
                return Result.GenError<Result>(Error.PermissionNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var permission in permissions)
            {
                permission.MarkedDateTime = markedDateTime;
                permission.Name = permission.Name ?? "";
            }
            PermissionHelper.Instance.Update(permissions);
            RedisHelper.PublishToTable(PermissionHelper.TableName);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Permission
        [HttpPost]
        public object PostPermission([FromBody] IEnumerable<Permission> permissions)
        {
            if (permissions == null || !permissions.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (permissions.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.PermissionNotEmpty);
            }
            if (permissions.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.PermissionDuplicate);
            }

            var sames = permissions.Select(x => x.Name);
            if (PermissionHelper.GetHaveSame(sames))
            {
                return Result.GenError<Result>(Error.PermissionIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var permission in permissions)
            {
                permission.CreateUserId = userId;
                permission.MarkedDateTime = markedDateTime;
                permission.Name = permission.Name ?? "";
            }
            PermissionHelper.Instance.Add(permissions);
            RedisHelper.PublishToTable(PermissionHelper.TableName);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Permission
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeletePermission([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = PermissionHelper.Instance.GetCountByIds(ids);
            if (cnt != ids.Count())
            {
                return Result.GenError<Result>(Error.PermissionNotExist);
            }
            PermissionHelper.Instance.Delete(ids);
            RedisHelper.PublishToTable(PermissionHelper.TableName);
            return Result.GenError<Result>(Error.Success);
        }
    }
}