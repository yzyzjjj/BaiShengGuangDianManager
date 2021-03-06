﻿using ApiManagement.Base.Server;
using ApiManagement.Models.AccountManagementModel;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.FlowCardManagementModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Device;
using ModelBase.Models.Result;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ApiManagement.Controllers.StatisticManagementController
{
    /// <summary>
    /// 数据统计
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class StatisticController : ControllerBase
    {
        public class StatisticRequest
        {
            //流程卡趋势图
            public int FlowCardId = 0;
            public int Order;
            public int WorkshopId;
            public string WorkshopName;
            //加工记录 0 = 所有    对比图 1,2,3
            public string DeviceId;
            public DateTime StartTime;
            public DateTime EndTime;
            public int ProductionId;
            public int StartHour;
            public int EndHour;
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
            public DeviceData DeviceData;
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
                    $"SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
                var cha = 30;
                var data = new List<MonitoringAnalysis>();
                var tStartTime = startTime;
                var tEndTime = tStartTime.AddMinutes(cha);
                var tasks = new List<Task<IEnumerable<MonitoringAnalysis>>>();
                while (true)
                {
                    if (tEndTime > endTime)
                    {
                        tEndTime = endTime;
                    }

                    var task = ServerConfig.DataReadDb.QueryAsync<MonitoringAnalysis>(sql, new
                    {
                        requestBody.DeviceId,
                        startTime = tStartTime,
                        endTime = tEndTime
                    }, 60);
                    tasks.Add(task);
                    if (tEndTime == endTime)
                    {
                        break;
                    }

                    tStartTime = tEndTime;
                    tEndTime = tStartTime.AddMinutes(cha);
                }
                //Task.WaitAll(tasks.ToArray());
                foreach (var task in tasks)
                {
                    data.AddRange(task.Result);
                }
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
                Log.Error(e);
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
            //                    $"SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
            //            }
            //            else
            //            {
            //                sql =
            //                    $"SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId IN @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
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
            //                    $"SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
            //            }
            //            else
            //            {
            //                sql =
            //                    $"SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId IN @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY `DeviceId`, DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";
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
            //                    $"SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

            //            }
            //            else
            //            {
            //                sql =
            //                    $"SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY `DeviceId`, DATE(SendTime) ORDER BY SendTime";
            //            }
            //            #endregion
            //            break;
            //        default: return Result.GenError<DataResult>(Error.ParamError);
            //    }

            //    IEnumerable<MonitoringAnalysis> data;
            //    if (requestBody.Compare == 0)
            //    {
            //        data = ServerConfig.DataReadDb.Query<MonitoringAnalysis>(sql, new
            //        {
            //            DeviceId = requestBody.DeviceId,
            //            startTime,
            //            endTime
            //        }, 60);
            //    }
            //    else
            //    {
            //        data = ServerConfig.DataReadDb.Query<MonitoringAnalysis>(sql, new
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
                            $"SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";

                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime.NoMinute();
                        endTime = requestBody.EndTime.NoMinute();

                        sql =
                            $"SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime.StartOfMonth();
                        endTime = requestBody.EndTime.StartOfNextMonth().DayBeginTime();

                        sql =
                            $"SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    default: return Result.GenError<Result>(Error.ParamError);
                }

                var data1 = ServerConfig.DataReadDb.Query<MonitoringAnalysis>(sql, new
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
                            $"SELECT Id, SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";

                        break;
                    #endregion
                    case 1:
                        #region 1 天 - 小时
                        //startTime = requestBody.StartTime.DayBeginTime();
                        //endTime = requestBody.EndTime.AddDays(1).DayBeginTime();
                        startTime = requestBody.StartTime1.NoMinute();
                        endTime = requestBody.EndTime1.NoMinute();

                        sql =
                            $"SELECT Id, DATE_FORMAT(SendTime, '%Y-%m-%d %H:00:00') SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime), HOUR (SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    case 2:
                        #region 2 月 天 
                        startTime = requestBody.StartTime1.StartOfMonth();
                        endTime = requestBody.EndTime1.StartOfNextMonth().DayBeginTime();

                        sql =
                            $"SELECT Id, DATE(SendTime) SendTime, `Data`, `DeviceId`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime GROUP BY DATE(SendTime) ORDER BY SendTime";

                        #endregion
                        break;
                    default: return Result.GenError<Result>(Error.ParamError);
                }

                var data2 = ServerConfig.DataReadDb.Query<MonitoringAnalysis>(sql, new
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
                Log.Error(e);
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

                var sql = $"SELECT Id, SendTime, `DATA`, ScriptId FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @startTime AND SendTime <= @endTime ORDER BY SendTime";
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
                        data.AddRange(ServerConfig.DataReadDb.Query<MonitoringAnalysis>(sql, new
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
                Log.Error(e);
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

                var workshops = new List<Workshop>();
                if (requestBody.WorkshopId != 0)
                {
                    workshops.Add(WorkshopHelper.Instance.Get<Workshop>(requestBody.WorkshopId));
                }
                else
                {
                    workshops.AddRange(WorkshopHelper.Instance.GetAll<Workshop>());
                }
                var wIds = workshops.Select(x => x.Id);
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
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
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
                        startTime = requestBody.StartTime;
                        endTime = requestBody.EndTime;
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
                    var data = MonitoringProcessHelper.GetMonitoringProcesses(wIds, null, startTime, endTime);
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
                Log.Error(e);
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        /// <summary>
        /// 设备加工详情
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/ProcessDetail
        [HttpPost("ProcessDetail")]
        public DataResult ProcessDetail([FromBody] StatisticRequest requestBody)
        {
            try
            {
                if (requestBody.StartTime == default(DateTime) || requestBody.EndTime == default(DateTime) || requestBody.DeviceId.IsNullOrEmpty())
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }
                var deviceIds = requestBody.DeviceId.Split(",").Select(int.Parse);
                if (!deviceIds.Any())
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var devices = DeviceLibraryHelper.Instance.GetByIds<DeviceLibrary>(deviceIds);
                if (devices.Count() != deviceIds.Count())
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var result = new DataResult();
                var sql = "";
                //var sql = "SELECT DeviceId, SUM(ProcessCount) ProcessCount, SUM(ProcessTime) ProcessTime FROM `npc_monitoring_process_day` " +
                //          "WHERE Time >= @Time AND Time <= @Time AND DeviceId IN @DeviceId GROUP BY DeviceId";
                //var monitoringProcess = ServerConfig.ApiDb.Query<MonitoringProcess>(sql, new
                //{
                //    Time = requestBody.StartTime,
                //    DeviceId = deviceIds,
                //}, 60).ToDictionary(x => x.DeviceId);

                var workshops = new List<Workshop>();
                if (requestBody.WorkshopId != 0)
                {
                    workshops.Add(WorkshopHelper.Instance.Get<Workshop>(requestBody.WorkshopId));
                }
                else
                {
                    workshops.AddRange(WorkshopHelper.Instance.GetAll<Workshop>());
                }

                foreach (var workshop in workshops)
                {
                    deviceIds = devices.Where(x => x.WorkshopId == workshop.Id).Select(x => x.Id);
                    if (!deviceIds.Any())
                    {
                        continue;
                    }

                    var workTime1 = DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, requestBody.StartTime.AddHours(12));
                    requestBody.StartTime = workTime1.Item1;
                    var workTime2 = DateTimeExtend.GetDayWorkDay(workshop.StatisticTimeList, requestBody.EndTime.AddHours(12));
                    requestBody.EndTime = workTime2.Item2;
                    var productions = ProductionHelper.Instance.GetAllData<Production>();
                    sql = "SELECT a.*, b.`Code`, c.Name ProcessorName, d.FlowCardName, d.ProductionProcessId FROM `npc_monitoring_process_log` a " +
                          "LEFT JOIN `device_library` b ON a.DeviceId = b.Id " +
                          "LEFT JOIN `accounts` c ON a.ProcessorId = c.Id " +
                          "LEFT JOIN `flowcard_library` d ON a.FlowCardId = d.Id " +
                          "WHERE b.WorkshopId = @WorkshopId AND a.DeviceId IN @DeviceId AND StartTime >= @StartTime1 AND StartTime <= @StartTime2 ORDER BY a.StartTime;";
                    var flowCardIs = new List<int>();
                    if (requestBody.ProductionId != 0)
                    {
                        var flowCards = FlowCardHelper.GetFlowCardsByProduction(requestBody.ProductionId);
                        flowCardIs.AddRange(flowCards.Select(x => x.Id));
                        if (!flowCardIs.Any())
                        {
                            return result;
                        }
                        sql = "SELECT a.*, b.`Code`, c.Name ProcessorName, d.FlowCardName, d.ProductionProcessId FROM `npc_monitoring_process_log` a " +
                            "LEFT JOIN `device_library` b ON a.DeviceId = b.Id " +
                            "LEFT JOIN `accounts` c ON a.ProcessorId = c.Id " +
                            "LEFT JOIN `flowcard_library` d ON a.FlowCardId = d.Id " +
                            "WHERE b.WorkshopId = @WorkshopId AND a.DeviceId IN @DeviceId AND StartTime >= @StartTime1 AND StartTime <= @StartTime2 AND a.FlowCardId IN @flowCardIs ORDER BY a.StartTime;";
                    }

                    var day = Math.Floor((workTime2.Item1 - workTime1.Item1).TotalDays + 1);
                    var data = ServerConfig.ApiDb.Query<MonitoringProcessLogDetail>(sql, new
                    {
                        WorkshopId = workshop.Id,
                        DeviceId = deviceIds,
                        StartTime1 = requestBody.StartTime,
                        StartTime2 = requestBody.EndTime,
                        ProductionId = requestBody.ProductionId,
                        flowCardIs
                    }, 60).OrderByDescending(x => x.StartTime);
                    //var Counts = ServerConfig.ApiDb.Query<dynamic>(
                    //    "SELECT b.DeviceId, COUNT(1) Count FROM (SELECT * FROM flowcard_report_get WHERE WorkshopId = @WorkshopId AND Time >= @StartTime1 AND Time <= @StartTime2) a JOIN (SELECT Id DeviceId, `Code` FROM device_library WHERE WorkshopId = @WorkshopId AND Id IN @DeviceId AND MarkedDelete = 0) b ON a.`Code` = b.`Code` GROUP BY b.DeviceId;",
                    //    new
                    //    {
                    //        WorkshopId = workshop.Id,
                    //        DeviceId = deviceIds,
                    //        StartTime1 = requestBody.StartTime,
                    //        StartTime2 = requestBody.EndTime,
                    //    }, 60).ToDictionary(x => x.DeviceId, x => x.Count);
                    foreach (var device in deviceIds)
                    {
                        //var idleTime = default(DateTime);
                        var logs = new List<object>();
                        var ds = data.Where(x => x.DeviceId == device);
                        var count = 0;
                        var time = 0;
                        foreach (var d in ds)
                        {
                            var product = productions.FirstOrDefault(x => x.Id == d.ProductionProcessId);
                            d.ProductionProcessName = product?.ProductionProcessName;
                            logs.Add(d);
                            if (d.ProcessType == ProcessType.Process)
                            {
                                count++;
                                time += d.TotalTime;
                            }
                        }

                        //var fcCount = Counts.ContainsKey(device) ? Counts[device] : 0;
                        result.datas.Add(new
                        {
                            DeviceId = device,
                            Count = count,
                            CountAvg = Math.Abs(day) > 0 ? (count / day).ToRound() : 0,
                            Time = time,
                            TimeAvg = count != 0 ? (int)(time / count) : 0,
                            //TimeAvg = fcCount != 0 ? (int)(Time / fcCount) : 0,
                            Logs = logs
                        });
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        /// <summary>
        /// 生产数据
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/ProductionData
        [HttpPost("ProductionData")]
        public DataResult ProductionData([FromBody] StatisticRequest requestBody)
        {
            var result = new DataResult();
            try
            {
                var workshops = new List<Workshop>();
                if (requestBody.WorkshopId != 0)
                {
                    workshops.Add(WorkshopHelper.Instance.Get<Workshop>(requestBody.WorkshopId));
                }
                else
                {
                    workshops.AddRange(WorkshopHelper.Instance.GetAll<Workshop>());
                }
                if (requestBody.DeviceId.IsNullOrEmpty())
                {
                    return Result.GenError<DataResult>(Error.ParamError);
                }

                var device =
                        ServerConfig.ApiDb
                            .Query<DeviceLibraryDetail>(
                                "SELECT a.Id, b.CategoryName FROM `device_library` a " +
                                "JOIN (SELECT a.Id, b.CategoryName FROM `device_model` a JOIN `device_category` b ON a.DeviceCategoryId = b.Id) b ON a.DeviceModelId = b.Id " +
                                "WHERE a.Id = @DeviceId AND `MarkedDelete` = 0;",
                                new { requestBody.DeviceId }).FirstOrDefault();
                if (device == null)
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var paramDic = new Dictionary<string, string[]>
                {
                    {"粗抛机", new []{ "CuPaoTime", "CuPaoFaChu", "CuPaoHeGe", "CuPaoLiePian", "CuPaoDeviceId"}},
                    {"精抛机", new []{ "JingPaoTime", "JingPaoFaChu", "JingPaoHeGe", "JingPaoLiePian", "JingPaoDeviceId"}},
                    {"研磨机", new []{ "YanMoTime", "YanMoFaChu", "YanMoHeGe", "YanMoLiePian", "YanMoDeviceId"}},
                };

                if (!paramDic.Any(x => device.CategoryName.Contains(x.Key)))
                {
                    return Result.GenError<DataResult>(Error.DeviceNotExist);
                }

                var startTime = requestBody.StartTime;
                var endTime = requestBody.EndTime;
                var totalDays = (endTime - startTime).TotalDays;
                var data = new Dictionary<DateTime, MonitoringProductionData>();
                for (var i = 0; i < totalDays; i++)
                {
                    var t = startTime.AddDays(i);
                    data.Add(t, new MonitoringProductionData { Time = t });
                }

                var category = paramDic.FirstOrDefault(x => device.CategoryName.Contains(x.Key)).Value;
                var sql =
                     "SELECT DATE({0}) Time, SUM({1}) FaChu, SUM({2}) HeGe, SUM({3}) LiePian, IF(SUM({1}) = 0, 0, round(SUM({2})/SUM({1}), 2)) Rate " +
                     "FROM `flowcard_library` WHERE {4} = @DeviceId AND {0} >= @startTime AND {0} <= @endTime GROUP BY DATE({0}) ORDER BY DATE({0})";
                var monitoringProductionData = ServerConfig.ApiDb.Query<MonitoringProductionData>(string.Format(sql, category[0], category[1], category[2], category[3], category[4]), new
                {
                    requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);
                if (monitoringProductionData.Any())
                {
                    foreach (var mpData in monitoringProductionData)
                    {
                        if (data.ContainsKey(mpData.Time))
                        {
                            data[mpData.Time] = mpData;
                        }
                    }
                }
                sql =
                  "SELECT * FROM `npc_monitoring_process_day` WHERE DeviceId = @DeviceId AND Time >= @startTime AND Time <= @endTime;";

                var monitoringProcesses = ServerConfig.ApiDb.Query<MonitoringProcess>(sql, new
                {
                    requestBody.DeviceId,
                    startTime,
                    endTime
                }, 60);

                if (monitoringProcesses.Any())
                {
                    foreach (var monitoringProcess in monitoringProcesses)
                    {
                        if (data.ContainsKey(monitoringProcess.Time))
                        {
                            data[monitoringProcess.Time].ProcessTime = monitoringProcess.ProcessTime;
                        }
                    }
                }

                result.datas.AddRange(data.Values.OrderBy(x => x.Time));
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e);
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
                //故障统计 0 无数据对比 1 车间对比 2 机台号对比 2 机台号对比 3 日数据 4 周数据 5 月数据 6 时间段数据
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
                            if (data.Any())
                            {
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
                                    "SELECT COUNT(1) FROM `device_library` WHERE Code IN @id AND `MarkedDelete` = 0;",
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
                                    ReportFaultType = data.All(x => x.Date != time) ? 0 : data.Where(x => x.Date == time).Sum(z => z.ReportSingleFaultType.Count(x => x.DeviceFaultTypes.Any(y => y.Code == device))),
                                    CodeReportFaultType = data.All(x => x.Date != time) ? 0 : data.Where(x => x.Date == time).Sum(z => z.ReportSingleFaultType.Count(x => x.DeviceFaultTypes.Any(y => y.Code == device))),
                                    ReportCount = data.All(x => x.Date != time) ? 0 : data.Where(x => x.Date == time).Sum(z => z.ReportSingleFaultType.Sum(x => x.DeviceFaultTypes.Where(y => y.Code == device).Sum(y => y.Count))),
                                    RepairFaultType = data.All(x => x.Date != time) ? 0 : data.Where(x => x.Date == time).Sum(z => z.RepairSingleFaultType.Count(x => x.DeviceFaultTypes.Any(y => y.Code == device))),
                                    RepairCount = data.All(x => x.Date != time) ? 0 : data.Where(x => x.Date == time).Sum(z => z.RepairSingleFaultType.Sum(x => x.DeviceFaultTypes.Where(y => y.Code == device).Sum(y => y.Count))),
                                };
                                if (data.Any(x => x.Date == time))
                                {
                                    foreach (var da in data.Where(x => x.Date == time))
                                    {
                                        foreach (var d in da.ReportSingleFaultType)
                                        {
                                            if (d.DeviceFaultTypes.Any(y => y.Code == device))
                                            {
                                                if (mf.ReportSingleFaultType.All(x => x.FaultId != d.FaultId))
                                                {
                                                    var add = new SingleFaultType
                                                    {
                                                        FaultId = d.FaultId,
                                                        FaultName = d.FaultName,
                                                    };
                                                    add.DeviceFaultTypes.Add(new DeviceFaultType
                                                    {
                                                        Code = device,
                                                    });
                                                    mf._reportSingleFaultType.Add(add);
                                                }
                                                mf.ReportSingleFaultType.First(x => x.FaultId == d.FaultId).DeviceFaultTypes.First(x => x.Code == device).Count +=
                                                    d.DeviceFaultTypes.First(x => x.Code == device).Count;
                                                mf.ReportSingleFaultType.First(x => x.FaultId == d.FaultId).Count = mf.ReportSingleFaultType.First(x => x.FaultId == d.FaultId)
                                                        .DeviceFaultTypes.Sum(x => x.Count);
                                            }
                                        }

                                        foreach (var d in da.RepairSingleFaultType)
                                        {
                                            if (d.DeviceFaultTypes.Any(y => y.Code == device))
                                            {
                                                if (mf.RepairSingleFaultType.All(x => x.FaultId != d.FaultId))
                                                {
                                                    var add = new SingleFaultType
                                                    {
                                                        FaultId = d.FaultId,
                                                        FaultName = d.FaultName,
                                                    };
                                                    add.DeviceFaultTypes.Add(new DeviceFaultType
                                                    {
                                                        Code = device,
                                                    });
                                                    mf._repairSingleFaultType.Add(add);
                                                }
                                                mf.RepairSingleFaultType.First(x => x.FaultId == d.FaultId).DeviceFaultTypes.First(x => x.Code == device).Count +=
                                                    d.DeviceFaultTypes.First(x => x.Code == device).Count;
                                                mf.RepairSingleFaultType.First(x => x.FaultId == d.FaultId).Count = mf.RepairSingleFaultType.First(x => x.FaultId == d.FaultId)
                                                    .DeviceFaultTypes.Sum(x => x.Count);
                                            }
                                        }


                                    }
                                }

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
                            if (data.Any())
                            {
                                monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                                foreach (var d in data)
                                {
                                    monitoringFault.Add(d);
                                }
                                result.datas.Add(monitoringFault);
                            }
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
                            if (data.Any())
                            {
                                monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                                foreach (var d in data)
                                {
                                    monitoringFault.Add(d);
                                }
                                result.datas.Add(monitoringFault);
                            }
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

                        if (data.Any())
                        {
                            monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }
                            result.datas.Add(monitoringFault);
                        }

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

                        if (data.Any())
                        {
                            monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }
                            result.datas.Add(monitoringFault);
                        }
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

                        if (data.Any())
                        {
                            monitoringFault = new MonitoringFault
                            { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }

                            result.datas.Add(monitoringFault);
                        }

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

                        if (data.Any())
                        {
                            monitoringFault = new MonitoringFault { Date = startTime, Workshop = requestBody.WorkshopName };
                            foreach (var d in data)
                            {
                                monitoringFault.Add(d);
                            }
                            result.datas.Add(monitoringFault);
                        }
                        #endregion
                        break;
                    case 6:
                        #region 时间段数据
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
                        endTime = requestBody.EndTime.DayEndTime();

                        sql = !requestBody.WorkshopName.IsNullOrEmpty()
                            ? "SELECT * FROM `npc_monitoring_fault_hour` WHERE Date >= @Date1 AND Date < @Date2 AND HOUR(Date) >= @Hour1 AND HOUR(Date) <= @Hour2 AND Workshop = @Workshop;"
                            : "SELECT * FROM `npc_monitoring_fault_hour` WHERE Date >= @Date1 AND Date < @Date2 AND HOUR(Date) >= @Hour1 AND HOUR(Date) <= @Hour2;";
                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime,
                            Hour1 = requestBody.StartHour,
                            Hour2 = requestBody.EndHour,
                        }, 60);
                        if (data.Any())
                        {
                            r = new List<MonitoringFault>();
                            monitoringFault = new MonitoringFault { Date = startTime.AddHours(requestBody.StartHour), Workshop = requestBody.WorkshopName };
                            foreach (var d in data.OrderBy(x => x.Date.Hour))
                            {
                                if (monitoringFault.Date.Hour == d.Date.Hour)
                                {
                                    monitoringFault.Add(d);
                                }
                                else
                                {
                                    r.Add(monitoringFault);
                                    monitoringFault = new MonitoringFault { Date = d.Date, Workshop = requestBody.WorkshopName };
                                    monitoringFault.Add(d);
                                }
                            }
                            r.Add(monitoringFault);
                            result.datas.AddRange(r);
                        }

                        data = ServerConfig.ApiDb.Query<MonitoringFault>(sql, new
                        {
                            Workshop = requestBody.WorkshopName,
                            Date1 = startTime,
                            Date2 = endTime,
                            Hour1 = requestBody.StartHour,
                            Hour2 = requestBody.EndHour,
                        }, 60);
                        if (data.Any())
                        {
                            var rr = new List<MonitoringFault>();
                            var monitoringFaultDay = new MonitoringFault { Date = startTime.AddHours(requestBody.StartHour), Workshop = requestBody.WorkshopName };
                            foreach (var d in data.OrderBy(x => x.Date))
                            {
                                if (monitoringFaultDay.Date.InSameDay(d.Date))
                                {
                                    monitoringFaultDay.DayAdd(d);
                                }
                                else
                                {
                                    rr.Add(monitoringFaultDay);
                                    monitoringFaultDay = new MonitoringFault { Date = d.Date, Workshop = requestBody.WorkshopName };
                                    monitoringFaultDay.DayAdd(d);
                                }
                            }
                            rr.Add(monitoringFaultDay);

                            var all = new MonitoringFault { Date = startTime.AddHours(requestBody.EndHour), Workshop = requestBody.WorkshopName }; ;
                            foreach (var d in rr)
                            {
                                all.Add(d);
                            }
                            result.datas.Add(all);
                        }
                        #endregion
                        break;
                    default:
                        break;
                }
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return Result.GenError<DataResult>(Error.ParamError);
            }
        }

        /// <summary>
        /// 历史监控数据
        /// </summary>
        /// <returns></returns>
        // POST: api/Statistic/Monitor
        [HttpPost("Monitor")]
        public object Monitor([FromBody] StatisticRequest requestBody)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (requestBody.DeviceId.IsNullOrEmpty() || requestBody.DeviceData == null || (requestBody.DeviceData.vals.Count + requestBody.DeviceData.ins.Count + requestBody.DeviceData.outs.Count) <= 0)
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            var device =
                ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, ScriptId FROM `device_library` WHERE Id = @DeviceId AND `MarkedDelete` = 0;", new { requestBody.DeviceId }).FirstOrDefault();
            if (device == null)
            {
                return Result.GenError<DataResult>(Error.DeviceNotExist);
            }

            var scriptVersion =
                ServerConfig.ApiDb.Query<ScriptVersion>("SELECT * FROM `script_version` WHERE Id = @id AND `MarkedDelete` = 0;", new { id = device.ScriptId }).FirstOrDefault();
            if (scriptVersion == null)
            {
                return Result.GenError<DataResult>(Error.ScriptVersionNotExist);
            }

            if (requestBody.DeviceData.vals.Any(x => scriptVersion.MaxValuePointerAddress < x || x <= 0))
            {
                return Result.GenError<DataResult>(Error.ValuePointerAddressOutLimit);
            }

            if (requestBody.DeviceData.ins.Any(x => scriptVersion.MaxInputPointerAddress < x || x <= 0))
            {
                return Result.GenError<DataResult>(Error.InputPointerAddressOutLimit);
            }

            if (requestBody.DeviceData.outs.Any(x => scriptVersion.MaxOutputPointerAddress < x || x <= 0))
            {
                return Result.GenError<DataResult>(Error.OutputPointerAddressOutLimit);
            }
            try
            {
                var sql = "SELECT * FROM `data_name_dictionary` WHERE ScriptId = @ScriptId";
                var p = new List<string>();
                if (requestBody.DeviceData.vals.Any())
                {
                    p.Add("(VariableTypeId = 1 AND PointerAddress IN @PointerAddress1)");
                }
                if (requestBody.DeviceData.ins.Any())
                {
                    p.Add("(VariableTypeId = 2 AND PointerAddress IN @PointerAddress2)");
                }
                if (requestBody.DeviceData.outs.Any())
                {
                    p.Add("(VariableTypeId = 3 AND PointerAddress IN @PointerAddress3)");
                }
                if (p.Any())
                {
                    sql += " AND (" + p.Join(" OR ") + ")";
                }

                var dataDic = ServerConfig.ApiDb.Query<DataNameDictionary>(sql, new
                {
                    ScriptId = device.ScriptId,
                    PointerAddress1 = requestBody.DeviceData.vals,
                    PointerAddress2 = requestBody.DeviceData.ins,
                    PointerAddress3 = requestBody.DeviceData.outs,
                });

                var data = new Dictionary<DateTime, DeviceTrueData>();
                sql =
                    $"SELECT * FROM `{ServerConfig.DataReadDb.Table}` WHERE DeviceId = @DeviceId AND SendTime >= @StartTime AND SendTime < @EndTime AND UserSend = 0;";
                var cha = 240;
                var tStartTime = requestBody.StartTime;
                var tEndTime = tStartTime.AddMinutes(cha);
                var tasks = new List<Task<IEnumerable<MonitoringAnalysis>>>();
                while (true)
                {
                    if (tEndTime > requestBody.EndTime)
                    {
                        tEndTime = requestBody.EndTime.AddSeconds(1);
                    }

                    var task = ServerConfig.DataReadDb.QueryAsync<MonitoringAnalysis>(sql, new
                    {
                        requestBody.DeviceId,
                        startTime = tStartTime,
                        endTime = tEndTime
                    }, 60);
                    tasks.Add(task);
                    if (tEndTime == requestBody.EndTime.AddSeconds(1))
                    {
                        break;
                    }

                    tStartTime = tEndTime;
                    tEndTime = tStartTime.AddMinutes(cha);
                }

                //Task.WaitAll(tasks.ToArray());
                foreach (var task in tasks)
                {
                    foreach (var d in task.Result)
                    {
                        var t = d.SendTime.NoMillisecond();
                        if (!data.ContainsKey(t))
                        {
                            data.Add(t, new DeviceTrueData());
                        }

                        foreach (var pointerAddress in requestBody.DeviceData.vals)
                        {
                            var usuallyDictionary = dataDic.FirstOrDefault(x => x.VariableTypeId == 1 && x.PointerAddress == pointerAddress);
                            var chu = Math.Pow(10, usuallyDictionary?.Precision ?? 0);
                            data[t].vals.Add((decimal)(d.AnalysisData.vals[pointerAddress - 1] / chu));
                        }
                        foreach (var pointerAddress in requestBody.DeviceData.ins)
                        {
                            var usuallyDictionary = dataDic.FirstOrDefault(x => x.VariableTypeId == 2 && x.PointerAddress == pointerAddress);
                            var chu = Math.Pow(10, usuallyDictionary?.Precision ?? 0);
                            data[t].ins.Add((decimal)(d.AnalysisData.ins[pointerAddress - 1] / chu));
                        }
                        foreach (var pointerAddress in requestBody.DeviceData.outs)
                        {
                            var usuallyDictionary = dataDic.FirstOrDefault(x => x.VariableTypeId == 3 && x.PointerAddress == pointerAddress);
                            var chu = Math.Pow(10, usuallyDictionary?.Precision ?? 0);
                            data[t].outs.Add((decimal)(d.AnalysisData.outs[pointerAddress - 1] / chu));
                        }
                    }
                }
                data = data.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                sw.Stop();
                Log.Debug($"Monitor: {cha}  {sw.ElapsedMilliseconds}");
                Console.WriteLine($"Monitor: {cha}  {sw.ElapsedMilliseconds}");
                return new
                {
                    errno = 0,
                    errmsg = "成功",
                    data
                };
            }
            catch (Exception e)
            {
                Log.Error(e);
                return Result.GenError<Result>(Error.TimeOut);
            }
        }

    }
}