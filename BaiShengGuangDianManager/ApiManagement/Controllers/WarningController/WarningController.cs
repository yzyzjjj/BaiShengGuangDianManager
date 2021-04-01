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
            var r = ServerConfig.ApiDb.Query<WarningSetWithItems>(
                $"SELECT a.*, b.`Class` FROM `warning_set` a " +
                $"JOIN `device_class` b ON a.ClassId = b.Id " +
                $"WHERE a.`MarkedDelete` = 0 AND a.`WarningType` = @type AND a.`DataType` = @dataType{(qId == 0 ? "" : " AND a.Id = @qId")};", new { qId, type, dataType });

            var d = new List<WarningSetItemDetail>();
            if (first && r.Any())
            {
                var setId = r.FirstOrDefault().Id;
                var sql = "";
                switch (dataType)
                {
                    case WarningDataType.设备数据:
                        var scriptId = r.FirstNonDefault().ScriptId;
                        sql = "SELECT b.*, a.VariableName Item, a.PointerAddress, a.VariableTypeId, a.Id DictionaryId " +
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
                            if (s.All(x => x.ItemType != val.Item2))
                            {
                                d.Add(new WarningSetItemDetail
                                {
                                    Item = val.Item1,
                                    ItemType = val.Item2
                                });
                            }
                            else
                            {
                                d.Add(s.First(x => x.Item == val.Item1));
                            }

                            d = d.OrderBy(x => WarningHelper.生产数据字段.FirstOrDefault(y => y.Item2 == x.ItemType)?.Item2).ToList();
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
                var device = DeviceLibraryHelper.GetMenus(r.SelectMany(x => x.DeviceList).Distinct()).OrderBy(x => x.Id);
                var categories = DeviceCategoryHelper.GetMenus(r.Select(x => x.CategoryId).Distinct()).OrderBy(x => x.Id);
                var script = ScriptVersionHelper.GetMenus(r.Select(x => x.ScriptId).Distinct()).OrderBy(x => x.Id);
                foreach (var dd in r)
                {
                    dd.CodeList = device.Where(x => dd.DeviceList.Contains(x.Id)).Select(y => new Tuple<int, string>(y.Id, y.Code));
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
            WarningSetWithItems set = null;
            if (!isNew)
            {
                set = WarningSetHelper.Instance.Get<WarningSetWithItems>(sId);
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
                    sql = "SELECT ScriptId, VariableName Item, PointerAddress, VariableTypeId, Id DictionaryId FROM `data_name_dictionary` WHERE ScriptId = @sId AND MarkedDelete = 0 ORDER BY VariableTypeId, PointerAddress;";
                    if (!isNew)
                    {
                        scriptId = set.ScriptId;
                        sql = "SELECT a.VariableName Item, a.PointerAddress, a.VariableTypeId, b.*, a.Id DictionaryId " +
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
                            Item = x.Item1,
                            ItemType = x.Item2
                        }));
                    }
                    else
                    {
                        var d = WarningSetItemHelper.GetWarningSetItemsBySetId(sId).ToList();
                        foreach (var val in WarningHelper.生产数据字段)
                        {
                            if (d.All(x => x.Item != val.Item1))
                            {
                                result.datas.Add(new WarningSetItem
                                {
                                    Item = val.Item1,
                                    ItemType = val.Item2
                                });
                            }
                            else
                            {
                                result.datas.Add(d.First(x => x.Item == val.Item1));
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
        public Result PutSet([FromBody] WarningSetWithItems set)
        {
            var oldSet = WarningSetHelper.Instance.Get<WarningSet>(set.Id);
            if (oldSet == null)
            {
                return Result.GenError<Result>(Error.WarningSetNotExist);
            }

            if (set.Name.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.WarningSetNotEmpty);
            }

            var names = new List<string> { set.Name };
            var ids = new List<int> { set.Id };
            if (WarningSetHelper.GetHaveSame(names, ids))
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
                        set.Items = set.Items.Where(x => x.ItemType != WarningItemType.Default).ToList();
                        foreach (var item in set.Items)
                        {
                            if (WarningHelper.生产数据单次字段.All(x => x.Item2 != item.ItemType))
                            {
                                item.DeviceIds = set.DeviceIds;
                            }
                        }
                        break;
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
                    foreach (var item in set.Items)
                    {
                        item.MarkedDateTime = markedDateTime;
                        item.SetId = set.Id;
                    }
                    if (set.Items.Any(x => !x.ValidFrequency()))
                    {
                        return Result.GenError<Result>(Error.WarningSetItemFrequencyError);
                    }
                    if (set.Items.Any(x => !x.ValidCondition()))
                    {
                        return Result.GenError<Result>(Error.WarningSetItemConditionError);
                    }

                    var oldWarningSetItems = WarningSetItemHelper.GetWarningSetItemsBySetId(set.Id);

                    var delItems = oldWarningSetItems.Where(x => set.Items.All(y => y.Id != x.Id));
                    if (delItems.Any())
                    {
                        WarningSetItemHelper.Instance.Delete(delItems.Select(x => x.Id));
                    }

                    var updateItems = set.Items.Where(x => x.Id != 0);
                    if (updateItems.Any())
                    {
                        var uIds = new List<int>();
                        foreach (var item in updateItems)
                        {
                            var oldItem = oldWarningSetItems.FirstOrDefault(x => x.Id == item.Id);
                            if (oldItem != null && ClassExtension.HaveChange(oldItem, item))
                            {
                                if (WarningHelper.生产数据单次字段.All(x => x.Item2 != item.ItemType))
                                {
                                    item.DeviceIds = set.DeviceIds;
                                }
                                uIds.Add(item.Id);
                            }
                        }
                        WarningSetItemHelper.Instance.Update(updateItems.Where(x => uIds.Contains(x.Id)));
                    }
                    var newItems = set.Items.Where(x => x.Id == 0);
                    if (newItems.Any())
                    {
                        foreach (var item in newItems)
                        {
                            item.CreateUserId = createUserId;
                        }
                        WarningSetItemHelper.Instance.Add(newItems);
                    }
                }
                else
                {
                    WarningSetItemHelper.Instance.Delete(set.Id);
                }
            }

            if (ClassExtension.HaveChange(oldSet, set))
            {
                set.MarkedDateTime = markedDateTime;
                WarningSetHelper.Instance.Update(set);
            }

            WarningHelper.UpdateConfig();
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/Warning
        [HttpPost]
        public Result PostSet([FromBody] WarningSetWithItems set)
        {
            if (set.Name.IsNullOrEmpty())
            {
                return Result.GenError<Result>(Error.WarningSetNotEmpty);
            }

            var names = new List<string> { set.Name };
            if (WarningSetHelper.GetHaveSame(names))
            {
                return Result.GenError<Result>(Error.WarningSetIsExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
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
            set.MarkedDateTime = markedDateTime;
            var id = ServerConfig.ApiDb.Query<int>(
                 "INSERT INTO `warning_set` (`CreateUserId`, `MarkedDateTime`, `WarningType`, `DataType`, `Name`, `Enable`, `ClassId`, `ScriptId`, `CategoryId`, `DeviceIds`) " +
                 "VALUES (@CreateUserId, @MarkedDateTime, @WarningType, @DataType, @Name, @Enable, @ClassId, @ScriptId, @CategoryId, @DeviceIds);SELECT LAST_INSERT_ID();",
                 set).FirstOrDefault();

            if (set.Items != null && set.Items.Any())
            {
                var dic = new List<DataNameDictionary>();
                if (set.DataType == WarningDataType.设备数据)
                {
                    var dIds = set.Items.Select(x => x.DictionaryId).Where(x => x != 0);
                    dic.AddRange(DataNameDictionaryHelper.Instance.GetAllByIds<DataNameDictionary>(dIds));
                }
                foreach (var item in set.Items)
                {
                    item.CreateUserId = createUserId;
                    item.MarkedDateTime = markedDateTime;
                    item.SetId = id;
                    item.Item = set.DataType == WarningDataType.设备数据 ? dic.FirstOrDefault(x => x.Id == item.DictionaryId)?.VariableName ?? "" : item.Item;
                }
                WarningSetItemHelper.Instance.Add<WarningSetItem>(set.Items);
            }
            WarningHelper.UpdateConfig();
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
            WarningHelper.UpdateConfig();
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
                var r = sId == 0 ? WarningHelper.CurrentData.Where(x => x.Key.Item3 == dataType && x.Key.Item4 == type).Select(y => y.Value)
                    : WarningHelper.CurrentData.Where(x => x.Key.Item3 == dataType && x.Key.Item4 == type).Where(y => y.Value.SetId == sId).Select(z => z.Value);
                if (r.Any())
                {
                    var sets = WarningSetHelper.GetMenus(r.Select(x => x.SetId));
                    var devices = DeviceLibraryHelper.GetMenus(r.Select(x => x.DeviceId));
                    var categories = DeviceCategoryHelper.GetMenus(r.Select(x => x.CategoryId));
                    //var classes = ServerConfig.ApiDb.Query<DeviceClass>("SELECT `Id`, `Class` FROM `device_class` WHERE `MarkedDelete` = 0 AND Id IN @Id;", new
                    //{
                    //    Id = data.Select(x => x.ClassId)
                    //});
                    foreach (var current in r.OrderBy(x => x.ItemId).ThenBy(x => x.DeviceId))
                    {
                        var d = ClassExtension.ParentCopyToChild<WarningCurrent, WarningCurrentDetail>(current);
                        d.SetName = sets.FirstOrDefault(x => x.Id == d.SetId)?.Name ?? "";
                        d.Code = devices.FirstOrDefault(x => x.Id == d.DeviceId)?.Code ?? "";
                        //d.Class = classes.FirstOrDefault(x => x.Id == d.ClassId)?.Class ?? "";
                        d.CategoryName = categories.FirstOrDefault(x => x.Id == d.CategoryId)?.CategoryName ?? "";
                        result.datas.Add(d);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取预警记录
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning
        [HttpGet("Log")]
        public DataResult GetWarningLog([FromQuery]int sId, DateTime startTime, DateTime endTime, WarningType type, WarningDataType dataType, string deviceIds = "")
        {
            var result = new DataResult();
            IEnumerable<int> deviceIdList = null;
            if (sId != 0)
            {
                var set = WarningSetHelper.Instance.Get<WarningSet>(sId);
                if (set == null)
                {
                    return Result.GenError<DataResult>(Error.WarningSetNotExist);
                }
                deviceIdList = deviceIds.IsNullOrEmpty() ? null : deviceIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();
            }
            if (startTime != default(DateTime))
            {
                startTime = startTime.DayBeginTime();
            }

            if (endTime != default(DateTime))
            {
                endTime = endTime.DayEndTime();
            }

            result.datas.AddRange(WarningLogHelper.GetWarningLogs(startTime, endTime, sId, 0, type, dataType, deviceIdList, null));
            return result;
        }

        /// <summary>
        /// 处理设备预警
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning
        [HttpPost("Deal")]
        public Result DealWarningLog([FromBody]WarningClear clear)
        {
            var set = WarningSetHelper.Instance.Get<WarningSet>(clear.SetId);
            if (set == null)
            {
                return Result.GenError<Result>(Error.WarningSetNotExist);
            }
            if (set.WarningType != WarningType.设备 || set.DataType != WarningDataType.设备数据)
            {
                return Result.GenError<Result>(Error.WarningSetItemDataTypeError);
            }
            if (clear.DeviceIds.IsNullOrEmpty())
            {
                clear.DeviceIds = set.DeviceIds;
            }
            if (!clear.DeviceIdList.Any())
            {
                return Result.GenError<Result>(Error.DeviceNotExist);
            }
            var markedDateTime = DateTime.Now;
            var createUserId = Request.GetIdentityInformation();
            clear.CreateUserId = createUserId;
            clear.MarkedDateTime = markedDateTime;
            clear.DealTime = markedDateTime;
            WarningLogHelper.DealWarningLogs(clear);
            WarningClearHelper.Instance.Add(clear);
            return Result.GenError<Result>(Error.Success);
        }

        /// <summary>
        /// 获取处理设备预警记录
        /// </summary>
        /// <returns></returns>
        // POST: api/Warning
        [HttpGet("DealLog")]
        public DataResult GetWarningDealLog([FromQuery]int sId, DateTime startTime, DateTime endTime, WarningType type, WarningDataType dataType, string deviceIds = "")
        {
            var result = new DataResult();
            if (sId != 0)
            {
                var set = WarningSetHelper.Instance.Get<WarningSet>(sId);
                if (set == null)
                {
                    return Result.GenError<DataResult>(Error.WarningSetNotExist);
                }
                if (set.WarningType != WarningType.设备 || set.DataType != WarningDataType.设备数据)
                {
                    return Result.GenError<DataResult>(Error.WarningSetItemDataTypeError);
                }
            }
            var markedDateTime = DateTime.Now;
            if (startTime == default(DateTime))
            {
                startTime = markedDateTime;
            }
            if (endTime == default(DateTime))
            {
                endTime = markedDateTime;
            }
            startTime = startTime.DayBeginTime();
            endTime = endTime.DayEndTime();
            var deviceIdList = deviceIds.IsNullOrEmpty() ? null : deviceIds.Split(",").Select(x => int.TryParse(x, out var a) ? a : 0).Where(y => y != 0).ToList();
            result.datas.AddRange(WarningClearHelper.GetWarningClears(startTime, endTime, sId, type, dataType, deviceIdList));
            return result;
        }
    }
}