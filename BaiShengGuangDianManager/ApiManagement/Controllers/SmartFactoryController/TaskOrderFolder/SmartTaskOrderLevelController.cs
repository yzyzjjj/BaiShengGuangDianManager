using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.BaseModel;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.TaskOrderFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartTaskOrderLevelController : ControllerBase
    {
        /// <summary>
        /// GET: api/SmartTaskOrderLevel
        /// </summary>
        /// <param name="qId">ID</param>
        /// <param name="wId">车间ID</param>
        /// <param name="menu">是否菜单</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartTaskOrderLevel([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartTaskOrderLevelHelper.GetMenu(qId, wId)
                : SmartTaskOrderLevelHelper.GetDetail(qId, wId));
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
        /// <param name="taskOrderLevels"></param>
        /// <returns></returns>
        // PUT: api/SmartTaskOrderLevel/Id/5
        [HttpPut]
        public Result PutSmartTaskOrderLevel([FromBody] IEnumerable<SmartTaskOrderLevel> taskOrderLevels)
        {
            if (taskOrderLevels == null || !taskOrderLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (taskOrderLevels.Any(x => x.Level.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelNotEmpty);
            }
            if (taskOrderLevels.GroupBy(x => x.Level).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelDuplicate);
            }

            var wId = taskOrderLevels.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = taskOrderLevels.Select(x => x.Level);
            var ids = taskOrderLevels.Select(x => x.Id);
            if (SmartTaskOrderLevelHelper.GetHaveSame(wId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelIsExist);
            }

            var cnt = SmartTaskOrderLevelHelper.Instance.GetCountByIds(ids);
            if (cnt != taskOrderLevels.Count())
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var taskOrderLevel in taskOrderLevels)
            {
                taskOrderLevel.MarkedDateTime = markedDateTime;
                taskOrderLevel.Remark = taskOrderLevel.Remark ?? "";
            }
            SmartTaskOrderLevelHelper.Instance.Update(taskOrderLevels);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartTaskOrderLevel
        [HttpPost]
        public Result PostSmartTaskOrderLevel([FromBody] IEnumerable<SmartTaskOrderLevel> operatorLevels)
        {
            if (operatorLevels == null || !operatorLevels.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (operatorLevels.Any(x => x.Level.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelNotEmpty);
            }
            if (operatorLevels.GroupBy(x => x.Level).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelDuplicate);
            }

            var wId = operatorLevels.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = operatorLevels.Select(x => x.Level);
            if (SmartTaskOrderLevelHelper.GetHaveSame(wId, sames))
            {
                return Result.GenError<Result>(Error.SmartTaskOrderLevelIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var operatorLevel in operatorLevels)
            {
                operatorLevel.CreateUserId = userId;
                operatorLevel.MarkedDateTime = markedDateTime;
                operatorLevel.Remark = operatorLevel.Remark ?? "";
            }
            SmartTaskOrderLevelHelper.Instance.Add(operatorLevels);
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