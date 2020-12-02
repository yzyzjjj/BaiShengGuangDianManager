using ApiManagement.Base.Helper;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase WebDb;
        public static DataBase ApiDb;
        public static string GateUrl;
        public static string ErpUrl;
        public static RedisCacheHelper RedisHelper;
        public static string IsSetProcessDataKey = "IsSetProcessDataKey";

        public static void Init(IConfiguration configuration)
        {
            WebDb = new DataBase(configuration.GetConnectionString("WebDb"));
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            ErpUrl = configuration.GetAppSettings<string>("ErpUrl");
            GlobalConfig.LoadGlobalConfig();
            RedisHelper = new RedisCacheHelper(configuration);
            FlowCardHelper.Init();
            AnalysisHelper.Init();
            TimerHelper.Init();
            WarningHelper.Init();
            WorkFlowHelper.Instance.Init();
            ScheduleHelper.Instance.Init();

            if (!RedisHelper.Exists(IsSetProcessDataKey))
            {
                RedisHelper.SetForever(IsSetProcessDataKey, 1);
            }

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
