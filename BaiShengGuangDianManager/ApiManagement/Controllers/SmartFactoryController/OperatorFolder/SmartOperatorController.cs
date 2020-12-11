using ApiManagement.Base.Server;
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
using ApiManagement.Base.Helper;

namespace ApiManagement.Controllers.SmartFactoryController.OperatorFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartOperatorController : ControllerBase
    {
        // GET: api/SmartOperator
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qId"></param>
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
        public DataResult GetSmartOperator([FromQuery]int qId, bool menu, bool add, string number, string name, int levelId, int processId, int condition, SmartOperatorState state)
        {
            var result = new DataResult();
            string sql;
            if (menu && add)
            {
                sql = $"SELECT a.Id, a.`Name` FROM `t_user` a LEFT JOIN(SELECT * FROM `t_operator` WHERE MarkedDelete = 0) b ON a.Id = b.UserId " +
                      $"WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY b.ProcessId, b.Priority, a.Id;";
            }
            else if (menu)
            {
                sql = $"SELECT Id, `Name` FROM `t_operator` WHERE MarkedDelete = 0{(qId == 0 ? "" : " AND Id = @qId")} ORDER BY ProcessId, Priority, a.Id;";
            }
            else if (add)
            {
                sql = $"SELECT a.* FROM `t_user` a LEFT JOIN (SELECT * FROM `t_operator` WHERE MarkedDelete = 0) b ON a.Id = b.UserId " +
                      $"WHERE a.MarkedDelete = 0 AND ISNULL(b.Id) ORDER BY b.ProcessId, b.Priority, a.Id;";
            }
            else
            {
                var paramList = new List<string>();
                if (qId != 0)
                {
                    paramList.Add(" AND a.Id = @qId");
                }
                if (state != 0)
                {
                    paramList.Add(" AND a.State = @state");
                }
                if (!number.IsNullOrEmpty())
                {
                    if (condition == 0)
                    {
                        paramList.Add($" AND b.`Number` = @number");
                    }
                    else
                    {
                        number = $"%{number}%";
                        paramList.Add($" AND b.`Number` LIKE @number");
                    }
                }
                if (!name.IsNullOrEmpty())
                {
                    if (condition == 0)
                    {
                        paramList.Add($" AND b.`Name` = @name");
                    }
                    else
                    {
                        name = $"%{name}%";
                        paramList.Add($" AND b.`Name` LIKE @name");
                    }
                }
                if (levelId != 0)
                {
                    paramList.Add($" AND a.LevelId = @levelId");
                }
                if (processId != 0)
                {
                    paramList.Add($" AND a.ProcessId = @processId");
                }
                sql = $"SELECT a.*, b.`Number`, b.`Name`, b.`Account`, c.Process, d.`Level` FROM `t_operator` a " +
                      $"JOIN `t_user` b ON a.UserId = b.Id " +
                      $"JOIN `t_process` c ON a.ProcessId = c.Id " +
                      $"JOIN `t_operator_level` d ON a.LevelId = d.Id " +
                      $"WHERE a.MarkedDelete = 0{(paramList.Join(""))} ORDER BY a.ProcessId, a.Priority, a.Id;";
            }

            result.datas.AddRange(menu
                ? ServerConfig.ApiDb.Query<dynamic>(sql, new { qId })
                : ServerConfig.ApiDb.Query<SmartOperatorDetail>(sql, new { qId, number, name, levelId, processId, state }));
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
        /// <param name="smartOperators"></param>
        /// <returns></returns>
        // PUT: api/SmartOperator/Id/5
        [HttpPut]
        public Result PutSmartOperator([FromBody] IEnumerable<SmartOperator> smartOperators)
        {
            if (smartOperators == null || !smartOperators.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var smartOperatorIds = smartOperators.Select(x => x.Id);
            var data = SmartOperatorHelper.Instance.GetByIds<SmartOperator>(smartOperatorIds);
            if (data.Count() != smartOperators.Count())
            {
                return Result.GenError<Result>(Error.SmartOperatorNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartOperator in smartOperators)
            {
                smartOperator.CreateUserId = createUserId;
                smartOperator.MarkedDateTime = markedDateTime;
            }
            SmartOperatorHelper.Instance.Update(smartOperators);
            WorkFlowHelper.Instance.OnSmartOperatorChanged(smartOperators);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartOperator
        [HttpPost]
        public Result PostSmartOperator([FromBody] IEnumerable<SmartOperator> smartOperators)
        {
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartOperator in smartOperators)
            {
                smartOperator.CreateUserId = createUserId;
                smartOperator.MarkedDateTime = markedDateTime;
            }
            SmartOperatorHelper.Instance.Add(smartOperators);
            WorkFlowHelper.Instance.OnSmartOperatorChanged(smartOperators);
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