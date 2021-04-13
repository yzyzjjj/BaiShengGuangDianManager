using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace ApiManagement.Base.Helper
{
    public enum CodeType
    {
        默认,
        流程卡,
    }
    public class CodeHelper
    {
        private const string RedisPre = "CodeHelper";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeType">类型</param>
        /// <param name="time">时间</param>
        /// <param name="suffix">后缀</param>
        /// <returns></returns>
        public static string GenCode(CodeType codeType, DateTime time, string suffix = "")
        {
            var results = new List<string>();
            switch (codeType)
            {
                case CodeType.流程卡: results.AddRange(GenFlowCard(1, time, suffix)); break;
            }
            return results.FirstOrDefault() ?? "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeType">类型</param>
        /// <param name="count">数量</param>
        /// <param name="time">时间</param>
        /// <param name="suffix">后缀</param>
        /// <returns></returns>
        public static IEnumerable<string> GenCodes(CodeType codeType, int count, DateTime time, string suffix = "")
        {
            var results = new List<string>();
            switch (codeType)
            {
                case CodeType.流程卡: results.AddRange(GenFlowCard(count, time, suffix)); break;
            }
            return results;
        }
        /// <summary>
        /// 00 201007 0000
        /// </summary>
        /// <param name="count">数量</param>
        /// <param name="time">时间</param>
        /// <param name="suffix">后缀</param>
        /// <returns></returns>
        private static IEnumerable<string> GenFlowCard(int count, DateTime time, string suffix)
        {
            var redisKey = $"{RedisPre}:{CodeType.流程卡}";
            var str = new List<string>();
            var ws = Stopwatch.StartNew();
            while (ws.ElapsedMilliseconds < 10 * 1000)
            {
                if (!RedisHelper.SetIfNotExist(redisKey, DateTime.Today.ToStr()))
                {
                    continue;
                }

                var startKey = $"{RedisPre}:{DateTime.Today.ToDateStr()}-{CodeType.流程卡}";
                var startValue = RedisHelper.Get<int>(startKey);
                for (var i = 0; i < count; i++)
                {
                    str.Add($"{((int)CodeType.流程卡):D2}{DateTime.Today.ToStrShort()}{startValue++:D4}{suffix}");
                }
                RedisHelper.SetForever(startKey, startValue);
                RedisHelper.Remove(redisKey);
                break;
            }
            return str;
        }
    }
}
