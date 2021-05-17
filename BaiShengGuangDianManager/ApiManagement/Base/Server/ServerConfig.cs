using ApiManagement.Base.Helper;
using ApiManagement.Models.AccountManagementModel;
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
        private static bool _close = false;
        public static DataBase ApiDb;
        public static DataBase DataReadDb;
        public static string GateUrl;
        public static string ErpUrl;
        public static string IsSetProcessDataKey = "IsSetProcessDataKey";
        public static Dictionary<string, Action> Loads;
        public static void Init(IConfiguration configuration)
        {
            RedisHelper.Init(configuration);
            RedisHelper.CloseWrite = _close;
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            ApiDb.CloseWrite = _close;
            Loads = new Dictionary<string, Action>
            {
                //{PermissionHelper.TableName, PermissionHelper.LoadConfig},
                {"ReadDB", LoadDateBase},
                {PermissionHelper.TableName, PermissionHelper.LoadConfig},
                {PermissionGroupHelper.TableName, PermissionGroupHelper.LoadConfig},
            };

            foreach (var action in Loads.Values)
            {
                action();
            }

            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            ErpUrl = configuration.GetAppSettings<string>("ErpUrl");
            GlobalConfig.LoadGlobalConfig();
            AccountInfoHelper.Init(configuration);
            WarningHelper.Init();
            HFlowCardHelper.Init();
            WorkFlowHelper.Init();
            SimulateHelper.Init();
            HScheduleHelper.Init();
            TimerHelper.Init();
            AnalysisHelper.Init();
            if (!RedisHelper.Exists(IsSetProcessDataKey))
            {
                RedisHelper.SetForever(IsSetProcessDataKey, 1);
            }

            Loads.Add(WarningHelper.RedisReloadKey, WarningHelper.NeedLoad);
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
