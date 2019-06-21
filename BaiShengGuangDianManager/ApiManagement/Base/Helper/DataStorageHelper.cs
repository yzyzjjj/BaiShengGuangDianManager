using ApiManagement.Base.Control;
using ApiManagement.Base.Server;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Logger;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ApiManagement.Base.Helper
{
    /// <summary>
    /// 数据解析
    /// </summary>
    public class DataStorageHelper
    {
        private static string _pre = "Analysis";
        private static Timer _analysis1;
        //private static Timer _analysis2;
        private static int _dealLength = 1000;
        public static void Init(IConfiguration configuration)
        {
            _analysis1 = new Timer(Analysis, null, 5000, 1000);
            //_analysis2 = new Timer(Analysis, null, 5000, 1000);
        }
        private static void Analysis(object state)
        {
            var lockKey = $"{_pre}:Lock";
            var redisKey = $"{_pre}:Id";
            if (ServerConfig.RedisHelper.SetIfNotExist(lockKey, "1"))
            {
                var startId = ServerConfig.RedisHelper.Get<int>(redisKey);
                Log.DebugFormat("统计startId：{0}", startId);
                var mData = ServerConfig.DataStorageDb.Query<dynamic>(
                    "SELECT * FROM `npc_monitoring_data` WHERE Id > @Id ORDER BY Id LIMIT @limit;", new
                    {
                        Id = startId,
                        limit = _dealLength
                    });
                if (mData.Any())
                {
                    var endId = mData.Last().Id;
                    Log.DebugFormat("统计endId：{0}", endId);
                    if (endId > startId)
                    {
                        ServerConfig.RedisHelper.SetForever(redisKey, endId);

                        foreach (var data in mData)
                        {
                            var infoMessagePacket = new DeviceInfoMessagePacket(data.ValNum, data.InNum, data.OutNum);
                            var analysisData = infoMessagePacket.Deserialize(data.Data);
                            data.Data = JsonConvert.SerializeObject(analysisData);
                        }

                        ServerConfig.ApiDb.ExecuteAsync(
                            "INSERT INTO npc_monitoring_analysis (`SendTime`, `DeviceId`, `ScriptId`, `Ip`, `Port`, `Data`) " +
                            "VALUES (@SendTime, @DeviceId, @ScriptId, @Ip, @Port, @Data);", mData);
                    }
                }
                ServerConfig.RedisHelper.Remove(lockKey);
            }
        }
        public class DeviceInfoResult
        {
            public List<int> vals;
            public List<int> ins;
            public List<int> outs;
        }
    }
}
