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

        /// <summary>
        /// 公司名
        /// </summary>
        [DataMember]
        public static string CompanyName;

    }
}