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
        public static string GateUrl;
        public static RedisCacheHelper RedisHelper;
        public static void Init(IConfiguration configuration)
        {
            ApiDb = new DataBase(configuration.GetConnectionString("ApiDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            RedisHelper = new RedisCacheHelper(configuration);
            FlowCardHelper.Init();
            Log.InfoFormat("ServerConfig Done");
        }


    }
}
