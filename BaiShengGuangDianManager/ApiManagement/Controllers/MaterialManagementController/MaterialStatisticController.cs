using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Linq;

namespace ApiManagement.Controllers.MaterialManagementController
{
    /// <summary>
    /// 物料请购单
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MaterialStatisticController : ControllerBase
    {
        // GET: api/MaterialValuer/Balance?qId=0
        [HttpGet("Balance")]
        public DataResult GetMaterialValuer([FromQuery] DateTime time1, DateTime time2, MaterialStatisticInterval interval,
            int categoryId = 0, int nameId = 0, int supplierId = 0, int specificationId = 0, int siteId = 0)
        {
            var result = new DataResult();
            if (time1 == default(DateTime))
            {
                return result;
            }
            if (time2 == default(DateTime))
            {
                time2 = time1;
            }

            switch (interval)
            {
                case MaterialStatisticInterval.天:
                    time1 = time1.Date.AddDays(-1);
                    time2 = time2.Date;
                    break;
                case MaterialStatisticInterval.周:
                    time1 = time1.WeekBeginTime().AddDays(-1);
                    time2 = time2.WeekEndTime();
                    break;
                case MaterialStatisticInterval.月:
                    time1 = time1.EndOfLastMonth();
                    time2 = time2.EndOfMonth();
                    break;
                case MaterialStatisticInterval.年:
                    time1 = time1.EndOfYear(-1);
                    time2 = time2.EndOfYear();
                    break;
                default: return result;
            }

            var data = ServerConfig.ApiDb.Query<MaterialStatistic>(
                $"SELECT *, SUM(Increase) Increase, SUM(IncreaseAmount) IncreaseAmount, SUM(Consume) Consume, SUM(ConsumeAmount) ConsumeAmount, SUM(CorrectIn) CorrectIn, SUM(CorrectInAmount) CorrectInAmount, SUM(CorrectCon) CorrectCon, SUM(CorrectConAmount) CorrectConAmount, SUM(Correct) Correct, SUM(CorrectAmount) CorrectAmount " +
                $"FROM (SELECT * FROM `material_balance` WHERE 1 = 1 " +
                $"{(categoryId != 0 ? "AND CategoryId = @categoryId " : "")}" +
                $"{(nameId != 0 ? "AND NameId = @nameId " : "")}" +
                $"{(supplierId != 0 ? "AND SupplierId = @supplierId " : "")}" +
                $"{(specificationId != 0 ? "AND SpecificationId = @specificationId " : "")}" +
                $"{(siteId != 0 ? "AND SiteId = @siteId " : "")} " +
                $"And Time > @time1 AND Time <= @time2 " +
                $"ORDER BY Time DESC) a GROUP BY BillId ORDER BY BillId DESC;", new
                {
                    time1,
                    time2,
                    categoryId,
                    nameId,
                    supplierId,
                    specificationId,
                    siteId
                }, 60).ToList();
            var beforeData = ServerConfig.ApiDb.Query<MaterialStatistic>(
                "SELECT * FROM `material_balance` WHERE Time = @time1 " +
                $"{(categoryId != 0 ? "AND CategoryId = @categoryId " : "")}" +
                $"{(nameId != 0 ? "AND NameId = @nameId " : "")}" +
                $"{(supplierId != 0 ? "AND SupplierId = @supplierId " : "")}" +
                $"{(specificationId != 0 ? "AND SpecificationId = @specificationId " : "")}" +
                $"{(siteId != 0 ? "AND SiteId = @siteId " : "")}" +
                "ORDER BY BillId;", new
                {
                    time1,
                    categoryId,
                    nameId,
                    supplierId,
                    specificationId,
                    siteId
                });
            data.AddRange(beforeData.Where(x => data.All(y => x.BillId != y.BillId)).Select(z =>
            {
                z.Init();
                return z;
            }));
            //data.AddRange(beforeData.Where(x => data.All(y => x.BillId != y.BillId)));
            foreach (var da in data)
            {
                da.LastNumber = 0;
                da.LastPrice = 0;
                var bill = beforeData.FirstOrDefault(x => x.BillId == da.BillId);
                if (bill != null)
                {
                    da.LastNumber = bill.TodayNumber;
                    da.LastPrice = bill.TodayPrice;
                }
            }

            var d = data.Where(x => x.Valid()).OrderBy(x => x.BillId);
            //var t1 = d.Sum(x => x.IncreaseAmount);
            //var t2 = d.Sum(x => x.ConsumeAmount);
            //var t3 = d.Sum(x => x.TodayAmount);
            result.datas.AddRange(d);
            return result;
        }
    }
}