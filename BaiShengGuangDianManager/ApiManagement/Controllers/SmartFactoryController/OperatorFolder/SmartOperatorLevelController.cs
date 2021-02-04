using ApiManagement.Base.Helper;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.OperatorFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartOperatorLevelController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartOperatorLevel
        /// </summary>
        /// <param name="qId">ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartOperatorLevel([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartOperatorLevelHelper.GetMenu(qId, wId)
                : SmartOperatorLevelHelper.GetDetail(qId, wId));
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
        /// <param name="operatorLevels"></param>
        /// <returns></returns>
        // PUT: api/SmartOperatorLevel/Id/5
        [HttpPut]
        public Result PutSmartOperatorLevel([FromBody] IEnumerable<SmartOperatorLevel> operatorLevels)
        {
            if (operatorLevels == null || !operatorLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (operatorLevels.Any(x => x.Level.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelNotEmpty);
            }
            if (operatorLevels.GroupBy(x => x.Level).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelDuplicate);
            }

            var wId = operatorLevels.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = operatorLevels.Select(x => x.Level);
            var ids = operatorLevels.Select(x => x.Id);
            if (SmartOperatorLevelHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelIsExist);
            }

            var cnt = SmartOperatorLevelHelper.Instance.GetCountByIds(ids);
            if (cnt != operatorLevels.Count())
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var operatorLevel in operatorLevels)
            {
                operatorLevel.MarkedDateTime = markedDateTime;
                operatorLevel.Remark = operatorLevel.Remark ?? "";
            }
            SmartOperatorLevelHelper.Instance.Update(operatorLevels);
            WorkFlowHelper.Instance.OnSmartOperatorLevelChanged(operatorLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartOperatorLevel
        [HttpPost]
        public Result PostSmartOperatorLevel([FromBody] IEnumerable<SmartOperatorLevel> operatorLevels)
        {
            if (operatorLevels == null || !operatorLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (operatorLevels.Any(x => x.Level.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelNotEmpty);
            }
            if (operatorLevels.GroupBy(x => x.Level).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelDuplicate);
            }

            var wId = operatorLevels.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = operatorLevels.Select(x => x.Level);
            if (SmartOperatorLevelHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartOperatorLevelIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var operatorLevel in operatorLevels)
            {
                operatorLevel.CreateUserId = userId;
                operatorLevel.MarkedDateTime = markedDateTime;
                operatorLevel.Remark = operatorLevel.Remark ?? "";
            }
            SmartOperatorLevelHelper.Instance.Add(operatorLevels);
            WorkFlowHelper.Instance.OnSmartOperatorLevelChanged(operatorLevels);
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