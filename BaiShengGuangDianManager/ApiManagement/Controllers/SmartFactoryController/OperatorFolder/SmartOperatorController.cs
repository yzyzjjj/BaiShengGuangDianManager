using ApiManagement.Base.Helper;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.SmartFactoryController.OperatorFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartOperatorController : ControllerBase
    {
        // GET: api/SmartOperator
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
        /// <param name="wId"></param>
        /// <param name="menu"></param>
        /// <param name="add"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        /// <param name="levelId"></param>
        /// <param name="processId"></param>
        /// <param name="state"> 1 正常 2 休息</param>
        /// <param name="condition"> 0 等于  1 包含</param>
        /// <returns></returns>
        [HttpGet]
        public DataResult GetSmartOperator([FromQuery]bool menu, bool add, int qId, int wId, int levelId = -1,
            int processId = -1, string number = "", string name = "", SmartOperatorState state = SmartOperatorState.全部, int condition = 0)
        {
            var result = new DataResult();
            result.datas.AddRange(menu
                ? SmartOperatorHelper.GetMenu(add, qId, wId, levelId, processId, number, name, state, condition)
                : SmartOperatorHelper.GetDetail(add, qId, wId, levelId, processId, number, name, state, condition));
            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartOperatorNotExist;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 自增Id
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        // PUT: api/SmartOperator/Id/5
        [HttpPut]
        public Result PutSmartOperator([FromBody] IEnumerable<SmartOperator> operators)
        {
            if (operators == null || !operators.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var ids = operators.Select(x => x.Id);
            var cnt = SmartOperatorHelper.Instance.GetCountByIds(ids);
            if (cnt != operators.Count())
            {
                return Result.GenError<Result>(Error.SmartOperatorNotExist);
            }
            var markedDateTime = DateTime.Now;
            foreach (var op in operators)
            {
                op.MarkedDateTime = markedDateTime;
                op.Remark = op.Remark ?? "";
            }
            SmartOperatorHelper.Instance.Update(operators);
            WorkFlowHelper.Instance.OnSmartOperatorChanged(operators);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartOperator
        [HttpPost]
        public Result PostSmartOperator([FromBody] IEnumerable<SmartOperator> operators)
        {
            if (operators == null || !operators.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (operators.Any(x => x.UserId == 0))
            {
                return Result.GenError<Result>(Error.SmartOperatorNotEmpty);
            }

            var wId = operators.FirstOrDefault()?.WorkshopId ?? 0;
            var uIds = operators.Select(x => x.UserId.ToString());
            if (SmartOperatorHelper.GetHaveSame(wId, uIds))
            {
                return Result.GenError<Result>(Error.SmartOperatorDuplicate);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var op in operators)
            {
                op.CreateUserId = userId;
                op.MarkedDateTime = markedDateTime;
                op.Remark = op.Remark ?? "";
            }

            SmartOperatorHelper.Instance.Add(operators);
            WorkFlowHelper.Instance.OnSmartOperatorChanged(operators);
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartOperator
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartOperator([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var operators = SmartOperatorHelper.Instance.GetByIds<SmartOperator>(ids);
            if (!operators.Any())
            {
                return Result.GenError<Result>(Error.SmartOperatorNotExist);
            }
            SmartOperatorHelper.Instance.Delete(ids);
            WorkFlowHelper.Instance.OnSmartOperatorChanged(operators);
            return Result.GenError<Result>(Error.Success);
        }
    }
}