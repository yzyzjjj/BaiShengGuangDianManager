using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;

namespace ApiFlowCardManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DeviceDb;
        public static DataBase FlowCardDb;
        public static DataBase ProcessDb;
        public static void Init(IConfiguration configuration)
        {
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));
            FlowCardDb = new DataBase(configuration.GetConnectionString("FlowCardDb"));
            ProcessDb = new DataBase(configuration.GetConnectionString("ProcessDb"));

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
