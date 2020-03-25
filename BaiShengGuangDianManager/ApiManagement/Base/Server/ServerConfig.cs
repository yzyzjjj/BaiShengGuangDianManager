using ApiManagement.Base.Helper;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.StatisticManagementModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase ApiDb;
        public static DataBase DataStorageDb;
        public static string GateUrl;
        public static RedisCacheHelper RedisHelper;
        public static MonitoringKanban MonitoringKanban;
        public static string IsSetProcessDataKey = "IsSetProcessDataKey";

        public static void Init(IConfiguration configuration)
        {
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            DataStorageDb = new DataBase(configuration.GetConnectionString("DataStorageDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            GlobalConfig.LoadGlobalConfig();
            RedisHelper = new RedisCacheHelper(configuration);
            FlowCardHelper.Init(configuration);
            AnalysisHelper.Init(configuration);
            SpotCheckHelper.Init(configuration);
            _6sHelper.Init(configuration);
            ManufactureHelper.Init(configuration);

            if (!RedisHelper.Exists(IsSetProcessDataKey))
            {
                RedisHelper.SetForever(IsSetProcessDataKey, 1);
            }

            Log.InfoFormat("ServerConfig Done");
            //var p1 = new ManufacturePlan
            //{

            //};
            //var p2 = new ManufacturePlan
            //{
            //    State = ManufacturePlanState.Assigned
            //};
            //p1.HaveChange(p2, out var _);
        }
    }
}
