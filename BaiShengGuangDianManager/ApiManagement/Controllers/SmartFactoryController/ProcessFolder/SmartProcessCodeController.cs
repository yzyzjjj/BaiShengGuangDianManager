using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.SmartFactoryController.ProcessFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartProcessCodeController : ControllerBase
    {
        // GET: api/SmartProcessCode
        [HttpGet]
        public DataResult GetSmartProcessCode([FromQuery]int qId, int cId, int wId, bool menu)
        {
            var result = new DataResult();
            var data = SmartProcessCodeHelper.GetDetail(qId, cId, wId);
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new { x.Id, x.Code }));
            }
            else
            {
                var processIds = data.SelectMany(x => x.ProcessIdList).Distinct();
                if (processIds.Any())
                {
                    var processList = SmartProcessCodeCategoryProcessHelper.GetProcess(processIds);
                    if (processList.Any())
                    {
                        foreach (var d in data)
                        {
                            foreach (var processId in d.ProcessIdList)
                            {
                                var process = processList.FirstOrDefault(x => x.Id == processId);
                                if (process != null)
                                {
                                    d.ProcessList.Add(process.Process);
                                }
                            }
                        }
                    }
                }
                result.datas.AddRange(data);
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProcessCodeNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProcessCode
        [HttpPut]
        public Result PutSmartProcessCode([FromBody] IEnumerable<SmartProcessCode> processCodes)
        {
            if (processCodes == null || !processCodes.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processCodes.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotEmpty);
            }
            if (processCodes.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeDuplicate);
            }

            var cId = processCodes.FirstOrDefault()?.CategoryId ?? 0;
            var sames = processCodes.Select(x => x.Code);
            var ids = processCodes.Select(x => x.Id);
            if (SmartProcessCodeHelper.GetHaveSame(cId, sames, ids))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeIsExist);
            }

            var cnt = SmartProcessCodeHelper.Instance.GetCountByIds(ids);
            if (cnt != processCodes.Count())
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotExist);
            }

            var markedDateTime = DateTime.Now;
            foreach (var processCode in processCodes)
            {
                processCode.MarkedDateTime = markedDateTime;
                processCode.Remark = processCode.Remark ?? "";
            }

            SmartProcessCodeHelper.Instance.Update(processCodes);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProcessCode
        [HttpPost]
        public Result PostSmartProcessCode([FromBody] IEnumerable<SmartProcessCode> processCodes)
        {
            if (processCodes == null || !processCodes.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (processCodes.Any(x => x.Code.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotEmpty);
            }
            if (processCodes.GroupBy(x => x.Code).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeDuplicate);
            }

            var cId = processCodes.FirstOrDefault()?.CategoryId ?? 0;
            var sames = processCodes.Select(x => x.Code);
            if (SmartProcessCodeHelper.GetHaveSame(cId, sames))
            {
                return Result.GenError<Result>(Error.SmartProcessCodeIsExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var processCode in processCodes)
            {
                processCode.CreateUserId = userId;
                processCode.MarkedDateTime = markedDateTime;
                processCode.Remark = processCode.Remark ?? "";
            }
            SmartProcessCodeHelper.Instance.Add(processCodes);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProcessCode
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProcessCode([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProcessCodeHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProcessCodeNotExist);
            }
            SmartProcessCodeHelper.Instance.Delete(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}