using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.Result;
using ServiceStack;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料日志
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialLogController : ControllerBase
    {
        // GET: api/MaterialLog?planId=0&qId=0...
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="planId"></param>
        /// <param name="billId"></param>
        /// <param name="qId">日志Id</param>
        /// <param name="type">0  ; 1 入库; 2 出库;</param>
        /// <param name="purposeId">0 所有 1 计划 2 其他</param>
        /// <param name="purpose">来源/用途</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetMaterialLog([FromQuery] DateTime startTime, DateTime endTime, int planId, int billId, int qId, int type, int purposeId, string purpose = "")
        {
            var result = new DataResult();
            var param = new List<string>();
            if (startTime != default(DateTime) && endTime != default(DateTime))
            {
                param.Add("a.Time >= @startTime AND a.Time <= @endTime");
            }
            if (planId != 0)
            {
                param.Add("a.PlanId = @planId");
            }
            if (purposeId == 1)
            {
                param.Add("a.PlanId != 0");
            }
            else if (purposeId == 2)
            {
                param.Add("a.PlanId == 0");
            }
            if (billId != 0)
            {
                param.Add("a.BillId = @billId");
            }
            if (qId != 0)
            {
                param.Add("a.Id = @qId");
            }
            if (type != 0)
            {
                param.Add("a.Type = @type");
            }
            if (purpose != "")
            {
                param.Add("a.Purpose = @purpose");
            }

            var sql = "SELECT a.*, b.Plan, c.`Code` FROM `material_log` a LEFT JOIN `production_plan` b ON a.PlanId = b.Id LEFT JOIN `material_bill` c ON a.BillId = c.Id";
            sql += !param.Any() ? ";" : " WHERE " + param.Join(" AND ") + ";";
            result.datas.AddRange(ServerConfig.ApiDb.Query<MaterialLog>(sql, new
            {
                startTime,
                endTime,
                planId,
                billId,
                qId,
                type,
                purpose
            }).OrderByDescending(x => x.Id));
            return result;
        }

        //// PUT: api/MaterialLog
        //[HttpPut]
        //public Result PutMaterialLog([FromBody] IEnumerable<MaterialLog> materialSites)
        //{
        //    if (materialSites == null)
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogNotExist);
        //    }

        //    if (materialSites.Any(x => x.Site.IsNullOrEmpty()))
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogNotEmpty);
        //    }

        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @id AND `MarkedDelete` = 0;",
        //            new { id = materialSites.Select(x => x.Id) }).FirstOrDefault();
        //    if (cnt != materialSites.Count())
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogNotExist);
        //    }

        //    if (materialSites.Count() != materialSites.GroupBy(x => x.Site).Count())
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogDuplicate);
        //    }

        //    cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id NOT IN @id AND Site IN @site AND `MarkedDelete` = 0;",
        //            new { id = materialSites.Select(x => x.Id), site = materialSites.Select(x => x.Site) }).FirstOrDefault();
        //    if (cnt > 0)
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogIsExist);
        //    }

        //    var markedDateTime = DateTime.Now;
        //    foreach (var materialSite in materialSites)
        //    {
        //        materialSite.MarkedDateTime = markedDateTime;
        //    }

        //    ServerConfig.ApiDb.Execute(
        //        "UPDATE material_site SET `MarkedDateTime` = @MarkedDateTime, `Site` = @Site, `Remark` = @Remark WHERE `Id` = @Id;", materialSites);

        //    return Result.GenError<Result>(Error.Success);
        //}

        //// POST: api/MaterialLog
        //[HttpPost]
        //public Result PostMaterialLog([FromBody] MaterialLog materialSite)
        //{
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Site = @Site AND MarkedDelete = 0;",
        //            new { materialSite.Site }).FirstOrDefault();
        //    if (cnt > 0)
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogIsExist);
        //    }
        //    materialSite.CreateUserId = Request.GetIdentityInformation();
        //    materialSite.MarkedDateTime = DateTime.Now;
        //    ServerConfig.ApiDb.Execute(
        //      "INSERT INTO material_site (`CreateUserId`, `MarkedDateTime`, `Site`, `Remark`) " +
        //      "VALUES (@CreateUserId, @MarkedDateTime, @Site, @Remark);",
        //      materialSite);

        //    return Result.GenError<Result>(Error.Success);
        //}

        //// DELETE: api/MaterialLog
        ///// <summary>
        ///// 批量删除
        ///// </summary>
        ///// <param name="ids"></param>
        ///// <returns></returns>
        //[HttpDelete]
        //public Result DeleteMaterialLog([FromBody] BatchDelete batchDelete)
        //{
        //    var ids = batchDelete.ids;
        //    var cnt =
        //        ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `material_site` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
        //    if (cnt == 0)
        //    {
        //        return Result.GenError<Result>(Error.MaterialLogNotExist);
        //    }

        //    ServerConfig.ApiDb.Execute(
        //        "UPDATE `material_site` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
        //        {
        //            MarkedDateTime = DateTime.Now,
        //            MarkedDelete = true,
        //            Id = ids
        //        });
        //    return Result.GenError<Result>(Error.Success);
        //}
    }
}