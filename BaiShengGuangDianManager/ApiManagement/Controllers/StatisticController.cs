﻿using ApiManagement.Base.Server;
using ApiManagement.Models;
using ApiManagement.Models.Analysis;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiManagement.Controllers
{
    /// <summary>
    /// 数据统计
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        /// <summary>
        /// 看板数据
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Kanban
        [HttpGet("Kanban")]
        public object Kanban()
        {
            return new
            {
                errno = 0,
                errmsg = "成功",
                data = ServerConfig.MonitoringKanban
            };
        }

        public class StatisticRequest
        {
            //流程卡趋势图
            public int FlowCardId = 0;
            public int Order;

            public string WorkshopName;
            //加工记录 0 = 所有    对比图 1,2,3
            public string DeviceId;
            public DateTime StartTime;
            public DateTime EndTime;
            public DateTime StartTime1;
            public DateTime EndTime1;
            public string Field;
            //趋势图 0 小时 - 秒  1 天 - 小时 2 月 天 
            //加工记录 0 秒  1 小时 2 天  3 分
            public int DataType;
            /// <summary>
            /// 是否为对比图 （ 0  1 ）
            /// 趋势图、加工记录  1 机台号对比
            /// 故障统计 0 无数据对比 1 车间对比 2 机台号对比 2 机台号对比 3 日数据 4 周数据 5 月数据
            /// </summary>
            public int Compare;
        }
        /// <summary>
        /// 趋势图   机台号对比
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
                    ServerConfig.ApiDb
                        .Query<int>(
                            "SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;",
                            new { id = requestBody.DeviceId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var startTime = requestBody.StartTime;
                var endTime = requestBody.EndTime;

                var sql =
                    "SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";

                var data = new List<MonitoringAnalysis>();
                var tStartTime = startTime;
                var tEndTime = tStartTime.AddMinutes(30);
                var tasks = new List<Task>();
                while (true)
                {
                    if (tEndTime > endTime)
                    {
                        tEndTime = endTime;
                    }

                    var task = Task.Factory.StartNew(() =>
                    {
                        data.AddRange(ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                        {
                            requestBody.DeviceId,
                            startTime = tStartTime,
                            endTime = tEndTime
                        }, 60));
                    });
                    tasks.Add(task);
                    if (tEndTime == endTime)
                    {
                        break;
                    }

                    tStartTime = tEndTime;
                    tEndTime = tStartTime.AddMinutes(30);
                }

                Task.WaitAll(tasks.ToArray());
                var scripts = data.OrderBy(x => x.Id).GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                scripts.Add(0);
                var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                    "SELECT a.VariableNameId, a.ScriptId, a.DictionaryId, a.VariableTypeId FROM `usually_dictionary` a JOIN `usually_dictionary_type` b ON a.VariableNameId = b.Id " +
                    "WHERE StatisticType = 1 AND a.VariableNameId IN @VariableNameId AND a.ScriptId IN @ScriptId;", new
                    {
                        ScriptId = scripts,
                        VariableNameId = fields,
                    });

                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                {
                    scripts.Remove(0);
                    var fieldDid = new Dictionary<Tuple<int, int>, Tuple<int, int>>();
                    foreach (var field in fields)
                    {
                        foreach (var scriptId in scripts)
                        {
                            var udd = usuallyDictionaries.FirstOrDefault(x =>
                                x.ScriptId == scriptId && x.VariableNameId == field);
                            var usuallyDictionary = udd ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == field);
                            fieldDid.Add(new Tuple<int, int>(field, scriptId), new Tuple<int, int>(usuallyDictionary.VariableTypeId, usuallyDictionary.DictionaryId));
                        }
                    }
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
                                var address = fieldDid[new Tuple<int, int>(field, da.ScriptId)];
                                var actAddress = address.Item2 - 1;
                                var v = 0;
                                if (address.Item1 == 1)
                                {
                                    v = analysisData.vals[actAddress];
                                }
                                else if (address.Item1 == 2)
                                {
                                    v = analysisData.ins[actAddress];
                                }
                                else if (address.Item1 == 3)
                                {
                                    v = analysisData.outs[actAddress];
                                }

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

            #region old

            //var result = new DataResult();
            //try
            //{
            //    var fields = requestBody.Field.Split(",").Select(int.Parse);
            //    if (!fields.Any())
            //    {
            //        return Result.GenError<DataResult>(Error.ParamError);
            //    }

            //    if (requestBody.Compare == 0)
            //    {
            //        var cnt =
            //            ServerConfig.ApiDb
            //                .Query<int>(
            //                    "SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;",
            //                    new { id = requestBody.DeviceId }).FirstOrDefault();
            //        if (cnt == 0)
            //        {
            //            return Result.GenError<DataResult>(Error.DeviceNotExist);
            //        }
            //    }
            //    else
            //    {
            //        if (requestBody.DeviceId.IsNullOrEmpty())
            //        {
            //            return Result.GenError<DataResult>(Error.DeviceNotExist);
            //        }

            //        var deviceIds = requestBody.DeviceId.Split(",");
            //        var cnt =
            //            ServerConfig.ApiDb
            //                .Query<int>(
            //                    "SELECT COUNT(1) FROM `device_library` WHERE Id IN @id AND `MarkedDelete` = 0;",
            //                    new { id = deviceIds }).FirstOrDefault();
            //        if (cnt != deviceIds.Length)
            //        {
            //            return Result.GenError<DataResult>(Error.DeviceNotExist);
            //        }
            //    }

            //    string sql;
            //    DateTime startTime;
            //    DateTime endTime;
            //    switch (requestBody.DataType)
            //    {
            //        case 0:
            //            #region 0 小时 - 秒
            //            startTime = requestBody.StartTime;
            //            endTime = requestBody.EndTime;

            //            if (requestBody.Compare == 0)
            //            {
            //                sql =
            //                    "SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
            //            }
            //            else
            //            {
            //                sql =
            //                    "SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId IN @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
            //            }

            //            break;
            //        #endregion
            //        case 1:
            //            #region 1 天 - 小时
            //            //startTime = requestBody.StartTime.DayBeginTime();
            //            //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
            //            startTime = requestBody.StartTime.NoMinute();
            //            endTime = requestBody.EndTime.NoMinute();

            //            if (requestBody.Compare == 0)
            //            {
            //                sql =
            //                    "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
            //            }
            //            else
            //            {
            //                sql =
            //                    "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId IN @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY `DeviceId`, DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
            //            }

            //            #endregion
            //            break;
            //        case 2:
            //            #region 2 月 天 
            //            startTime = requestBody.StartTime.StartOfMonth();
            //            endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();

            //            if (requestBody.Compare == 0)
            //            {
            //                sql =
            //                    "SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

            //            }
            //            else
            //            {
            //                sql =
            //                    "SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY `DeviceId`, DATE(SendTime) ORDER BY SendTime";
            //            }
            //            #endregion
            //            break;
            //        default: return Result.GenError<DataResult>(Error.ParamError);
            //    }

            //    IEnumerable<MonitoringAnalysis> data;
            //    if (requestBody.Compare == 0)
            //    {
            //        data = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
            //        {
            //            DeviceId = requestBody.DeviceId,
            //            startTime,
            //            endTime
            //        }, 60);
            //    }
            //    else
            //    {
            //        data = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
            //        {
            //            DeviceId = requestBody.DeviceId.Split(","),
            //            startTime,
            //            endTime
            //        }, 60);
            //    }

            //    var scripts = data.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
            //    scripts.Add(0);
            //    var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
            //        "SELECT a.VariableNameId, a.ScriptId, a.DictionaryId FROM `usually_dictionary` a JOIN `usually_dictionary_type` b ON a.VariableNameId = b.Id " +
            //        "WHERE StatisticType = 1 AND a.VariableNameId IN @VariableNameId AND a.ScriptId IN @ScriptId;", new
            //        {
            //            ScriptId = scripts,
            //            VariableNameId = fields,
            //        });
            //    if (usuallyDictionaries != null && usuallyDictionaries.Any())
            //    {
            //        foreach (var da in data)
            //        {
            //            var jObject = new JObject();
            //            jObject["time"] = da.SendTime;
            //            foreach (var field in fields)
            //            {
            //                var key = "v" + field;
            //                jObject[key] = 0;
            //                var analysisData = da.AnalysisData;
            //                if (analysisData != null)
            //                {
            //                    //今日加工次数
            //                    var udd = usuallyDictionaries.FirstOrDefault(x =>
            //                         x.ScriptId == da.ScriptId && x.VariableNameId == field);
            //                    var address = udd?.DictionaryId ?? usuallyDictionaries.First(x => x.ScriptId == 0 && x.VariableNameId == field).DictionaryId;
            //                    var actAddress = address - 1;
            //                    var v = analysisData.vals[actAddress];
            //                    jObject[key] = v;
            //                }
            //            }
            //            result.datas.Add(jObject);
            //        }
            //    }

            //    return result;
            //}
            //catch (Exception e)
            //{
            //    return Result.GenError<DataResult>(Error.ParamError);
            //}

            #endregion
        }

        /// <summary>
        /// 趋势图  时间对比
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Trend
        [HttpPost("TimeTrend")]
        public object TimeTrend([FromBody] StatisticRequest requestBody)
        {
            try
            {
                var fields = requestBody.Field.Split(",").Select(int.Parse);
                if (!fields.Any())
                {
                    return Result.GenError<Result>(Error.ParamError);
                }

                var cnt =
                    ServerConfig.ApiDb
                        .Query<int>(
                            "SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;",
                            new { id = requestBody.DeviceId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<Result>(Error.DeviceNotExist);
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
                            "SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";

                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime.NoMinute();
                        endTime = requestBody.EndTime.NoMinute();

                        sql =
                            "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();

                        sql =
                            "SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    default: return Result.GenError<Result>(Error.ParamError);
                }

                var data1 = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                {
                    DeviceId = requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);

                switch (requestBody.DataType)
                {
                    case 0:
                        #region 0 小时 - 秒
                        startTime = requestBody.StartTime1;
                        endTime = requestBody.EndTime1;

                        sql =
                            "SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";

                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime1.NoMinute();
                        endTime = requestBody.EndTime1.NoMinute();

                        sql =
                            "SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime1.StartOfMonth();
                        endTime = requestBody.EndTime1.StartOfNextMonth().DayBeginTime();

                        sql =
                            "SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    default: return Result.GenError<Result>(Error.ParamError);
                }

                var data2 = ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                {
                    DeviceId = requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);

                var scripts = data1.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
                scripts.AddRange(data2.GroupBy(x => x.ScriptId).Select(x => x.Key).ToList());
                scripts.Add(0);
                var usuallyDictionaries = ServerConfig.ApiDb.Query<UsuallyDictionary>(
                    "SELECT a.VariableNameId, a.ScriptId, a.DictionaryId FROM `usually_dictionary` a JOIN `usually_dictionary_type` b ON a.VariableNameId = b.Id " +
                    "WHERE StatisticType = 1 AND a.VariableNameId IN @VariableNameId AND a.ScriptId IN @ScriptId;", new
                    {
                        ScriptId = scripts,
                        VariableNameId = fields,
                    });
                var rData1 = new List<object>();
                var rData2 = new List<object>();
                if (usuallyDictionaries != null && usuallyDictionaries.Any())
                {
                    foreach (var da in data1)
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
                        rData1.Add(jObject);
                    }

                    foreach (var da in data2)
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
                        rData2.Add(jObject);
                    }
                }

                return new
                {
                    errno = 0,
                    errmsg = "成功",
                    data1 = rData1,
                    data2 = rData2,
                };
            }
            catch (Exception e)
            {
                return Result.GenError<Result>(Error.ParamError);
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
                var startTime = flowCardProcessStep.ProcessTime;
                var endTime = flowCardProcessStep.ProcessEndTime == default(DateTime) ? DateTime.Now : flowCardProcessStep.ProcessEndTime;
                var cnt =
                    ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `device_library` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = requestBody.DeviceId }).FirstOrDefault();
                if (cnt == 0)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var sql = "SELECT Id, SendTime, `DATA`, ScriptId FROM `npc_monitoring_analysis` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
                var data = new List<MonitoringAnalysis>();
                var tStartTime = startTime;
                var tEndTime = tStartTime.AddMinutes(30);
                var tasks = new List<Task>();
                while (true)
                {
                    if (tEndTime > endTime)
                    {
                        tEndTime = endTime;
                    }

                    var task = Task.Factory.StartNew(() =>
                    {
                        data.AddRange(ServerConfig.ApiDb.Query<MonitoringAnalysis>(sql, new
                        {
                            requestBody.DeviceId,
                            startTime,
                            endTime
                        }, 60));
                    });
                    tasks.Add(task);
                    if (tEndTime == endTime)
                    {
                        break;
                    }

                    tStartTime = tEndTime;
                    tEndTime = tStartTime.AddMinutes(30);
                }

                Task.WaitAll(tasks.ToArray());
                var scripts = data.OrderBy(x => x.Id).GroupBy(x => x.ScriptId).Select(x => x.Key).ToList();
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
                IEnumerable<int> siteIds = null;
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
                else
                {
                    if (requestBody.WorkshopName != "")
                    {
                        siteIds = ServerConfig.ApiDb.Query<int>(
                            "SELECT Id FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                            {
                                SiteName = requestBody.WorkshopName
                            });
                    }
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
                        if (requestBody.DeviceId == "0")
                        {
                            if (requestBody.WorkshopName != "")
                            {
                                sql =
                                    " SELECT `Time`, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, SUM(`TotalProcessTime`) " +
                                    "`TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime GROUP BY Time ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    " SELECT `Time`, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, SUM(`TotalProcessTime`) " +
                                    "`TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` WHERE Time >= @startTime AND Time <= @endTime GROUP BY Time ORDER BY Time;";
                            }
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                sql =
                                    "SELECT `Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` " +
                                    "WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time";
                            }
                            else
                            {
                                sql =
                                    "SELECT `Time`, `DeviceId`, `ProcessCount`, `TotalProcessCount`, `ProcessTime`, `TotalProcessTime`, `State`, `Rate`, `Use`, `Total` FROM `npc_monitoring_process` " +
                                    "WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time";
                            }
                        }
                        #endregion
                        break;
                    case 1:
                        #region 1 天 - 小时
                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.EndTime.DayEndTime();
                        if (requestBody.DeviceId == "0")
                        {
                            if (requestBody.WorkshopName == "")
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_hour` " +
                                    "WHERE Time >= @startTime AND Time <= @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_hour` " +
                                    "WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_hour` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_hour` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                        }

                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.EndOfMonth();
                        if (requestBody.DeviceId == "0")
                        {
                            if (requestBody.WorkshopName == "")
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_day` " +
                                    "WHERE Time >= @startTime AND Time <= @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_day` " +
                                    "WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time < @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_day` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_day` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                        }

                        #endregion
                        break;
                    case 3:
                        #region 3 分
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
                        if (requestBody.DeviceId == "0")
                        {
                            if (requestBody.WorkshopName == "")
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_min` " +
                                    "WHERE Time >= @startTime AND Time <= @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT Time, SUM(`ProcessCount`) `ProcessCount`, SUM(`TotalProcessCount`) `TotalProcessCount`, SUM(`ProcessTime`) `ProcessTime`, " +
                                    "SUM(`TotalProcessTime`) `TotalProcessTime`, SUM(IF(`State` = 1, 1, 0)) `State`, `Rate`, `Use`, `Total` FROM ( SELECT * FROM `npc_monitoring_process_min` " +
                                    "WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time < @endTime ORDER BY Time ) a GROUP BY Time ORDER BY Time;";
                            }
                        }
                        else
                        {
                            if (requestBody.Compare == 0)
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_min` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                            else
                            {
                                sql =
                                    "SELECT * FROM `npc_monitoring_process_min` WHERE DeviceId IN @DeviceId AND Time >= @startTime AND Time <= @endTime ORDER BY Time;";
                            }
                        }
                        #endregion
                        break;
                    default: return Result.GenError<DataResult>(Error.ParamError);
                }

                if (requestBody.DeviceId == "0")
                {
                    var data = ServerConfig.ApiDb.Query<MonitoringProcess>(sql, new
                    {
                        DeviceId = siteIds,
                        startTime,
                        endTime
                    }, 60);
                    result.datas.AddRange(data);
                }
                else
                {
                    if (requestBody.Compare == 0)
                    {
                        var data = ServerConfig.ApiDb.Query<MonitoringProcess>(sql, new
                        {
                            DeviceId = requestBody.DeviceId,
                            startTime,
                            endTime
                        }, 60);
                        result.datas.AddRange(data);
                    }
                    else
                    {
                        var data = ServerConfig.ApiDb.Query<MonitoringProcess>(sql, new
                        {
                            DeviceId = requestBody.DeviceId.Split(","),
                            startTime,
                            endTime
                        }, 60);
                        result.datas.AddRange(data);
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
        /// 故障统计
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Fault
        [HttpPost("Fault")]
        public DataResult Fault([FromBody] StatisticRequest requestBody)
        {
            var result = new DataResult();
            try
            {
                string sql;
                //故障统计 0 无数据对比 1 车间对比 2 机台号对比 2 机台号对比 3 日数据 4 周数据 5 月数据
                int cnt;
                IEnumerable<MonitoringFault> data;
                List<MonitoringFault> r;
                DateTime startTime;
                DateTime endTime;
                MonitoringFault monitoringFault;
                switch (requestBody.Compare)
                {
                    case 0:
                        #region 无数据对比
                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            cnt = ServerConfig.ApiDb.Query<int>(
                                "SELECT COUNT(1) FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                                {
                                    SiteName = requestBody.WorkshopName
                                }).FirstOrDefault();
                            if (cnt <= 0)
                            {
                                return Result.GenError<DataResult>(Error.WorkshopNotExist);
                            }

                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2 AND Workshop = @Workshop;";

                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            result.datas.AddRange(data);
                        }
                        else
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2;";

                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            r = new List<MonitoringFault>();
                            monitoringFault = null;
                            foreach (var d in data)
                            {
                                if (monitoringFault == null)
                                {
                                    monitoringFault = new MonitoringFault { Date = d.Date, Workshop = string.Empty };
                                }

                                if (!monitoringFault.Date.InSameDay(d.Date))
                                {
                                    r.Add(monitoringFault);
                                    monitoringFault = null;
                                }
                                else
                                {
                                    monitoringFault.Add(d);
                                }
                            }
                            r.Add(monitoringFault);
                            result.datas.AddRange(r);
                        }
                        #endregion
                        break;
                    case 1:
                        #region 车间对比
                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        if (requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            return Result.GenError<DataResult>(Error.WorkshopNotExist);
                        }

                        var siteName = requestBody.WorkshopName.Split(",");
                        cnt = ServerConfig.ApiDb.Query<int>(
                            "SELECT Id FROM `site` WHERE MarkedDelete = 0  AND SiteName IN @SiteName GROUP BY SiteName;", new
                            {
                                SiteName = siteName
                            }).Count();
                        if (cnt != siteName.Length)
                        {
                            return Result.GenError<DataResult>(Error.WorkshopNotExist);
                        }

                        sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2 AND Workshop IN @Workshop;";

                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = siteName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);
                        result.datas.AddRange(data);
                        #endregion
                        break;
                    case 2:
                        #region 机台号对比
                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            cnt = ServerConfig.ApiDb.Query<int>(
                                "SELECT COUNT(1) FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                                {
                                    SiteName = requestBody.WorkshopName
                                }).FirstOrDefault();
                            if (cnt <= 0)
                            {
                                return Result.GenError<DataResult>(Error.WorkshopNotExist);
                            }
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2 AND Workshop = @Workshop;";
                        }
                        else
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2;";
                        }

                        if (requestBody.DeviceId.IsNullOrEmpty())
                        {
                            return Result.GenError<DataResult>(Error.DeviceNotExist);
                        }

                        var deviceIds = requestBody.DeviceId.Split(",");
                        cnt =
                            ServerConfig.ApiDb
                                .Query<int>(
                                    "SELECT COUNT(1) FROM `device_library` WHERE Id IN @id AND `MarkedDelete` = 0;",
                                    new { id = deviceIds }).FirstOrDefault();
                        if (cnt != deviceIds.Length)
                        {
                            return Result.GenError<DataResult>(Error.DeviceNotExist);
                        }

                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);

                        r = new List<MonitoringFault>();
                        var days = (endTime - startTime).TotalDays;
                        for (var i = 0; i < days; i++)
                        {
                            var time = startTime.AddDays(i);
                            foreach (var device in deviceIds)
                            {
                                var mf = new MonitoringFault
                                {
                                    Date = time,
                                    Code = device,
                                    Workshop = !requestBody.WorkshopName.IsNullOrEmpty() ? requestBody.WorkshopName : string.Empty,
                                    ReportFaultType = data.All(x => x.Date != time) ? 0 : data.First(x => x.Date == time).ReportSingleFaultType.Count(x => x.DeviceFaultTypes.Any(y => y.Code == device)),
                                    CodeReportFaultType = data.All(x => x.Date != time) ? 0 : data.First(x => x.Date == time).ReportSingleFaultType.Sum(x => x.DeviceFaultTypes.Where(y => y.Code == device).Sum(y => y.Count)),
                                    ReportCount = data.All(x => x.Date != time) ? 0 : data.First(x => x.Date == time).ReportCount,
                                    RepairFaultType = data.All(x => x.Date != time) ? 0 : data.First(x => x.Date == time).RepairSingleFaultType.Count(x => x.DeviceFaultTypes.Any(y => y.Code == device)),
                                    RepairCount = data.All(x => x.Date != time) ? 0 : data.First(x => x.Date == time).RepairSingleFaultType.Sum(x => x.DeviceFaultTypes.Where(y => y.Code == device).Sum(y => y.Count)),
                                };
                                mf.AllDevice = mf.ReportCount;
                                mf.FaultDevice = mf.CodeReportFaultType;
                                r.Add(mf);
                            }
                        }
                        result.datas.AddRange(r);
                        #endregion
                        break;
                    case 3:
                        #region 日数据对比
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            cnt = ServerConfig.ApiDb.Query<int>(
                                "SELECT COUNT(1) FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                                {
                                    SiteName = requestBody.WorkshopName
                                }).FirstOrDefault();
                            if (cnt <= 0)
                            {
                                return Result.GenError<DataResult>(Error.WorkshopNotExist);
                            }
                        }

                        startTime = requestBody.StartTime.DayBeginTime();
                        endTime = requestBody.StartTime.AddDays(1).DayBeginTime();
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2 AND Workshop = @Workshop;";
                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            result.datas.AddRange(data);
                        }
                        else
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2;";
                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }
                            result.datas.Add(monitoringFault);
                        }

                        startTime = requestBody.EndTime.DayBeginTime();
                        endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2 AND Workshop = @Workshop;";

                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            result.datas.AddRange(data);
                        }
                        else
                        {
                            sql = "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date < @Date2;";
                            data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                            {
                                Workshop = requestBody.WorkshopName,
                                Date1 = startTime,
                                Date2 = endTime
                            }, 60).OrderBy(x => x.Date);
                            monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }
                            result.datas.Add(monitoringFault);
                        }
                        #endregion
                        break;
                    case 4:
                        #region 周数据对比
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            cnt = ServerConfig.ApiDb.Query<int>(
                                "SELECT COUNT(1) FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                                {
                                    SiteName = requestBody.WorkshopName
                                }).FirstOrDefault();
                            if (cnt <= 0)
                            {
                                return Result.GenError<DataResult>(Error.WorkshopNotExist);
                            }
                        }

                        var weeks = DateTimeExtend.GetWeek(0, requestBody.StartTime);
                        startTime = weeks.Item1;
                        endTime = weeks.Item2;
                        sql = !requestBody.WorkshopName.IsNullOrEmpty()
                            ? "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2 AND Workshop = @Workshop;"
                            : "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2;";

                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);
                        monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                        foreach (var d in data)
                        {
                            monitoringFault.Add(d);
                        }
                        result.datas.Add(monitoringFault);

                        weeks = DateTimeExtend.GetWeek(0, requestBody.EndTime);
                        startTime = weeks.Item1;
                        endTime = weeks.Item2;
                        sql = !requestBody.WorkshopName.IsNullOrEmpty()
                            ? "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2 AND Workshop = @Workshop;"
                            : "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2;";

                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);
                        monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                        foreach (var d in data)
                        {
                            monitoringFault.Add(d);
                        }
                        result.datas.Add(monitoringFault);
                        #endregion
                        break;
                    case 5:
                        #region 月数据对比
                        if (!requestBody.WorkshopName.IsNullOrEmpty())
                        {
                            cnt = ServerConfig.ApiDb.Query<int>(
                                "SELECT COUNT(1) FROM `site` WHERE MarkedDelete = 0 AND SiteName = @SiteName;", new
                                {
                                    SiteName = requestBody.WorkshopName
                                }).FirstOrDefault();
                            if (cnt <= 0)
                            {
                                return Result.GenError<DataResult>(Error.WorkshopNotExist);
                            }
                        }

                        var months = DateTimeExtend.GetMonth(0, requestBody.StartTime);
                        startTime = months.Item1;
                        endTime = months.Item2;
                        sql = !requestBody.WorkshopName.IsNullOrEmpty()
                            ? "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2 AND Workshop = @Workshop;"
                            : "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2;";
                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);
                        monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                        foreach (var d in data)
                        {
                            monitoringFault.Add(d);
                        }
                        result.datas.Add(monitoringFault);

                        months = DateTimeExtend.GetMonth(0, requestBody.EndTime);
                        startTime = months.Item1;
                        endTime = months.Item2;
                        sql = !requestBody.WorkshopName.IsNullOrEmpty()
                            ? "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2 AND Workshop = @Workshop;"
                            : "SELECT * FROM `npc_monitoring_fault` WHERE Date >= @Date1 AND Date <= @Date2;";
                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime
                        }, 60).OrderBy(x => x.Date);
                        monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                        foreach (var d in data)
                        {
                            monitoringFault.Add(d);
                        }
                        result.datas.Add(monitoringFault);
                        #endregion
                        break;
                    default:
                        break;
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