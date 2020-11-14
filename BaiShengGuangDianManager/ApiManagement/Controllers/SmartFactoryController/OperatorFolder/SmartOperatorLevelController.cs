using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;

namespace ApiManagement.Controllers.SmartFactoryController.OperatorFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SmartOperatorLevelController : ControllerBase
    {
        // GET: api/SmartOperatorLevel
        [HttpGet]
        public DataResult GetSmartOperatorLevel([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = menu ? $"SELECT Id, `Level`, `Order` FROM `t_operator_level` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY `Order`;"
                : $"SELECT * FROM `t_operator_level` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY `Order`;";
            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartOperatorLevel>(sql, new { qId }));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartOperatorLevelNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="smartOperatorLevels"></param>
        /// <returns></returns>
        // PUT: api/SmartOperatorLevel/Id/5
        [HttpPut]
        public Result PutSmartOperatorLevel([FromBody] IEnumerable<SmartOperatorLevel> smartOperatorLevels)
        {
            if (smartOperatorLevels == null || !smartOperatorLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartOperatorLevelIds = smartOperatorLevels.Select(x => x.Id);
            var data = SmartOperatorLevelHelper.Instance.GetByIds<SmartOperatorLevel>(smartOperatorLevelIds);
            if (data.Count() != smartOperatorLevels.Count())
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartOperatorLevel in smartOperatorLevels)
            {
                smartOperatorLevel.CreateUserId = createUserId;
                smartOperatorLevel.MarkedDateTime = markedDateTime;
            }
            SmartOperatorLevelHelper.Instance.Update(smartOperatorLevels);
            WorkFlowHelper.Instance.OnSmartOperatorLevelChanged(smartOperatorLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartOperatorLevel
        [HttpPost]
        public Result PostSmartOperatorLevel([FromBody] IEnumerable<SmartOperatorLevel> smartOperatorLevels)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartOperatorLevel in smartOperatorLevels)
            {
                smartOperatorLevel.CreateUserId = createUserId;
                smartOperatorLevel.MarkedDateTime = markedDateTime;
            }
            SmartOperatorLevelHelper.Instance.Add(smartOperatorLevels);
            WorkFlowHelper.Instance.OnSmartOperatorLevelChanged(smartOperatorLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartOperatorLevel
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartOperatorLevel([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var data = SmartOperatorLevelHelper.Instance.GetByIds<SmartOperatorLevel>(ids);
            if (!data.Any())
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelNotExist);
            }
            SmartOperatorLevelHelper.Instance.Delete(ids);
            WorkFlowHelper.Instance.OnSmartOperatorLevelChanged(data);
            return Result.GenError<Result>(Error.Success);
        }
    }
}