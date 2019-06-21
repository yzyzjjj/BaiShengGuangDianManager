using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 数据统计
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        public class RequestBody
        {
            public DateTime StartTime;
            public DateTime EndTime;
            //0 小时 - 秒  1 天 - 小时 2 月 天 
            public DateTime DataType;
        }
        // POST: api/Trend
        [HttpPost]
        public DataResult Trend()
        {
            var result = new DataResult();
            //result.datas.AddRange(ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE MarkedDelete = 0;"));
            return result;
        }

        // POST: api/Process
        [HttpPost]
        public DataResult Process()
        {
            var result = new DataResult();
            //result.datas.AddRange(ServerConfig.ApiDb.Query<Surveyor>("SELECT * FROM `surveyor` WHERE MarkedDelete = 0;"));
            return result;
        }
    }
}