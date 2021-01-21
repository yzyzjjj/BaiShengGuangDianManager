using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.DeviceManagementModel;
using ApiManagement.Models.Warning;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.WarningController
{
    /// <summary>
    /// 预警管理
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class WarningController : ControllerBase
    {
        /// <summary>
        /// 获取预警设置
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning
        [HttpGet]
        public DataResult GetSet([FromQuery]int qId, WarningType type, WarningDataType dataType, bool first = false)
        {
            var result = new DataResult();
            var r = ServerConfig.ApiDb.Query<WarningSet>(
                $"SELECT a.*, b.`Class` FROM `warning_set` a " +
                $"JOIN `device_class` b ON a.ClassId = b.Id " +
                $"WHERE a.`MarkedDelete` = 0 AND a.`Type` = @type AND a.`DataType` = @dataType{(qId == 0 ? "" : " AND a.Id = @qId")};", new { qId, type, dataType });

            var d = new List<WarningSetItemDetail>();
            if (first && r.Any())
            {
                var setId = r.FirstOrDefault().Id;
                var sql = "";
                switch (dataType)
                {
                    case WarningDataType.设备数据:
                        var scriptId = r.FirstNonDefault().ScriptId;
                        sql = "SELECT a.VariableName, a.PointerAddress, a.VariableTypeId, b.*, a.Id DictionaryId " +
                              "FROM (SELECT * FROM `data_name_dictionary` WHERE ScriptId = @scriptId AND MarkedDelete = 0) a " +
                              "LEFT JOIN (SELECT * FROM `warning_set_item` WHERE SetId = @setId AND MarkedDelete = 0) b " +
                              "ON a.Id = b.DictionaryId ORDER BY a.VariableTypeId, a.PointerAddress;";
                        d.AddRange(ServerConfig.ApiDb.Query<WarningSetItemDetail>(sql, new { setId, scriptId }));
                        break;
                    case WarningDataType.生产数据:
                        sql = "SELECT * FROM `warning_set_item` WHERE MarkedDelete = 0 AND SetId = @setId;";
                        var s = ServerConfig.ApiDb.Query<WarningSetItemDetail>(sql, new { setId }).ToList();
                        foreach (var val in WarningHelper.生产数据字段)
                        {
                            if (s.All(x => x.Item != val))
                            {
                                d.Add(new WarningSetItemDetail
                                {
                                    Item = val
                                });
                            }
                            else
                            {
                                d.Add(s.First(x => x.Item == val));
                            }
                        }
                        foreach (var val in WarningHelper.生产数据汇总字段)
                        {
                            if (s.All(x => x.Item != val))
                            {
                                d.Add(new WarningSetItemDetail
                                {
                                    Item = val,
                                    IsSum = true
                                });
                            }
                            else
                            {
                                d.Add(s.First(x => x.Item == val));
                            }
                        }
                        break;
                    case WarningDataType.故障数据:
                        //sql = "SELECT * FROM `warning_set_item` WHERE MarkedDelete = 0 AND SetId = @setId;";
                        break;
                }

                if (!sql.IsNullOrEmpty())
                {
                    r.FirstOrDefault().Items.AddRange(d);
                }
            }

            if (r.Any())
            {
                var device =
                    ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, `Code` FROM `device_library` WHERE `MarkedDelete` = 0 AND `Id` IN @Id;"
                        , new { Id = r.SelectMany(x => x.DeviceList).Distinct().ToList() });

                var categories =
                    ServerConfig.ApiDb.Query<DeviceCategory>("SELECT `Id`, `CategoryName` FROM `device_category` WHERE `MarkedDelete` = 0 AND `Id` IN @Id;"
                        , new { Id = r.Select(x => x.CategoryId).Distinct().ToList() });

                var script =
                    ServerConfig.ApiDb.Query<ScriptVersion>("SELECT `Id`, `ScriptName` FROM `script_version` WHERE `MarkedDelete` = 0 AND `Id` IN @Id;"
                        , new { Id = r.Select(x => x.ScriptId).Distinct().ToList() });
                foreach (var dd in r)
                {
                    dd.Code = device.Where(x => dd.DeviceList.Contains(x.Id)).Select(y => y.Code).Join();
                    dd.Script = script.FirstOrDefault(x => x.Id == dd.ScriptId)?.ScriptName ?? "";
                    dd.CategoryName = categories.FirstOrDefault(x => x.Id == dd.CategoryId)?.CategoryName ?? "";
                }
            }
            if (qId != 0 && !r.Any())
            {
                return Result.GenError<DataResult>(Error.WarningSetNotExist);
            }
            result.datas.AddRange(r);
            return result;
        }

        /// <summary>
        /// 获取预警管理设置项
        /// </summary>
        /// <param name="sId">预警id</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="isNew">是否是新预警</param>
        /// <returns></returns>
        // POST: api/Warning/Item
        [HttpGet("Item")]
        public DataResult GetSetItem([FromQuery]int sId, WarningDataType dataType, bool isNew = false)
        {
            var result = new DataResult();
            WarningSet set = null;
            if (!isNew)
            {
                set = ServerConfig.ApiDb
                  .Query<WarningSet>("SELECT * FROM `warning_set` WHERE `MarkedDelete` = 0 AND Id = @sId;", new { sId })
                  .FirstOrDefault();
                if (set == null)
                {
                    return Result.GenError<DataResult>(Error.WarningSetNotExist);
                }
            }

            var scriptId = -1;
            var sql = "";
            switch (dataType)
            {
                case WarningDataType.设备数据:
                    sql = "SELECT ScriptId, VariableName, PointerAddress, VariableTypeId, Id DictionaryId FROM `data_name_dictionary` WHERE ScriptId = @sId AND MarkedDelete = 0 ORDER BY VariableTypeId, PointerAddress;";
                    if (!isNew)
                    {
                        scriptId = set.ScriptId;
                        sql = "SELECT a.VariableName, a.PointerAddress, a.VariableTypeId, b.*, a.Id DictionaryId " +
                              "FROM (SELECT * FROM `data_name_dictionary` WHERE ScriptId = @scriptId AND MarkedDelete = 0) a " +
                              "LEFT JOIN (SELECT * FROM `warning_set_item` WHERE SetId = @setId AND MarkedDelete = 0) b " +
                              "ON a.Id = b.DictionaryId ORDER BY a.VariableTypeId, a.PointerAddress;";
                    }

                    break;
                case WarningDataType.生产数据:
                    if (isNew)
                    {
                        result.datas.AddRange(WarningHelper.生产数据字段.Select(x => new WarningSetItem
                        {
                            Item = x
                        }));
                        result.datas.AddRange(WarningHelper.生产数据汇总字段.Select(x => new WarningSetItem
                        {
                            Item = x,
                            IsSum = true
                        }));
                    }
                    else
                    {
                        sql = "SELECT * FROM `warning_set_item` WHERE MarkedDelete = 0 AND SetId = @sId;";
                        var d = ServerConfig.ApiDb.Query<WarningSetItem>(sql, new { sId }).ToList();

                        foreach (var val in WarningHelper.生产数据字段)
                        {
                            if (d.All(x => x.Item != val))
                            {
                                result.datas.Add(new WarningSetItem
                                {
                                    Item = val
                                });
                            }
                            else
                            {
                                result.datas.Add(d.First(x => x.Item == val));
                            }
                        }
                        foreach (var val in WarningHelper.生产数据汇总字段)
                        {
                            if (d.All(x => x.Item != val))
                            {
                                d.Add(new WarningSetItem
                                {
                                    Item = val,
                                    IsSum = true
                                });
                            }
                            else
                            {
                                result.datas.Add(d.First(x => x.Item == val));
                            }
                        }
                    }
                    return result;
                case WarningDataType.故障数据:
                    //sql = "SELECT * FROM `warning_set_item` WHERE MarkedDelete = 0 AND SetId = @setId;";
                    break;
            }
            result.datas.AddRange(ServerConfig.ApiDb.Query<WarningSetItemDetail>(sql, new { sId, scriptId }));
            return result;
        }


        // PUT: api/Warning
        [HttpPut]
        public Result PutSet([FromBody] WarningSetNoItems set)
        {
            var oldSet = ServerConfig.ApiDb.Query<WarningSet>(
                "SELECT * FROM `warning_set` WHERE `MarkedDelete` = 0 AND Id = @Id;", new { set.Id }).FirstOrDefault();
            if (oldSet == null)
            {
                return Result.GenError<Result>(Error.WarningSetNotExist);
            }

            if (set.Name.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.WarningSetNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `warning_set` WHERE `Name` = @Name AND `Type` = @Type AND `DataType` = @DataType AND Id != @Id AND `MarkedDelete` = 0;",
                    new { set.Name, set.Type, set.DataType, set.Id }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.WarningSetIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            if (set.Items != null)
            {
                switch (set.DataType)
                {
                    case WarningDataType.设备数据:
                        set.Items = set.Items.Where(x => x.DictionaryId != 0).ToList();
                        foreach (var item in set.Items)
                        {
                            item.Item = "";
                        }
                        break;
                    case WarningDataType.生产数据:
                        set.Items = set.Items.Where(x => !x.Item.IsNullOrEmpty()).ToList(); break;
                    case WarningDataType.故障数据:
                        set.Items = set.Items; break;
                    default:
                        set.Items.Clear(); break;
                }

                if (set.Items.Any())
                {
                    //if (set.Items.Any(x => !x.ValidDataType()))
                    //{
                    //    return Result.GenError<Result>(Error.WarningSetItemDataTypeError);
                    //}
                    if (set.Items.Any(x => !x.ValidFrequency()))
                    {
                        return Result.GenError<Result>(Error.WarningSetItemFrequencyError);
                    }
                    if (set.Items.Any(x => !x.ValidCondition()))
                    {
                        return Result.GenError<Result>(Error.WarningSetItemConditionError);
                    }

                    var oldWarningSetItems = ServerConfig.ApiDb.Query<WarningSetItem>("SELECT * FROM `warning_set_item` WHERE `MarkedDelete` = 0 AND SetId = @Id;",
                        new { set.Id });

                    var delItems = oldWarningSetItems.Where(x => set.Items.All(y => y.Id != x.Id));
                    if (delItems.Any())
                    {
                        foreach (var item in delItems)
                        {
                            item.MarkedDateTime = markedDateTime;
                            item.MarkedDelete = true;
                        }

                        ServerConfig.ApiDb.Execute(
                            "UPDATE `warning_set_item` SET `MarkedDateTime` = @MarkedDateTime, `MarkedDelete` = @MarkedDelete WHERE `Id` = @Id;", delItems);
                    }

                    var updateItems = set.Items.Where(x => x.Id != 0);
                    if (updateItems.Any())
                    {
                        var ids = new List<int>();
                        foreach (var item in updateItems)
                        {
                            item.SetId = set.Id;
                            item.DataType = set.DataType;
                            var oldItem = oldWarningSetItems.FirstOrDefault(x => x.Id == item.Id);
                            if (oldItem != null && oldItem.HaveChange(item))
                            {
                                item.MarkedDateTime = markedDateTime;
                                ids.Add(item.Id);
                            }
                        }

                        ServerConfig.ApiDb.Execute(
                            "UPDATE `warning_set_item` SET `MarkedDateTime` = @MarkedDateTime, `SetId` = @SetId, `Item` = @Item, `Condition1` = @Condition1, `Value1` = @Value1, `Logic` = @Logic, " +
                            "`Condition2` = @Condition2, `Value2` = @Value2, `Frequency` = @Frequency, `Interval` = @Interval, `Count` = @Count, `DictionaryId` = @DictionaryId WHERE `Id` = @Id;",
                            updateItems.Where(x => ids.Contains(x.Id)));
                    }
                    var newItems = set.Items.Where(x => x.Id == 0);
                    if (newItems.Any())
                    {
                        foreach (var item in newItems)
                        {
                            item.CreateUserId = createUserId;
                            item.SetId = set.Id;
                            item.DataType = set.DataType;
                            item.Item = item.DataType == WarningDataType.设备数据 ? "" : item.Item;
                        }

                        ServerConfig.ApiDb.Execute(
                            "INSERT INTO `warning_set_item` (`CreateUserId`, `DataType`, `SetId`, `Item`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `Frequency`, `Interval`, `Count`, `DictionaryId`, `IsSum`) " +
                            "VALUES (@CreateUserId, @DataType, @SetId, @Item, @Condition1, @Value1, @Logic, @Condition2, @Value2, @Frequency, @Interval, @Count, @DictionaryId, @IsSum);",
                            newItems);
                    }
                }
                else
                {
                    ServerConfig.ApiDb.Execute(
                        "UPDATE `warning_set_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `SetId`= @Id;", new
                        {
                            MarkedDateTime = markedDateTime,
                            MarkedDelete = true,
                            Id = set.Id
                        });
                }
            }

            if (oldSet.HaveChange(set))
            {
                set.MarkedDateTime = DateTime.Now;
                ServerConfig.ApiDb.Execute(
                    "UPDATE `warning_set` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Enable` = @Enable, `ClassId` = @ClassId, `ScriptId` = @ScriptId, `DeviceIds` = @DeviceIds, `IsSum` = @IsSum WHERE `Id` = @Id;", set);
            }

            WarningHelper.LoadConfig();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Warning
        [HttpPost]
        public Result PostSet([FromBody] WarningSet set)
        {
            if (set.Name.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.WarningSetNotEmpty);
            }

            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `warning_set` WHERE `Name` = @Name AND `MarkedDelete` = 0;", new { set.Name }).FirstOrDefault();
            if (cnt > 0)
            {
                return Result.GenError<Result>(Error.WarningSetIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            if (set.Items != null && set.Items.Any())
            {
                //if (set.Items.Any(x => !x.ValidDataType()))
                //{
                //    return Result.GenError<Result>(Error.WarningSetItemDataTypeError);
                //}
                if (set.Items.Any(x => !x.ValidFrequency()))
                {
                    return Result.GenError<Result>(Error.WarningSetItemFrequencyError);
                }
                if (set.Items.Any(x => !x.ValidCondition()))
                {
                    return Result.GenError<Result>(Error.WarningSetItemConditionError);
                }
            }

            set.CreateUserId = createUserId;
            var id = ServerConfig.ApiDb.Query<int>(
                 "INSERT INTO `warning_set` (`CreateUserId`, `Type`, `DataType`, `Name`, `Enable`, `ClassId`, `ScriptId`, `CategoryId`, `DeviceIds`, `IsSum`) " +
                 "VALUES (@CreateUserId, @Type, @DataType, @Name, @Enable, @ClassId, @ScriptId, @CategoryId, @DeviceIds, @IsSum);SELECT LAST_INSERT_ID();",
                 set).FirstOrDefault();

            if (set.Items != null && set.Items.Any())
            {
                foreach (var item in set.Items)
                {
                    item.CreateUserId = createUserId;
                    item.SetId = id;
                    item.DataType = set.DataType;
                    item.Item = item.DataType == WarningDataType.设备数据 ? "" : item.Item;
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO `warning_set_item` (`CreateUserId`, `DataType`, `SetId`, `Item`, `Condition1`, `Value1`, `Logic`, `Condition2`, `Value2`, `Frequency`, `Interval`, `Count`, `DictionaryId`, `IsSum`) " +
                    "VALUES (@CreateUserId, @DataType, @SetId, @Item, @Condition1, @Value1, @Logic, @Condition2, @Value2, @Frequency, @Interval, @Count, @DictionaryId, @IsSum);",
                    set.Items);
            }
            WarningHelper.LoadConfig();
            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/Warning/5
        [HttpDelete("{id}")]
        public Result DeleteSet([FromRoute] int id)
        {
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `warning_set` WHERE Id = @id AND `MarkedDelete` = 0;", new { id }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.WarningSetNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `warning_set` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });

            ServerConfig.ApiDb.Execute(
                "UPDATE `warning_set_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `SetId`= @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = id
                });
            WarningHelper.LoadConfig();
            return Result.GenError<Result>(Error.Success);
        }


        /// <summary>
        /// 获取当前预警
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning/Current
        [HttpGet("Current")]
        public DataResult GetCurrentWarning([FromQuery]int sId, WarningType type, WarningDataType dataType)
        {
            var result = new DataResult();
            if (WarningHelper.CurrentData != null && WarningHelper.CurrentData.Any())
            {
                var data = sId == 0 ? WarningHelper.CurrentData.Where(x => x.Key.Item3 == dataType && x.Key.Item4 == type).Select(y => y.Value)
                    : WarningHelper.CurrentData.Where(x => x.Key.Item3 == dataType && x.Key.Item4 == type).Where(y => y.Value.SetId == sId).Select(z => z.Value);
                var set = data.Select(x => x.SetId).Any() ? ServerConfig.ApiDb.Query<WarningSet>("SELECT `Id`, `Name` FROM `warning_set` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = data.Select(x => x.SetId)
                }) : new List<WarningSet>();
                var device = data.Select(x => x.DeviceId).Any() ? ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, `Code` FROM `device_library` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = data.Select(x => x.DeviceId)
                }) : new List<DeviceLibrary>();
                var categories = data.Select(x => x.CategoryId).Any() ? ServerConfig.ApiDb.Query<DeviceCategory>("SELECT `Id`, `CategoryName` FROM `device_category` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = data.Select(x => x.CategoryId)
                }) : new List<DeviceCategory>();
                //var classes = ServerConfig.ApiDb.Query<DeviceClass>("SELECT `Id`, `Class` FROM `device_class` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                //{
                //    Id = data.Select(x => x.ClassId)
                //});
                foreach (var current in data.OrderBy(x => x.ItemId).ThenBy(x => x.DeviceId))
                {
                    var d = ClassExtension.ParentCopyToChild<WarningCurrent, WarningCurrentDetail>(current);
                    d.SetName = set.FirstOrDefault(x => x.Id == d.SetId)?.Name ?? "";
                    d.Code = device.FirstOrDefault(x => x.Id == d.DeviceId)?.Code ?? "";
                    //d.Class = classes.FirstOrDefault(x => x.Id == d.ClassId)?.Class ?? "";
                    d.CategoryName = categories.FirstOrDefault(x => x.Id == d.CategoryId)?.CategoryName ?? "";
                    result.datas.Add(d);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取预警管理设置
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning
        [HttpGet("Log")]
        public DataResult GetWarningLog([FromQuery]int sId, DateTime startTime, DateTime endTime, WarningType type, WarningDataType dataType)
        {
            var result = new DataResult();
            var r = ServerConfig.ApiDb.Query<WarningLog>(
                $"SELECT * FROM `warning_log` WHERE 1 = 1" +
                $" AND Type = @type" +
                $" AND `DataType` = @dataType" +
                //$"SELECT * FROM `warning_log` WHERE `MarkedDelete` = 0" +
                $"{(sId == 0 ? "" : " AND SetId = @sId")}" +
                $"{(startTime == default(DateTime) ? "" : " AND CurrentTime >= @startTime")}" +
                $"{(endTime == default(DateTime) ? "" : " AND CurrentTime <= @endTime")};", new { sId, startTime, endTime, type, dataType });

            if (r != null && r.Any())
            {
                var set = r.Select(x => x.SetId).Any() ? ServerConfig.ApiDb.Query<WarningSet>("SELECT `Id`, `Name` FROM `warning_set` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = r.Select(x => x.SetId)
                }) : new List<WarningSet>();
                var device = r.Select(x => x.DeviceId).Any() ? ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT `Id`, `Code` FROM `device_library` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = r.Select(x => x.DeviceId)
                }) : new List<DeviceLibrary>();
                var categories = r.Select(x => x.CategoryId).Any() ? ServerConfig.ApiDb.Query<DeviceCategory>("SELECT `Id`, `CategoryName` FROM `device_category` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                {
                    Id = r.Select(x => x.CategoryId)
                }) : new List<DeviceCategory>();

                foreach (var current in r)
                {
                    var d = ClassExtension.ParentCopyToChild<WarningCurrent, WarningLog>(current);
                    d.SetName = set.FirstOrDefault(x => x.Id == d.SetId)?.Name ?? "";
                    d.Code = device.FirstOrDefault(x => x.Id == d.DeviceId)?.Code ?? "";
                    //d.Class = classes.FirstOrDefault(x => x.Id == d.ClassId)?.Class ?? "";
                    d.CategoryName = categories.FirstOrDefault(x => x.Id == d.CategoryId)?.CategoryName ?? "";
                    result.datas.Add(d);
                }
            }
            return result;
        }
    }
}