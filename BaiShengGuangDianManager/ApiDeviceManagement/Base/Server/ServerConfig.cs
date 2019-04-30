using ApiDeviceManagement.Base.Helper;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiDeviceManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DeviceDb;
        public static DataBase FlowCardDb;
        public static DataBase ProcessDb;
        public static DataBase RepairDb;
        public static string GateUrl;
        public static RedisCacheHelper RedisHelper;
        public static void Init(IConfiguration configuration)
        {
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));
            FlowCardDb = new DataBase(configuration.GetConnectionString("FlowCardDb"));
            ProcessDb = new DataBase(configuration.GetConnectionString("ProcessDb"));
            RepairDb = new DataBase(configuration.GetConnectionString("RepairDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");
            RedisHelper = new RedisCacheHelper(configuration);
            Log.InfoFormat("ServerConfig Done");
        }


    }
}
