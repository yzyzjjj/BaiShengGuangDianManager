using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.Utils;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartAccountHelper : DataHelper
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
        private SmartAccountHelper()
        {
            Table = "accounts";
            SameField = "Account";
            InsertSql =
                "INSERT INTO accounts (`Account`, `Password`, `Name`, `Role`, `Phone`, `EmailType`, `EmailAddress`, `MarkedDelete`, `SelfPermissions`, `AllDevice`, `DeviceIds`, `Default`, `ProductionRole`, `MaxProductionRole`) " +
                "VALUES (@Account, @Password, @Name, @Role, @Phone, @EmailType, @EmailAddress, @MarkedDelete, @SelfPermissions, @AllDevice, @DeviceIds, @Default, @ProductionRole, @MaxProductionRole);";
            UpdateSql = "UPDATE accounts SET `Account` = @Account, `Password` = @Password, `Name` = @Name, `Role` = @Role, `Phone` = @Phone, `EmailType` = @EmailType, `EmailAddress` = @EmailAddress, `MarkedDelete` = @MarkedDelete, " +
                      "`SelfPermissions` = @SelfPermissions, `AllDevice` = @AllDevice, `DeviceIds` = @DeviceIds, `Default` = @Default, `ProductionRole` = @ProductionRole, `MaxProductionRole` = @MaxProductionRole WHERE `Id` = @Id;";
        }

        public static readonly SmartAccountHelper Instance = new SmartAccountHelper();
        #region Get
        /// <summary>
        /// 将当前请求的User转换成 SmartAccount，以便获取数据
        /// </summary>
        public static SmartAccount CurrentUser { get; set; }
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

        /// <summary>
        /// 根据名字获取账号信息
        /// </summary>
        /// <param name="name">姓名</param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccountByName(string name)
        {
            return ServerConfig.ApiDb.Query<SmartAccount>("SELECT * FROM `accounts` WHERE `Name` = @name AND MarkedDelete = 0", new { name }).FirstOrDefault();
        }
        /// <summary>
        /// 根据id获取账号信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccount(int id, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions )) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE a.Id = @id {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { id }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }
        /// <summary>
        /// 根据id获取账号信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccountAll(int id)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE a.Id = @id AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { id }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }
        /// <summary>
        /// 根据账号获取账号信息
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccount(string account, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE a.Account = @account {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { account }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }

        /// <summary>
        /// 根据number获取账号信息
        /// </summary>
        /// <param name="number"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccountByNumber(string number, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE MD5(a.Number) = @number {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { number }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }

        /// <summary>
        /// 根据姓名获取账号信息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccountByName(string name, bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE a.Name = @name {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { name }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }
        /// <summary>
        /// 根据姓名获取账号信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SmartAccount GetSmartAccountByNameAll(string name)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE a.Name = @name AND b.MarkedDelete = 0;";
            var info = ServerConfig.ApiDb.Query<SmartAccount>(sql, new { name }).FirstOrDefault();
            return info == null || info.Account.IsNullOrEmpty() ? null : info;
        }
        /// <summary>
        /// 获取所有账号信息
        /// </summary>
        /// <param name="isAll">是否包含已删除</param>
        /// <returns></returns>
        public static IEnumerable<SmartAccount> GetSmartAccount(bool isAll = false)
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE {(isAll ? "" : "AND a.MarkedDelete = 0")} AND b.MarkedDelete = 0 GROUP BY a.Id ORDER BY a.Id;";
            return ServerConfig.ApiDb.Query<SmartAccount>(sql);
        }
        /// <summary>
        /// 获取所有账号信息    包括已删除
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SmartAccount> GetSmartAccountAll()
        {
            var sql = $"SELECT a.*, GROUP_CONCAT(b.`Name`) RoleName, IF ( a.SelfPermissions = '', GROUP_CONCAT(b.Permissions), CONCAT( GROUP_CONCAT(b.Permissions), ',', a.SelfPermissions ) ) Permissions FROM `accounts` a JOIN `roles` b ON FIND_IN_SET(b.Id, a.Role) != 0 " +
                      $"WHERE b.MarkedDelete = 0 GROUP BY a.Id ORDER BY a.Id;";
            return ServerConfig.ApiDb.Query<SmartAccount>(sql);
        }

        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}
