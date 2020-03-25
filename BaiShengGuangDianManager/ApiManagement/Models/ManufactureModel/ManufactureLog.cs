using ApiManagement.Base.Server;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 计划和任务日志
    /// </summary>
    public class ManufactureLog
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public int PlanId { get; set; }
        public string Plan { get; set; }
        /// <summary>
        /// 计划是否下发
        /// </summary>
        public bool IsAssign { get; set; }
        public int TaskId { get; set; }
        public string Task { get; set; }
        public int ItemId { get; set; }
        public string Item { get; set; }
        [JsonIgnore]
        public int Order { get; set; }
        public string Account { get; set; }
        public string AccountName { get; set; }
        public int ParsingWay { get; set; }
        public ManufactureLogType Type { get; set; }
        public string TypeDesc => Type.GetAttribute<DescriptionAttribute>()?.Description ?? "";
        [JsonIgnore]
        public string Param { get; set; }

        private List<ManufactureLogItem> _paramList;
        [JsonIgnore]
        public List<ManufactureLogItem> ParamList
        {
            get
            {
                if (_paramList == null)
                {
                    try
                    {
                        _paramList = Param.IsNullOrEmpty() ? new List<ManufactureLogItem>() : JsonConvert.DeserializeObject<List<ManufactureLogItem>>(Param);
                    }
                    catch (Exception e)
                    {
                        _paramList = new List<ManufactureLogItem>();
                    }
                }
                return _paramList;
            }
            set => _paramList = value;
        }

        /// <summary>
        /// 修改日志 标题
        /// </summary>
        public string Log => (ManufactureLogConfig.LogFormat.ContainsKey(Type)
            ? string.Format(ManufactureLogConfig.LogFormat[Type], $"&nbsp<strong>{Time.ToStr()}</strong>&nbsp",
                $"&nbsp<strong>{AccountName}</strong>&nbsp")
            : "");
        /// <summary>
        /// 修改日志 内容
        /// </summary>
        public IEnumerable<string> Items
        {
            get
            {
                return ParamList.Select(x => (ManufactureLogConfig.LogFormat.ContainsKey(x.Type)
                        ? string.Format(ManufactureLogConfig.LogFormat[x.Type], $"&nbsp<strong>{x.Field}</strong>&nbsp", $"&nbsp<strong>{x.Old}</strong>&nbsp", $"&nbsp<strong>{x.New}</strong>&nbsp") : "")
                            + (x.Items.Any() ? x.Items.Select(y =>
                             (ManufactureLogConfig.LogFormat.ContainsKey(y.Type)
                                ? string.Format(ManufactureLogConfig.LogFormat[y.Type], $"&nbsp<strong>{y.Field}</strong>&nbsp", $"&nbsp<strong>{y.Old}</strong>&nbsp", $"&nbsp<strong>{y.New}</strong>&nbsp")
                               : "")).Join("，") + "。" : "")
                    ).Where(y => !y.IsNullOrEmpty());
            }
        }

        public static void AddLog(IEnumerable<ManufactureLog> manufactureLogs)
        {
            ServerConfig.ApiDb.Execute(
                "INSERT INTO manufacture_log (`Time`, `PlanId`, `IsAssign`, `TaskId`, `ItemId`, `Account`, `Type`, `TypeDesc`, `Param`) " +
                "VALUES (@Time, @PlanId, @IsAssign, @TaskId, @ItemId, @Account, @Type, @TypeDesc, @Param);",
                manufactureLogs.Select(x =>
                {
                    switch (x.ParsingWay)
                    {
                        case 0:
                            x.Param = x.ParamList.Select(y => new
                            {
                                y.Type,
                                y.Field,
                                y.Old,
                                y.New,
                            }).ToJSON();
                            break;
                        case 1:
                            x.Param = x.ParamList.Select(y => new
                            {
                                y.Type,
                                y.Field,
                                Items = y.Items.Select(z => new
                                {
                                    z.Type,
                                    z.Field,
                                    z.Old,
                                    z.New,
                                })
                            }).ToJSON();
                            break;
                    }

                    return x;
                }));
        }
    }

    /// <summary>
    /// 计划和任务日志
    /// </summary>
    public class ManufactureLogItem
    {
        public ManufactureLogType Type { get; set; }
        public string Field { get; set; } = "";
        public string Old { get; set; } = "";
        public string New { get; set; } = "";
        public IEnumerable<ManufactureLogItem> Items { get; set; } = new List<ManufactureLogItem>();
    }
}
