using ApiManagement.Base.Server;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.HttpServer;
using ModelBase.Base.Logger;
using ModelBase.Base.Utils;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace ApiManagement.Base.Helper
{
    public class AccountHelper
    {
        private static string _createUserId = "ErpSystem";
        private static string _url = "";
        private static Timer _checkTimer;

        private static readonly string CheckAccountPre = "CheckAccount";
        private static readonly string CheckAccountLock = $"{CheckAccountPre}:Lock";
        public static void Init(IConfiguration configuration)
        {
            _url = configuration.GetAppSettings<string>("ErpUrl");
            _checkTimer = new Timer(CheckAccount, null, 5000, 1000 * 60);
        }

        private static void CheckAccount(object state)
        {
            if (ServerConfig.RedisHelper.SetIfNotExist(CheckAccountLock, "lock"))
            {
                ServerConfig.RedisHelper.SetExpireAt(CheckAccountLock, DateTime.Now.AddMinutes(5));
                var f = HttpServer.Get(_url, new Dictionary<string, string>
                {
                    { "type", "getUser" },
                });
                if (f == "fail")
                {
                    Log.ErrorFormat("CheckAccount 请求erp获取账号数据失败,url:{0}", _url);
                }
                else
                {
                    try
                    {
                        var rr = HttpUtility.UrlDecode(f);
                        var res = JsonConvert.DeserializeObject<ErpAccount[]>(rr);
                        if (res.Any())
                        {
                            var accounts = ServerConfig.WebDb.Query<AccountInfo>("SELECT * FROM `accounts`;");
                            var add = res.Where(x => accounts.All(y => y.Account != x.f_username));
                            if (add.Any())
                            {
                                var role = ServerConfig.WebDb.Query<int>("SELECT Id FROM `roles` WHERE New = 1;");
                                ServerConfig.WebDb.Execute(
                                    "INSERT INTO accounts (`Account`, `Name`, `Role`, `DeviceIds`, `IsDeleted`) VALUES (@Account, @Name, @Role, '', @IsDeleted);",
                                    add.Select(x => new AccountInfo
                                    {
                                        Account = x.f_username,
                                        Name = x.f_name,
                                        Role = role?.Join(",") ?? "",
                                        IsDeleted = x.ifdelete,
                                    }));
                            }

                            var update = res.Where(x => accounts.Any(y => y.Account == x.f_username) &&
                                                        (!accounts.First(y => y.Account == x.f_username).IsDeleted && x.ifdelete || accounts.First(y => y.Account == x.f_username).Name != x.f_name));
                            if (update.Any())
                            {
                                ServerConfig.WebDb.Execute(
                                    "UPDATE accounts SET `Name` = @Name, `IsDeleted` = @IsDeleted WHERE `Account` = @Account;",
                                    update.Select(x => new AccountInfo
                                    {
                                        Account = x.f_username,
                                        Name = x.f_name,
                                        IsDeleted = x.ifdelete,
                                    }));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("CheckAccount erp账号数据解析失败,原因:{0},错误:{1}", e.Message, e.StackTrace);
                    }
                }

                ServerConfig.RedisHelper.Remove(CheckAccountLock);
            }
        }

        public class AccountInfo
        {
            /// <summary>
            /// 账号Id
            /// </summary>
            public int Id { get; set; }
            public string Account { get; set; }
            [JsonIgnore]
            public string Password { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
            private IEnumerable<int> _roleList { get; set; }
            public IEnumerable<int> RoleList
            {
                get => _roleList ?? (_roleList = !Role.IsNullOrEmpty() ? Role.Split(",").Select(int.Parse) : new List<int>());
                set
                {
                    _roleList = value;
                    Role = _roleList.Join(",");
                }
            }

            public bool IsDeleted { get; set; }
        }

        public class ErpAccount
        {
            public string f_name;
            public string f_username;
            public string f_ifdelete;
            public bool ifdelete => f_ifdelete == "1";
        }
    }
}
