using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Utils;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountModel
{
    public class AccountInfoHelper : DataHelper
    {
        public static string PasswordKey;
        public static string EmailAccount;
        public static string EmailPassword;
        public static void Init(IConfiguration configuration)
        {
            PasswordKey = configuration.GetAppSettings<string>("PasswordKey");
            EmailAccount = configuration.GetAppSettings<string>("EmailAccount");
            EmailPassword = configuration.GetAppSettings<string>("EmailPassword");
        }
        private AccountInfoHelper()
        {
            Table = "accounts";
            InsertSql =
                "INSERT INTO accounts (`Account`, `Password`, `Name`, `Role`, `Phone`, `EmailType`, `EmailAddress`, `MarkedDelete`, `SelfPermissions`, `AllDevice`, `DeviceIds`, `Default`, `ProductionRole`, `MaxProductionRole`) " +
                "VALUES (@Account, @Password, @Name, @Role, @Phone, @EmailType, @EmailAddress, @MarkedDelete, @SelfPermissions, @AllDevice, @DeviceIds, @Default, @ProductionRole, @MaxProductionRole);";
            UpdateSql = "UPDATE accounts SET `Password` = @Password, `Name` = @Name, `Role` = @Role, `Phone` = @Phone, `EmailType` = @EmailType, `EmailAddress` = @EmailAddress, `MarkedDelete` = @MarkedDelete, " +
                      "`SelfPermissions` = @SelfPermissions, `AllDevice` = @AllDevice, `DeviceIds` = @DeviceIds, `Default` = @Default, `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;";

            SameField = "Account";
            MenuFields.AddRange(new[] { "Id", "Account", "Name" });
        }

        public static readonly AccountInfoHelper Instance = new AccountInfoHelper();
        #region Get
        /// <summary>
        /// 将当前请求的User转换成 SmartAccount，以便获取数据
        /// </summary>
        //public static AccountInfo CurrentUser { get; set; }
        /// <summary>
        /// 账号创建密码规则
        /// </summary>
        /// <param name="account"></param>
        /// <param name="pwd">两次MD5</param>
        /// <returns></returns>
        public static string GenAccountPwd(string account, string pwd)
        {
            var pwdStr = pwd + account + PasswordKey;
            return MD5Util.GetMd5Hash(pwdStr);
        }
        /// <summary>
        /// 账号创建密码规则 一次MD5
        /// </summary>
        /// <param name="account"></param>
        /// <param name="pwd">两次MD5</param>
        /// <returns></returns>
        public static string GenAccountPwdByOne(string account, string pwd)
        {
            pwd = MD5Util.GetMd5Hash(pwd);
            var pwdStr = pwd + account + PasswordKey;
            return MD5Util.GetMd5Hash(pwdStr);
        }
        /// <summary>
        /// 使用原始密码生成数据库密码
        /// </summary>
        /// <param name="account"></param>
        /// <param name="originalPwd">原始密码</param>
        /// <returns></returns>
        public static string GenAccountPwdByOriginalPwd(string account, string originalPwd)
        {
            var pwd = MD5Util.GetMd5Hash(MD5Util.GetMd5Hash(originalPwd));
            var pwdStr = pwd + account + PasswordKey;
            return MD5Util.GetMd5Hash(pwdStr);
        }

        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Account", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }

        /// <summary>
        /// 根据获取账号信息
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        public static AccountInfo GetAccountInfo(int accountId, bool isAll = false)
        {
            return GetAccountInfo(accountId, "", "", "", isAll);
        }

        /// <summary>
        /// 根据account获取账号信息
        /// </summary>
        /// <param name="account"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        public static AccountInfo GetAccountInfo(string account, bool isAll = false)
        {
            return GetAccountInfo(0, account, "", "", isAll);
        }

        /// <summary>
        /// 根据number获取账号信息
        /// </summary>
        /// <param name="number"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static AccountInfo GetAccountInfoByNumber(string number, bool isAll = false)
        {
            return GetAccountInfo(0, "", number, "", isAll);
        }

        /// <summary>
        /// 根据name获取账号信息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static AccountInfo GetAccountInfoByName(string name, bool isAll = false)
        {
            return GetAccountInfo(0, "", "", name, isAll);
        }
        /// <summary>
        /// 根据获取账号信息
        /// </summary>
        /// <returns></returns>
        public static AccountInfo GetAccountInfo(int accountId, string account, string number, string name, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE{(accountId == 0 ? "" : " a.Id = @accountId AND")}" +
                      $"{(account.IsNullOrEmpty() ? "" : " a.Account = @account AND")}" +
                      $"{(number.IsNullOrEmpty() ? "" : " MD5(a.Number) = @number AND")}" +
                      $"{(name.IsNullOrEmpty() ? "" : " a.Name = @name AND")}" +
                      $"{(isAll ? "" : " b.MarkedDelete = 0 AND")}" +
                      $" 1 = 1;";
            var info = ServerConfig.ApiDb.Query<AccountInfo>(sql, new { accountId, account, number, name }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }

        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<AccountInfo> GetMenu(bool isAll, int accountId, string account = "", string number = "", string name = "",
            IEnumerable<int> accountIds = null, IEnumerable<string> accounts = null, IEnumerable<string> numbers = null, IEnumerable<string> names = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (accountId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", accountId));
            }
            if (!account.IsNullOrEmpty())
            {
                args.Add(new Tuple<string, string, dynamic>("Account", "=", account));
            }
            if (!number.IsNullOrEmpty())
            {
                args.Add(new Tuple<string, string, dynamic>("MD5(Number)", "=", number));
            }
            if (!name.IsNullOrEmpty())
            {
                args.Add(new Tuple<string, string, dynamic>("Name", "=", name));
            }
            if (accountIds != null && accountIds.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", accountIds));
            }
            if (accounts != null && accounts.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Account", "IN", accounts));
            }
            if (numbers != null && numbers.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("MD5(Number)", "IN", numbers));
            }
            if (names != null && names.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Name", "IN", names));
            }
            if (!isAll)
            {
                args.Add(new Tuple<string, string, dynamic>("MarkedDelete", "=", 0));
            }

            return Instance.CommonGet<AccountInfo>(args, true);
        }

        /// <summary>
        /// 获取所有账号信息
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static IEnumerable<AccountInfo> GetAccountInfos(int accountId, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE{(accountId == 0 ? "" : " a.Id = @accountId AND")}" +
                      $"{(isAll ? "" : " a.MarkedDelete = 0 AND")}" +
                      $" b.MarkedDelete = 0 GROUP BY a.Id ORDER BY a.Id;";
            return ServerConfig.ApiDb.Query<AccountInfo>(sql);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<AccountInfo> GetAccountInfoByAccountIds(IEnumerable<int> accountIds)
        {
            if (accountIds == null || !accountIds.Any())
            {
                return new List<AccountInfo>();
            }
            var args = new List<Tuple<string, string, dynamic>>();
            args.Add(new Tuple<string, string, dynamic>("Id", "IN", accountIds));

            return Instance.CommonGet<AccountInfo>(args);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<AccountInfo> GetAccountInfoByAccounts(IEnumerable<string> accounts)
        {
            if (accounts == null || !accounts.Any())
            {
                return new List<AccountInfo>();
            }
            var args = new List<Tuple<string, string, dynamic>>();
            args.Add(new Tuple<string, string, dynamic>("Account", "IN", accounts));

            return Instance.CommonGet<AccountInfo>(args);
        }

        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<AccountInfo> GetAccountInfoByNames(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<AccountInfo>();
            }
            var args = new List<Tuple<string, string, dynamic>>();
            args.Add(new Tuple<string, string, dynamic>("Name", "IN", names));

            return Instance.CommonGet<AccountInfo>(args);
        }

        /// <summary>
        /// 获取导入账号
        /// </summary>
        public static IEnumerable<AccountInfo> GetAccountInfoByImport()
        {
            return ServerConfig.ApiDb.Query<AccountInfo>("SELECT * FROM `accounts` WHERE `IsErp` = 1");
        }
        ///// <summary>
        ///// 根据名字获取账号信息
        ///// </summary>
        ///// <param name="name">姓名</param>
        ///// <returns></returns>
        //public static AccountInfo GetAccountInfoByName(string name)
        //{
        //    return ServerConfig.ApiDb.Query<AccountInfo>("SELECT * FROM `accounts` WHERE `Name` = @name AND MarkedDelete = 0", new { name }).FirstOrDefault();
        //}
        ///// <summary>
        ///// 根据名字获取账号信息
        ///// </summary>
        ///// <param name="names">姓名</param>
        ///// <returns></returns>
        //public static IEnumerable<AccountInfo> GetAccountInfoByName(IEnumerable<string> names)
        //{
        //    return ServerConfig.ApiDb.Query<AccountInfo>("SELECT * FROM `accounts` WHERE `Name` IN @names AND MarkedDelete = 0", new { names });
        //}

        ///// <summary>
        ///// 根据number获取账号信息
        ///// </summary>
        ///// <param name="number"></param>
        ///// <param name="isAll">是否包含已删除</param>
        ///// <returns></returns>
        //public static AccountInfo GetAccountInfoByNumber(string number, bool isAll = false)
        //{
        //    var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
        //              $"WHERE MD5(a.Number) = @number {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
        //    var info = ServerConfig.ApiDb.Query<AccountInfo>(sql, new { number }).FirstOrDefault();
        //    return info == null || info.Account.IsNullOrEmpty() ? null : info;
        //}

        ///// <summary>
        ///// 根据姓名获取账号信息
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="isAll">是否包含已删除</param>
        ///// <returns></returns>
        //public static AccountInfo GetAccountInfoByName(string name, bool isAll = false)
        //{
        //    var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
        //              $"WHERE a.Name = @name {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
        //    var info = ServerConfig.ApiDb.Query<AccountInfo>(sql, new { name }).FirstOrDefault();
        //    return info == null || info.Account.IsNullOrEmpty() ? null : info;
        //}
        ///// <summary>
        ///// 根据姓名获取账号信息
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public static AccountInfo GetAccountInfoByNameAll(string name)
        //{
        //    var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
        //              $"WHERE a.Name = @name AND b.MarkedDelete = 0;";
        //    var info = ServerConfig.ApiDb.Query<AccountInfo>(sql, new { name }).FirstOrDefault();
        //    return info == null || info.Account.IsNullOrEmpty() ? null : info;
        //}

        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}
