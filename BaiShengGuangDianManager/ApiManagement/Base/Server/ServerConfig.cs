using ApiManagement.Base.Helper;
using ApiManagement.Models.ManufactureModel;
using ApiManagement.Models.Notify;
using ApiManagement.Models.StatisticManagementModel;
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

            if (!RedisHelper.Exists(IsSetProcessDataKey))
            {
                RedisHelper.SetForever(IsSetProcessDataKey, 1);
            }
            //NotifyHelper.Notify("测试", NotifyTypeEnum.Main, NotifyMsgTypeEnum.text, new[] { "18815276513" });
            //NotifyHelper.Notify("测试", NotifyTypeEnum.Repair, NotifyMsgTypeEnum.text, new[] { "18815276513" });
            //NotifyHelper.Notify("测试", NotifyTypeEnum.Test, NotifyMsgTypeEnum.text, new[] { "18815276513" });
            //NotifyHelper.Notify("测试", NotifyTypeEnum.Test, NotifyMsgTypeEnum.text, new[] { "18815276513" });

            Log.InfoFormat("ServerConfig Done");
            //var p1 = new ManufacturePlan
            //{

            //};
            //var p2 = new ManufacturePlan
            //{
            //    State = ManufacturePlanState.Assigned
            //};
            //p1.HaveChange(p2, out var _);



        }
    }
}
