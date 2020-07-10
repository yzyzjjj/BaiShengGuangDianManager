using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料请购部门员工
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialDepartmentMemberController : ControllerBase
    {
        // GET: api/MaterialDepartmentMember/?qId=0
        [HttpGet]
        public DataResult GetMaterialDepartmentMember([FromQuery] int qId, int dId)
        {
            var result = new DataResult();
            var p = new List<string>();
            if (qId != 0)
            {
                p.Add((qId == 0 ? "" : " AND Id = @qId"));
            }

            if (dId != 0)
            {
                p.Add((dId == 0 ? "" : " AND DepartmentId = @dId"));
            }

            var sql = "SELECT * FROM `material_department_member` WHERE `MarkedDelete` = 0" + p.Join("");
            var data = ServerConfig.ApiDb.Query<MaterialDepartmentMember>(sql, new { qId, dId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialDepartmentMemberNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialDepartmentMember
        [HttpPut]
        public Result PutMaterialDepartmentMember([FromBody] IEnumerable<MaterialDepartmentMember> members)
        {
            if (members == null || !members.Any())
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberNotExist);
            }

            if (members.Any(x => x.Member.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department_member` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = members.Select(x => x.Id) }).FirstOrDefault();
            if (cnt != members.Count())
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberNotExist);
            }

            if (members.Count() != members.GroupBy(x => new { x.DepartmentId, x.Member }).Count())
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberDuplicate);
            }

            cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department_member` WHERE Id NOT IN @id AND DepartmentId IN @DepartmentId AND Member IN @member AND `MarkedDelete` = 0;",
                    new { id = members.Select(x => x.Id), DepartmentId = members.Select(x => x.DepartmentId), member = members.Select(x => x.Member) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var materialSite in members)
            {
                materialSite.MarkedDateTime = markedDateTime;
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE material_department_member SET `MarkedDateTime` = @MarkedDateTime, `DepartmentId` = @DepartmentId, `Member` = @Member WHERE `Id` = @Id;", members);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialDepartmentMember
        [HttpPost]
        public Result PostMaterialDepartmentMember([FromBody] MaterialDepartmentMember materialDepartmentMember)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department_member` WHERE Member = @Member AND MarkedDelete = 0;",
                    new { materialDepartmentMember.Member }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberIsExist);
            }
            materialDepartmentMember.CreateUserId = Request.GetIdentityInformation();
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_department_member (`CreateUserId`, `Member`) VALUES (@CreateUserId, @Member);",
              materialDepartmentMember);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialDepartmentMember
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialDepartmentMember([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department_member` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentMemberNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_department_member` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}