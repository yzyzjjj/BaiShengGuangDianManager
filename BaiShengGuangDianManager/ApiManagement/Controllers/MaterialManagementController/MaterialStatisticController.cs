﻿using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
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
        public DataResult GetMaterialValuer([FromQuery] DateTime day, MaterialStatisticInterval interval)
        {
            var result = new DataResult();
            DateTime day1;
            DateTime day2;
            switch (interval)
            {
                case MaterialStatisticInterval.天:
                    day1 = day.Date;
                    day2 = day1.AddDays(-1);
                    break;
                case MaterialStatisticInterval.周:
                    day1 = day.WeekEndTime();
                    day2 = day.WeekBeginTime().AddDays(-1);
                    break;
                case MaterialStatisticInterval.月:
                    day1 = day.EndOfMonth();
                    day2 = day.EndOfLastMonth();
                    break;
                case MaterialStatisticInterval.年:
                    day1 = day.EndOfYear();
                    day2 = day.EndOfYear(-1);
                    break;
                default: return result;
            }
            var data = ServerConfig.ApiDb.Query<MaterialStatistic>(
                "SELECT *, SUM(Increase) Increase, SUM(IncreaseAmount) IncreaseAmount, SUM(Consume) Consume, SUM(ConsumeAmount) ConsumeAmount, SUM(CorrectIn) CorrectIn, SUM(CorrectInAmount) CorrectInAmount, SUM(CorrectCon) CorrectCon, SUM(CorrectConAmount) CorrectConAmount, SUM(Correct) Correct, SUM(CorrectAmount) CorrectAmount " +
                "FROM `material_balance` WHERE Time >= @day1 AND Time <= @day2 GROUP BY BillId ORDER BY BillId DESC;", new
                {
                    day1,
                    day2
                }).ToList();
            var beforeData = ServerConfig.ApiDb.Query<MaterialStatistic>(
                "SELECT * FROM `material_balance` WHERE Time = @day2 ORDER BY BillId;", new
                {
                    day2
                });
            data.AddRange(beforeData.Where(x => data.All(y => x.BillId != y.BillId)).Select(z =>
            {
                z.Init();
                return z;
            }));

            foreach (var bd in beforeData)
            {
                var bill = data.FirstOrDefault(x => x.BillId == bd.BillId);
                if (bill != null)
                {
                    bill.LastNumber = bd.TodayNumber;
                    bill.LastPrice = bd.TodayPrice;
                    bill.LastAmount = bd.TodayAmount;
                }
            }
            result.datas.AddRange(data);
            return result;
        }
    }
}