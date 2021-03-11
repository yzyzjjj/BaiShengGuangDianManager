using ApiManagement.Base.Helper;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using ModelBase.Models.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase ApiDb;
        public static DataBase DataReadDb;
        public static string GateUrl;
        public static string ErpUrl;
        public static string IsSetProcessDataKey = "IsSetProcessDataKey";
        public static Dictionary<string, Action> Loads;
        public static void Init(IConfiguration configuration)
        {
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            Loads = new Dictionary<string, Action>
            {
                //{PermissionHelper.TableName, PermissionHelper.LoadConfig},
                {"ReadDB", LoadDateBase},
            };

            foreach (var action in Loads.Values)
            {
                action();
            }

            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            ErpUrl = configuration.GetAppSettings<string>("ErpUrl");
            GlobalConfig.LoadGlobalConfig();
            SmartAccountHelper.Init(configuration);
            RedisHelper.Init(configuration);
            AnalysisHelper.Init();
            HFlowCardHelper.Init();
            WarningHelper.Init();
            WorkFlowHelper.Init();
            SimulateHelper.Init();
            HScheduleHelper.Init();
            TimerHelper.Init();
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

        private static void LoadDateBase()
        {
            var dbs = ApiDb.Query<ServerDataBase>("SELECT * FROM `management_database`;");
            var dataRead = dbs.Where(x => x.Type == DataBaseType.Data && x.Read);
            if (dataRead.Count() != 1)
            {
                throw new Exception($"LoadDateBase Read DataBase, {dataRead.Count()}!!!");
            }

            DataReadDb = new DataBase(dataRead.First().DataBase);
        }
    }
}
