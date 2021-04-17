using ApiManagement.Base.Helper;
using ApiManagement.Models.AccountManagementModel;
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
    /// 登陆/权限相关
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class PermissionGroupController : ControllerBase
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
                ? PermissionGroupHelper.GetMenu(qId)
                : PermissionGroupHelper.GetDetail(qId));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.PermissionNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/Permission
        [HttpPut]
        public object PutPermission([FromBody] IEnumerable<PermissionGroup> permissions)
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

            //var sames = permissions.Select(x => x.Name);
            var ids = permissions.Select(x => x.Id);
            //if (PermissionGroupHelper.GetHaveSame(sames, ids))
            //{
            //    return Result.GenError<Result>(Error.PermissionIsExist);
            //}

            var cnt = PermissionGroupHelper.Instance.GetCountByIds(ids);
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

            PermissionGroupHelper.Instance.Update(permissions);
            RedisHelper.PublishToTable(PermissionGroupHelper.TableName);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Permission
        [HttpPost]
        public object PostPermission([FromBody] IEnumerable<PermissionGroup> permissions)
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
            if (PermissionGroupHelper.GetHaveSame(sames))
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
            PermissionGroupHelper.Instance.Add(permissions);
            RedisHelper.PublishToTable(PermissionGroupHelper.TableName);
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
            var cnt = PermissionGroupHelper.Instance.GetCountByIds(ids);
            if (cnt != ids.Count())
            {
                return Result.GenError<Result>(Error.PermissionNotExist);
            }
            PermissionGroupHelper.Instance.Delete(ids);
            RedisHelper.PublishToTable(PermissionGroupHelper.TableName);
            return Result.GenError<Result>(Error.Success);
        }


        /// <summary>
        /// 获取个人页面
        /// </summary>
        /// <returns></returns>
        [HttpGet("Pages")]
        public DataResult Pages()
        {
            var userId = Request.GetIdentityInformation();
            var accountInfo = AccountInfoHelper.GetAccountInfo(userId);
            if (accountInfo == null)
            {
                return Result.GenError<DataResult>(Error.AccountNotExist);
            }

            var result = new DataResult();
            var permissions = PermissionGroupHelper.PermissionGroupsList.Values
                //.Where(x => !x.IsDelete && x.IsPage && accountInfo.PermissionsList.Any(y => y == x.Id)).Select(x => new { x.Id, x.Name, x.Url, x.Order, x.Label }));
                .Where(x => !x.MarkedDelete && x.IsPage && accountInfo.PermissionsList.Any(y => y == x.Id)).ToList();
            var parents = permissions.GroupBy(x => x.Parent).Select(x => x.Key).Distinct();
            var not = parents.Where(x => permissions.All(y => y.Id != x));
            permissions.AddRange(PermissionGroupHelper.PermissionGroupsList.Values.Where(x => not.Contains(x.Id)));
            permissions = permissions.Where(x => x.Type != 0).ToList();
            var rp = new List<PermissionGroup>();
            var p = permissions.Where(x => x.Parent == 0).OrderBy(x => x.Order);
            rp.AddRange(p);
            rp.AddRange(permissions.Where(x => x.Parent != 0).OrderBy(x => p.FirstOrDefault(y => x.Parent == y.Id)?.Order).ThenBy(x => x.Order));
            result.datas.AddRange(rp);

            return result;
        }

        /// <summary>
        /// 获取个人权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("Permission")]
        public CommonResult Permission()
        {
            var createUserId = Request.GetIdentityInformation();
            var accountInfo = AccountInfoHelper.GetAccountInfo(createUserId);
            if (accountInfo == null)
            {
                return Result.GenError<CommonResult>(Error.AccountNotExist);
            }

            //if (!PermissionHelper.CheckPermission(Request.Path.Value))
            //{
            //    return Result.GenError<CommonResult>(Error.NoAuth);
            //}
            var result = new CommonResult { data = accountInfo.Permissions };
            return result;
        }

        ///// <summary>
        ///// 获取所有权限
        ///// </summary>
        ///// <returns></returns>
        //[HttpGet("Permissions")]
        //public DataResult Permissions()
        //{
        //    //if (!PermissionHelper.CheckPermission(Request.Path.Value))
        //    //{
        //    //    return Result.GenError<DataResult>(Error.NoAuth);
        //    //}
        //    var result = new DataResult();
        //    //result.datas.AddRange(PermissionHelper.PermissionsList.Values.Where(x => x.Type != 0).Select(x => new { x.Id, x.Name, x.IsPage, x.Type, x.Label, x.Order }));
        //    result.datas.AddRange(PermissionGroupHelper.PermissionGroupsList.Values.Where(x => x.Type != 0));
        //    return result;
        //}

        /// <summary>
        /// 获取其他权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("OtherPermissions")]
        public DataResult OtherPermissions([FromQuery]string role)
        {
            var roleList = role.Split(",").GroupBy(x => x).Where(x => int.TryParse(x.Key, out var _)).Select(y => int.Parse(y.Key)).OrderBy(x => x);
            if (!roleList.Any())
            {
                return Result.GenError<DataResult>(Error.RoleNotSelect);
            }
            var roleInfos = RoleInfoHelper.Instance.GetByIds<RoleInfo>(roleList);
            IEnumerable<int> rolePermissionsList;
            if (roleInfos == null || !roleInfos.Any())
            {
                rolePermissionsList = new List<int>();
            }
            else
            {
                rolePermissionsList = roleInfos.SelectMany(x => x.PermissionsList).Distinct();
            }

            var result = new DataResult();
            var otherPermissions = PermissionGroupHelper.PermissionGroupsList.Values.Where(x => x.Type != 0 && !rolePermissionsList.Contains(x.Id)).ToList();
            var parents = otherPermissions.GroupBy(x => x.Parent).Select(x => x.Key).Distinct();
            var not = parents.Where(x => otherPermissions.All(y => y.Id != x));
            otherPermissions.AddRange(PermissionGroupHelper.PermissionGroupsList.Values.Where(x => not.Contains(x.Id)));
            //result.datas.AddRange(otherPermissions.Select(x => new { x.Id, x.Name, x.IsPage, x.Type, x.Label, x.Order }));
            otherPermissions = otherPermissions.Where(x => x.Type != 0).ToList();
            var rp = new List<PermissionGroup>();
            var p = otherPermissions.Where(x => x.Parent == 0).OrderBy(x => x.Order);
            rp.AddRange(p);
            rp.AddRange(otherPermissions.Where(x => x.Parent != 0).OrderBy(x => p.FirstOrDefault(y => x.Parent == y.Id)?.Order).ThenBy(x => x.Order));
            result.datas.AddRange(rp);
            return result;
        }
    }
}