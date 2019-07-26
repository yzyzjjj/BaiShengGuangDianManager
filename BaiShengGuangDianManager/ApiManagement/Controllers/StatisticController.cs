using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System;
using System.Linq;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 数据统计
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        public class StatisticRequest
        {
            //流程卡趋势图
            public int FlowCardId = 0;
            public int Order;

            //加工记录 0 = 所有    对比图 1,2,3
            public string DeviceId;
            public DateTime StartTime;
            public DateTime EndTime;
            public string Field;
            //0 小时 - 秒  1 天 - 小时 2 月 天 
            public int DataType;
            //是否为对比图 （ 0  1 ）
            public int Compare;
        }
        /// <summary>
        /// 趋势图
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Trend
        [HttpPost("Trend")]
        public DataResult Trend([FromBody] StatisticRequest requestBody)
        {
            var result = new DataResult();
            try
            {
                var fields = requestBody.Field.Split(",").Select(int.Parse);
                if (!fields.Any())
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }

                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = requestBody.DeviceId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                string sql;
                DateTime startTime;
                DateTime endTime;
                switch (requestBody.DataType)
                {
                    case 0:
                        #region 0 小时 - 秒
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
                        sql =
                            "SELECT Id, SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime.NoMinute();
                        endTime = requestBody.EndTime.NoMinute();
                        sql =
                            "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();
                        sql =
                            "SELECT Id, DATE(SendTime) SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";
                        #endregion
                        break;
                    default: return Result.GenError<DataResult>(Error.ParamError);
                }

                var data = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                {
                    requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);
                var scripts = data.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                scripts.Add(0);
                var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                    "SELECT a.VariableNameId, a.ScriptId, a.DictionaryId FROM `usually_dictionary` a JOIN `usually_dictionary_type` b ON a.VariableNameId = b.Id " +
                    "WHERE StatisticType = 1 AND a.VariableNameId IN @VariableNameId AND a.ScriptId IN @ScriptId;", new
                    {
                        ScriptId = scripts,
                        VariableNameId = fields,
                    });
                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                {
                    foreach (var da in data)
                    {
                        var jObject = new JObject();
                        jObject["time"] = da.SendTime;
                        foreach (var field in fields)
                        {
                            var key = "v" + field;
                            jObject[key] = 0;
                            var analysisData = da.AnalysisData;
                            if (analysisData != null)
                            {
                                //今日加工次数
                                var udd = usuallyDictionaries.FirstOrDefault(x =>
                                     x.ScriptId == da.ScriptId && x.VariableNameId == field);
                                var address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == field).DictionaryId;
                                var actAddress = address - 1;
                                var v = analysisData.vals[actAddress];
                                jObject[key] = v;
                            }
                        }
                        result.datas.Add(jObject);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        /// <summary>
        /// 趋势图 流程卡
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/FlowCardTrend
        [HttpPost("FlowCardTrend")]
        public DataResult FlowCardTrend([FromBody] StatisticRequest requestBody)
        {
            var result = new DataResult();
            try
            {
                var fields = requestBody.Field.Split(",").Select(int.Parse);
                if (!fields.Any() || requestBody.FlowCardId == 0)
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }

                var flowCardProcessStep = ServerConfig.ApiDb.Query<FlowCardProcessStep>(
                    "SELECT* FROM `flowcard_process_step` WHERE FlowCardId = @FlowCardId AND ProcessStepOrder = @ProcessStepOrder;", new
                    {
                        FlowCardId = requestBody.FlowCardId,
                        ProcessStepOrder = requestBody.Order
                    }).FirstOrDefault();


                if (flowCardProcessStep == null)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                requestBody.DeviceId = flowCardProcessStep.DeviceId.ToString();
                if (flowCardProcessStep.ProcessTime == default(DateTime))
                {
                    return Result.GenError<DataResult>(Error.ProcessNotStart);
                }
                requestBody.StartTime = flowCardProcessStep.ProcessTime;
                requestBody.EndTime = flowCardProcessStep.ProcessEndTime == default(DateTime) ? DateTime.Now : flowCardProcessStep.ProcessEndTime;
                requestBody.DataType = (requestBody.EndTime - requestBody.StartTime).TotalDays > 1 ? 1 : 0;

                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = requestBody.DeviceId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                string sql;
                DateTime startTime;
                DateTime endTime;
                switch (requestBody.DataType)
                {
                    case 0:
                        #region 0 小时 - 秒
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
                        sql =
                            "SELECT Id, SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime.NoMinute();
                        endTime = requestBody.EndTime.NoMinute();
                        sql =
                            "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();
                        sql =
                            "SELECT Id, DATE(SendTime) SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";
                        #endregion
                        break;
                    default: return Result.GenError<DataResult>(Error.ParamError);
                }

                var data = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                {
                    requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);
                var scripts = data.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                scripts.Add(0);
                var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                    "SELECT a.VariableNameId, a.ScriptId, a.DictionaryId FROM `usually_dictionary` a JOIN `usually_dictionary_type` b ON a.VariableNameId = b.Id " +
                    "WHERE StatisticType = 1 AND a.VariableNameId IN @VariableNameId AND a.ScriptId IN @ScriptId;", new
                    {
                        ScriptId = scripts,
                        VariableNameId = fields,
                    });
                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                {
                    foreach (var da in data)
                    {
                        var jObject = new JObject();
                        jObject["time"] = da.SendTime;
                        foreach (var field in fields)
                        {
                            var key = "v" + field;
                            jObject[key] = 0;
                            var analysisData = da.AnalysisData;
                            if (analysisData != null)
                            {
                                //今日加工次数
                                var udd = usuallyDictionaries.FirstOrDefault(x =>
                                     x.ScriptId == da.ScriptId && x.VariableNameId == field);
                                var address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == field).DictionaryId;
                                var actAddress = address - 1;
                                var v = analysisData.vals[actAddress];
                                jObject[key] = v;
                            }
                        }
                        result.datas.Add(jObject);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        /// <summary>
        /// 加工记录
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Process
        [HttpPost("Process")]
        public DataResult Process([FromBody] StatisticRequest requestBody)
        {
            var result = new DataResult();
            try
            {
                if (requestBody.DeviceId != "0")
                {
                    if (requestBody.Compare == 0)
                    {
                        var cnt =
                            ServerConfig.ApiDb
                                .Query<int>(
                                    "SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;",
                                    new { id = requestBody.DeviceId }).FirstOrDefault();
                        if (cnt == 0)
                        {
                            return Result.GenError<DataResult>(Error.DeviceNotExist);
                        }
                    }
                    else
                    {
                        var deviceIds = requestBody.DeviceId.Split(",");
                        var cnt =
                            ServerConfig.ApiDb
                                .Query<int>(
                                    "SELECT COUNT(1) FROM `device_library` WHERE Id IN @id AND `MarkedDelete` = 0;",
                                    new { id = deviceIds }).FirstOrDefault();
                        if (cnt != deviceIds.Length)
                        {
                            return Result.GenError<DataResult>(Error.DeviceNotExist);
                        }
                    }
                }

                string processSql;
                DateTime startTime;
                DateTime endTime;
                switch (requestBody.DataType)
                {
                    case 0:

                        #region 0 小时 - 秒
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
                        if (requestBody.DeviceId == "0")
                        {
                            processSql =
                                " SELECT `Time`, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, SUM(`TotalProcessTime`) " +
                                "`TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` WHERE Time >= @startTime AND Time <= @endTime GROUP BY Time ORDER BY Time;";
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                processSql =
                                    "SELECT `Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` " +
                                    "WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time";
                            }
                            else
                            {
                                processSql =
                                    "SELECT `Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` " +
                                    "WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime GROUP BY DeviceId ,Time ORDER BY Time";
                            }
                        }

                        break;

                    #endregion

                    case 1:

                        #region 1 天 - 小时

                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        if (requestBody.DeviceId == "0")
                        {
                            processSql =
                                "SELECT DATE_FORMAT(Time, '%Y-%m-%d %H:00:00') Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, " +
                                "SUM(`ProcessTime`) `ProcessTime`, SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM " +
                                "( SELECT * FROM `npc_monitoring_process` WHERE Time >= @startTime AND Time <= @endTime ORDER BY Time DESC ) " +
                                "a GROUP BY DeviceId, DATE(Time), HOUR (Time) ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                processSql =
                                    "SELECT DATE_FORMAT(Time, '%Y-%m-%d %H:00:00') Time, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` " +
                                    "FROM ( SELECT * FROM `npc_monitoring_process` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime " +
                                    "ORDER BY Time DESC ) a GROUP BY DATE(Time), HOUR (Time) ORDER BY Time;";
                            }
                            else
                            {
                                processSql =
                                    "SELECT DATE_FORMAT(Time, '%Y-%m-%d %H:00:00') Time, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` " +
                                    "FROM ( SELECT * FROM `npc_monitoring_process` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime " +
                                    "ORDER BY Time DESC ) a GROUP BY DeviceId, DATE(Time), HOUR (Time) ORDER BY Time;";
                            }
                        }

                        #endregion

                        break;
                    case 2:

                        #region 2 月 天 

                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();
                        if (requestBody.DeviceId == "0")
                        {
                            processSql =
                                "SELECT DATE(Time) Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM ( SELECT * FROM `npc_monitoring_process` " +
                                "WHERE Time >= @startTime AND Time <= @endTime ORDER BY Time DESC ) a GROUP BY DeviceId, DATE(Time) ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                processSql =
                                    "SELECT * FROM ( SELECT * FROM `npc_monitoring_process` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time DESC ) " +
                                    "a GROUP BY DATE(Time) ORDER BY Time;";
                            }
                            else
                            {
                                processSql =
                                    "SELECT * FROM ( SELECT * FROM `npc_monitoring_process` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time DESC ) " +
                                    "a GROUP BY DeviceId, DATE(Time) ORDER BY Time;";
                            }
                        }

                        #endregion

                        break;
                    default: return Result.GenError<DataResult>(Error.ParamError);
                }

                if (requestBody.Compare == 0)
                {
                    var data = ServerConfig.ApiDb.Query<MonitoringProcess>(processSql, new
                    {
                        DeviceId = requestBody.DeviceId,
                        startTime,
                        endTime
                    }, 60);
                    result.datas.AddRange(data);
                }
                else
                {
                    var data = ServerConfig.ApiDb.Query<MonitoringProcess>(processSql, new
                    {
                        DeviceId = requestBody.DeviceId.Split(","),
                        startTime,
                        endTime
                    }, 60);
                    result.datas.AddRange(data);
                }
                return result;
            }
            catch (Exception e)
            {
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }
    }
}