using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;

namespace ApiRepairManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase RepairDb;

        public static void Init(IConfiguration configuration)
        {
            RepairDb = new DataBase(configuration.GetConnectionString("RepairDb"));

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
