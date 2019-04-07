using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiProcessManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DeviceDb;
        public static DataBase FlowcardDb;
        public static DataBase ProcessDb;

        public static void Init(IConfiguration configuration)
        {
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));
            FlowcardDb = new DataBase(configuration.GetConnectionString("FlowcardDb"));
            ProcessDb = new DataBase(configuration.GetConnectionString("ProcessDb"));

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
