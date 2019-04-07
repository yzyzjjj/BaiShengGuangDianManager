using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiDeviceManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DeviceDb;
        public static string GateUrl;
        public static void Init(IConfiguration configuration)
        {
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));
            GateUrl = configuration.GetAppSettings<string>("GateUrl");

            Log.InfoFormat("ServerConfig Done");
        }


    }
}
