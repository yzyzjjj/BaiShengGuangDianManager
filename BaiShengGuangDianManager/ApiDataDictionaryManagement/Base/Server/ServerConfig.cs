using Microsoft.Extensions.Configuration;
using ModelBase.Base.Dapper;
using ModelBase.Base.Logger;

namespace ApiDataDictionaryManagement.Base.Server
{
    public class ServerConfig
    {
        public static DataBase DictionaryDb;
        public static DataBase DeviceDb;
        public static void Init(IConfiguration configuration)
        {
            DictionaryDb = new DataBase(configuration.GetConnectionString("DictionaryDb"));
            DeviceDb = new DataBase(configuration.GetConnectionString("DeviceDb"));

            Log.InfoFormat("ServerConfig Done");
        }
    }
}
