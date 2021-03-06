﻿using System;
using ModelBase.Base.Logger;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ApiManagement.Base.Helper;

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
                var fi = fis.FirstOrDefault(fItem => fItem.Name == config.Key && fItem.GetCustomAttributes(typeof(DataMemberAttribute), true).Length > 0);
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

            RedisHelper.PublishToTable(TableName);
            LoadGlobalConfig();
        }

        /// <summary>
        /// 刷新配置表,发送通知
        /// </summary>
        /// <param name="tableName"></param>
        public static void RefreshCnfTable(string tableName)
        {
            RedisHelper.PublishToTable(tableName);
        }

        /// <summary>
        /// 公司名
        /// </summary>
        [DataMember]
        public static string CompanyName;
        /// <summary>
        /// 白班上班时间 8:00
        /// </summary>
        [DataMember]
        public static TimeSpan Morning;
        /// <summary>
        /// 中午吃饭时间 11:30
        /// </summary>
        [DataMember]
        public static TimeSpan Noon;
        /// <summary>
        /// 下午上班时间 12:30
        /// </summary>
        [DataMember]
        public static TimeSpan Afternoon;
        /// <summary>
        /// 正常下班时间 17:00
        /// </summary>
        [DataMember]
        public static TimeSpan Evening;
        /// <summary>
        /// 晚上饭后上班时间 18:00
        /// </summary>
        [DataMember]
        public static TimeSpan Night18;
        /// <summary>
        /// 夜班上班时间 20:00
        /// </summary>
        [DataMember]
        public static TimeSpan Night20;
    }
}