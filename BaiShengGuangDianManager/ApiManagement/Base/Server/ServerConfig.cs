using ApiManagement.Base.Helper;
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
        public static void Init(IConfiguration configuration)
        {
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            DataStorageDb = new DataBase(configuration.GetConnectionString("DataStorageDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            GlobalConfig.LoadGlobalConfig();
            RedisHelper = new RedisCacheHelper(configuration);
            FlowCardHelper.Init(configuration);
            DataStorageHelper.Init(configuration);
            Log.InfoFormat("ServerConfig Done");
        }
    }
}
