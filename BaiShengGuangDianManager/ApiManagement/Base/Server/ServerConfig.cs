using ApiManagement.Base.Helper;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;

namespace ApiManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase ApiDb;
        public static string GateUrl;
        public static string ErpUrl;
        public static string IsSetProcessDataKey = "IsSetProcessDataKey";
        public static Dictionary<string, Action> Loads;
        public static void Init(IConfiguration configuration)
        {
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            ErpUrl = configuration.GetAppSettings<string>("ErpUrl");
            GlobalConfig.LoadGlobalConfig();
            SmartAccountHelper.Init(configuration);
            RedisHelper.Init(configuration);
            Loads = new Dictionary<string, Action>
            {
                //{PermissionHelper.TableName, PermissionHelper.LoadConfig},
            };

            foreach (var action in Loads.Values)
            {
                action();
            }
            if (!RedisHelper.Exists(IsSetProcessDataKey))
            {
                RedisHelper.SetForever(IsSetProcessDataKey, 1);
            }

            Log.InfoFormat("ServerConfig Done");
        }
        public static void ReloadConfig(string tableName)
        {
            if (tableName != "all" && !Loads.ContainsKey(tableName))
            {
                return;
            }

            if (tableName == "all")
            {
                foreach (var action in Loads.Values)
                {
                    action();
                }
            }
            else
            {
                if (Loads.ContainsKey(tableName))
                {
                    Loads[tableName]();
                }
            }
        }
    }
}
