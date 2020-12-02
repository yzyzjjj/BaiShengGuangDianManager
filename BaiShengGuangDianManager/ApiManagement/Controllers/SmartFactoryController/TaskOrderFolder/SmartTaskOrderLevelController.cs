using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.TaskOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SmartTaskOrderLevelController : ControllerBase
    {
        // GET: api/SmartTaskOrderLevel
        [HttpGet]
        public DataResult GetSmartTaskOrderLevel([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Level`, `Order` FROM `t_task_order_level` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY `Order`;"
                : $"SELECT * FROM `t_task_order_level` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY `Order`;";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartTaskOrderLevel>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartTaskOrderLevelNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="smartTaskOrderLevels"></param>
        /// <returns></returns>
        // PUT: api/SmartTaskOrderLevel/Id/5
        [HttpPut]
        public Result PutSmartTaskOrderLevel([FromBody] IEnumerable<SmartTaskOrderLevel> smartTaskOrderLevels)
        {
            if (smartTaskOrderLevels == null || !smartTaskOrderLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (smartTaskOrderLevels.Any(x => x.Order == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var smartTaskOrderLevelIds = smartTaskOrderLevels.Select(x => x.Id);
            var data = SmartTaskOrderLevelHelper.Instance.GetByIds<SmartTaskOrderLevel>(smartTaskOrderLevelIds);
            if (data.Count() != smartTaskOrderLevels.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelNotExist);
            }

            if(data.Any(x => x.Order == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartTaskOrderLevel in smartTaskOrderLevels)
            {
                smartTaskOrderLevel.CreateUserId = createUserId;
                smartTaskOrderLevel.MarkedDateTime = markedDateTime;
            }
            SmartTaskOrderLevelHelper.Instance.Update(smartTaskOrderLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartTaskOrderLevel
        [HttpPost]
        public Result PostSmartTaskOrderLevel([FromBody] IEnumerable<SmartTaskOrderLevel> smartTaskOrderLevels)
        {
            if (smartTaskOrderLevels.Any(x => x.Order == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartTaskOrderLevel in smartTaskOrderLevels)
            {
                smartTaskOrderLevel.CreateUserId = createUserId;
                smartTaskOrderLevel.MarkedDateTime = markedDateTime;
            }
            SmartTaskOrderLevelHelper.Instance.Add(smartTaskOrderLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartTaskOrderLevel
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartTaskOrderLevel([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var data = SmartTaskOrderLevelHelper.Instance.GetByIds<SmartTaskOrderLevel>(ids);
            if (!data.Any())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelNotExist);
            }
            SmartTaskOrderLevelHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}