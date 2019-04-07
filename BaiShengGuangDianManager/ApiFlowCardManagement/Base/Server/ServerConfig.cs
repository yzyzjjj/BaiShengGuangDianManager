using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiFlowCardManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DeviceDb;
        public static DataBase FlowcardDb;
        public static void Init(IConfiguration configuration)
        {
            FlowcardDb = new DataBase(configuration.GetConnectionString("FlowcardDb"));
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
