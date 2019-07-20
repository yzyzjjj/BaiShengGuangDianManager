using ModelBase.Base.Logger;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ApiManagement.Base.Server
{
    public static partial class GlobalConfig
    {
        public static string TableName = "global_config";
        /// <summary>
        /// 读取全局配置
        /// </summary>
        public static void LoadGlobalConfig()
        {
            var globalConfigs =
                ServerConfig.ApiDb.Query<dynamic>($"SELECT * FROM {TableName};");
            var fis = typeof(GlobalConfig).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var config in globalConfigs)
            {
                var fi = fis.FirstOrDefault(fItem => fItem.Name == config.key && fItem.GetCustomAttributes(typeof(DataMemberAttribute), true).Length > 0);
                if (fi != null)
                {
                    fi.SetValue(null, TypeDescriptor.GetConverter(fi.FieldType).ConvertFrom(config.Value));
                }
            }

            Log.InfoFormat("{0}:Done", TableName);
        }

        //更新配置
        public static void UpdateValue(string key, string value)
        {
            ServerConfig.ApiDb.Execute($"update {TableName} set `Value`=@value where `key`=@key", new
            {
                key,
                value
            });

            ServerConfig.RedisHelper.PublishToTable("", TableName);
            LoadGlobalConfig();
        }

        /// <summary>
        /// 刷新配置表,发送通知
        /// </summary>
        /// <param name="tableName"></param>
        public static void RefreshCnfTable(string tableName)
        {
            ServerConfig.RedisHelper.PublishToTable("", tableName);
        }

        ///// <summary>
        ///// 数据解析Id
        ///// </summary>
        //[DataMember]
        //public static int AnalysisId;

        /// <summary>
        /// 正常运行设备数量
        /// </summary>
        public static int NormalDevice = 0;
        /// <summary>
        /// 故障设备数量
        /// </summary>
        public static int FaultDevice = 0;
        /// <summary>
        /// 使用率日最大
        /// </summary>
        public static decimal MaxUseRate = 0;
        /// <summary>
        /// 使用率日最小
        /// </summary>
        public static decimal MinUseRate = 0;
        /// <summary>
        /// 同时使用台数日最大
        /// </summary>
        public static decimal MaxSimultaneousUseRate = 0;
        /// <summary>
        /// 同时使用台数日最小
        /// </summary>
        public static decimal MinSimultaneousUseRate = 0;
        /// <summary>
        /// 单台加工利用率=加工时间/24h
        /// </summary>
        public static decimal SingleProcessRate = 0;
        /// <summary>
        /// 所有利用率=总加工时间/（机台号*24h）
        /// </summary>
        public static decimal AllProcessRate = 0;
        /// <summary>
        /// 运行时间
        /// </summary>
        public static int RunTime = 0;
        /// <summary>
        /// 加工时间
        /// </summary>
        public static int ProcessTime = 0;
        /// <summary>
        /// 闲置时间 = 运行时间-加工时间
        /// </summary>
        public static int IdleTime = 0;
    }
}