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
    /// 组织架构管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class OrganizationUnitManagementController : ControllerBase
    {
        /// <summary>
        /// 获取组织架构
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public DataResult List()
        {
            var result = new DataResult();
            result.datas.AddRange(OrganizationUnitHelper.GetOrganizationUnit());
            return result;
        }

        /// <summary>
        /// 更新组织结构
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public Result Update([FromBody] IEnumerable<OrganizationUnit> units)
        {
            if (units == null || !units.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (units.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotEmpty);
            }

            if (units.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.OrganizationUnitDuplicate);
            }

            var sames = units.Select(x => x.Name);
            var ids = units.Select(x => x.Id);
            if (OrganizationUnitHelper.GetHaveSame(sames, ids))
            {
                return Result.GenError<Result>(Error.OrganizationUnitIsExist);
            }

            var cnt = OrganizationUnitHelper.Instance.GetCountByIds(ids);
            if (cnt != units.Count())
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotExist);
            }

            var parentIds = units.Select(x => x.ParentId).Distinct(); ;
            var p = parentIds.Where(x => x != 0);
            if (p.Any())
            {
                var parentUnits = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(p);
                if (p.Count() != parentUnits.Count())
                {
                    return Result.GenError<Result>(Error.ParentNotExist);
                }
            }

            var underUnits = OrganizationUnitHelper.GetUnderOrganizationUnitsByParentIds(parentIds);
            if (underUnits.Any(x => units.Any(y => y.ParentId == x.ParentId && y.Name == x.Name)))
            {
                return Result.GenError<Result>(Error.OrganizationUnitIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var unit in units)
            {
                unit.MarkedDateTime = markedDateTime;
                unit.Name = unit.Name ?? "";
            }
            OrganizationUnitHelper.Instance.Update(units);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 移动组织结构
        /// </summary>
        /// <returns></returns>
        [HttpPost("Move")]
        public Result Move([FromBody] IEnumerable<OrganizationUnit> units)
        {
            if (units == null || !units.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var ids = units.Select(x => x.Id);
            var oldUnits = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(ids);
            if (units.Count() != oldUnits.Count())
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotExist);
            }
            var parentIds = units.Select(x => x.ParentId).Distinct(); ;
            var parentUnits = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(parentIds);
            if (parentIds.Count() != parentUnits.Count())
            {
                return Result.GenError<Result>(Error.ParentNotExist);
            }

            var underUnits = OrganizationUnitHelper.GetUnderOrganizationUnitsByParentIds(parentIds);
            if (underUnits.Any(x => units.Any(y => y.ParentId == x.ParentId && y.Name == x.Name)))
            {
                return Result.GenError<Result>(Error.OrganizationUnitIsExist);
            }

            foreach (var unit in units)
            {
                var parent = parentUnits.FirstOrDefault(x => x.Id == unit.ParentId);
                if (parent != null)
                {
                    OrganizationUnitHelper.MoveOrganizationUnit(parent, unit);
                }
            }
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 添加组织结构
        /// parentId  
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result Add([FromBody] IEnumerable<OrganizationUnit> units)
        {
            if (units == null || !units.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (units.Any(x => x.Name.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotEmpty);
            }

            if (units.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.OrganizationUnitDuplicate);
            }

            var sames = units.Select(x => x.Name);
            if (OrganizationUnitHelper.GetHaveSame(sames))
            {
                return Result.GenError<Result>(Error.OrganizationUnitIsExist);
            }

            var parentIds = units.Select(x => x.ParentId).Distinct(); ;
            var p = parentIds.Where(x => x != 0);
            if (p.Any())
            {
                var parentUnits = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(p);
                if (p.Count() != parentUnits.Count())
                {
                    return Result.GenError<Result>(Error.ParentNotExist);
                }
            }

            var underUnits = OrganizationUnitHelper.GetUnderOrganizationUnitsByParentIds(parentIds);
            if (underUnits.Any(x => units.Any(y => y.ParentId == x.ParentId && y.Name == x.Name)))
            {
                return Result.GenError<Result>(Error.OrganizationUnitIsExist);
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var unit in units)
            {
                unit.CreateUserId = userId;
                unit.MarkedDateTime = markedDateTime;
                unit.Name = unit.Name ?? "";
                OrganizationUnitHelper.AddOrganizationUnit(unit);
            }
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 删除组织结构
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result Delete([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var units = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(ids);
            if (units.Count() < ids.Count())
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotExist);
            }

            OrganizationUnitHelper.DeleteOrganizationUnit(units);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 获取组织成员
        /// parentId  
        /// </summary>
        /// <returns></returns>
        [HttpGet("Member")]
        public DataResult GetMemberList([FromQuery]int unitId)
        {
            var cnt = OrganizationUnitHelper.Instance.GetCountById(unitId);
            if (cnt == 0)
            {
                return Result.GenError<DataResult>(Error.OrganizationUnitNotExist);
            }

            var result = new DataResult();
            result.datas.AddRange(OrganizationUnitHelper.MemberListByUnit(unitId));
            return result;
        }

        /// <summary>
        /// 添加组织成员
        /// parentId  
        /// </summary>
        /// <returns></returns>
        [HttpPost("Member")]
        public Result PostMember([FromBody] IEnumerable<OrganizationUnitMember> members)
        {
            var unitIds = members.Select(x => x.OrganizationUnitId).Distinct();
            var units = OrganizationUnitHelper.Instance.GetByIds<OrganizationUnit>(unitIds);
            if (unitIds.Count() != units.Count())
            {
                return Result.GenError<Result>(Error.OrganizationUnitNotExist);
            }
            var accIds = members.Select(x => x.AccountId).Distinct();
            var accountInfos = AccountInfoHelper.Instance.GetByIds<AccountInfo>(accIds);
            if (accIds.Count() != accountInfos.Count())
            {
                return Result.GenError<Result>(Error.AccountNotExist);
            }

            var existMembers = OrganizationUnitHelper.MemberListByUnits(unitIds);
            if (existMembers.Any(x => members.Any(y => y.OrganizationUnitId == x.OrganizationUnitId && y.AccountId == x.AccountId)))
            {
                return Result.GenError<Result>(Error.MemberIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var member in members)
            {
                member.CreateUserId = userId;
                member.MarkedDateTime = markedDateTime;
            }
            OrganizationUnitHelper.AddMembers(members);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 删除组织成员
        /// </summary>
        /// <returns></returns>
        [HttpDelete("Member")]
        public Result DeleteMember([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var members = OrganizationUnitHelper.MemberList(ids);
            if (members.Count() < ids.Count())
            {
                return Result.GenError<Result>(Error.MemberNotExist);
            }

            OrganizationUnitHelper.DeleteMember(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}