using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="isPlan">是否是计划相关 0  1  </param>
        /// <param name="planId"></param>
        /// <param name="billId"></param>
        /// <param name="qId">日志Id</param>
        /// <param name="type">0  ; 1 入库; 2 出库;</param>
        /// <param name="purposeId">0 所有 1 计划 2 其他</param>
        /// <param name="purpose">来源/用途</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetMaterialLog([FromQuery] DateTime startTime, DateTime endTime, int isPlan, int planId, int billId, int qId, int type, int purposeId, string purpose = "")
        {
            var result = new DataResult();
            var param = new List<string>();
            if (startTime != default(DateTime) && endTime != default(DateTime))
            {
                param.Add("a.Time >= @startTime AND a.Time <= @endTime");
            }
            if (isPlan != 0)
            {
                param.Add("a.PlanId = @planId");
            }
            if (purposeId == 1)
            {
                param.Add("a.PlanId != 0");
            }
            else if (purposeId == 2)
            {
                param.Add("a.PlanId = 0");
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

            var sql = "SELECT a.*, b.Plan, c.`Code`, d.`Name`, e.Specification FROM `material_log` a " +
                      "LEFT JOIN `production_plan` b ON a.PlanId = b.Id " +
                      "LEFT JOIN `material_bill` c ON a.BillId = c.Id " +
                      "LEFT JOIN `material_name` d ON a.NameId = d.Id " +
                      "LEFT JOIN `material_specification` e ON a.SpecificationId = e.Id";
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
    }
}