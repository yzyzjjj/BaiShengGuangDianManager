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
    /// 物料请购部门
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialDepartmentController : ControllerBase
    {
        // GET: api/MaterialDepartment/?qId=0
        [HttpGet]
        public DataResult GetMaterialDepartment([FromQuery] int qId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<MaterialDepartment>($"SELECT * FROM `material_department` WHERE {(qId == 0 ? "" : "Id = @id AND ")}`MarkedDelete` = 0;",
                new { id = qId });
            if (qId != 0 && !data.Any())
            {
                return Result.GenError<DataResult>(Error.MaterialDepartmentNotExist);
            }
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/MaterialDepartment
        [HttpPut]
        public Result PutMaterialDepartment([FromBody] IEnumerable<MaterialDepartment> materialDepartments)
        {
            if (materialDepartments == null)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentNotExist);
            }

            if (materialDepartments.Any(x => x.Department.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.MaterialDepartmentNotEmpty);
            }

            var departments =
                ServerConfig.ApiDb.Query<MaterialDepartment>("SELECT * FROM `material_department` WHERE Id IN @id AND `MarkedDelete` = 0;",
                    new { id = materialDepartments.Select(x => x.Id) });
            if (!departments.Any())
            {
                return Result.GenError<Result>(Error.MaterialDepartmentNotExist);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department` WHERE Id NOT IN @id AND Department IN @Department AND `MarkedDelete` = 0;",
                    new { id = materialDepartments.Select(x => x.Id), Department = materialDepartments.Select(x => x.Department) }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentIsExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var department in departments)
            {
                var dep = materialDepartments.FirstOrDefault(x => x.Id == department.Id);
                if (dep == null)
                {
                    continue;
                }

                if (dep.Department != null && dep.Department != department.Department)
                {
                    department.MarkedDateTime = markedDateTime;
                    department.Department = dep.Department;
                }

                if (dep.Remark != null && dep.Remark != department.Remark)
                {
                    department.MarkedDateTime = markedDateTime;
                    department.Remark = dep.Remark;
                }
                if (dep.Get != department.Get)
                {
                    department.MarkedDateTime = markedDateTime;
                    department.Get = dep.Get;
                }
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_department` SET `MarkedDateTime` = @MarkedDateTime, `Department` = @Department, `Remark` = @Remark, `Get` = @Get WHERE `Id` = @Id;", departments);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/MaterialDepartment
        [HttpPost]
        public Result PostMaterialDepartment([FromBody] MaterialDepartment materialDepartment)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department` WHERE Department = @Department AND MarkedDelete = 0;",
                    new { materialDepartment.Department }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentIsExist);
            }
            materialDepartment.CreateUserId = Request.GetIdentityInformation();
            ServerConfig.ApiDb.Execute(
              "INSERT INTO material_department (`CreateUserId`, `Department`, `Remark`) VALUES (@CreateUserId, @Department, @Remark);",
              materialDepartment);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/MaterialDepartment
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteMaterialDepartment([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_department` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.MaterialDepartmentNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_department` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `material_department_member` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `DepartmentId` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });
            return Result.GenError<Result>(Error.Success);
        }
    }
}