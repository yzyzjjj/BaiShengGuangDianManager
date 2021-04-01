using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class ScriptVersionHelper : DataHelper
    {
        private ScriptVersionHelper()
        {
            Table = "script_version";
            InsertSql =
                "UPDATE script_version SET `MarkedDateTime` = @MarkedDateTime, `DeviceModelId` = @DeviceModelId, `ScriptName` = @ScriptName, `ScriptFile` = @ScriptFile WHERE `Id` = @Id;";

            UpdateSql = "UPDATE script_version SET `MarkedDateTime` = @MarkedDateTime, `DeviceModelId` = @DeviceModelId, `ScriptName` = @ScriptName, `ScriptFile` = @ScriptFile WHERE `Id` = @Id;";

            SameField = "ScriptName";
            MenuFields.AddRange(new[] { "Id", "ScriptName" });
        }
        public static readonly ScriptVersionHelper Instance = new ScriptVersionHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            return Instance.CommonGet<ScriptVersion>(args, true).Select(x => new { x.Id, x.ScriptName }).OrderByDescending(x => x.Id);
        }

        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<ScriptVersion> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<ScriptVersion>(args, true);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ScriptName", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<ScriptVersion> GetDetails(int qId, int deviceModelId)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (qId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", qId));
            }
            if (deviceModelId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("DeviceModelId", "FIND_IN_SET", deviceModelId));
            }

            return Instance.CommonGet<ScriptVersion>(args);
        }
        #endregion

        #region Add

        public static int Add(ScriptVersion scriptVersion)
        {
            return ServerConfig.ApiDb.Query<int>(
                "INSERT INTO script_version (`CreateUserId`, `MarkedDateTime`, `DeviceModelId`, `ScriptName`, `ValueNumber`, `InputNumber`, `OutputNumber`, `HeartPacket`, `ScriptFile`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @DeviceModelId, @ScriptName, @ValueNumber, @InputNumber, @OutputNumber, @HeartPacket, @ScriptFile);SELECT LAST_INSERT_ID();",
                scriptVersion).FirstOrDefault();
        }
        #endregion

        #region Update

        public static void Update(ScriptVersion scriptVersion)
        {
            ServerConfig.ApiDb.Execute(
                "UPDATE script_version SET `MarkedDateTime` = @MarkedDateTime, `ValueNumber` = @ValueNumber, `InputNumber` = @InputNumber, `OutputNumber` = @OutputNumber, `MaxValuePointerAddress` = @MaxValuePointerAddress, " +
                "`MaxInputPointerAddress` = @MaxInputPointerAddress, `MaxOutputPointerAddress` = @MaxOutputPointerAddress, `HeartPacket` = @HeartPacket WHERE `Id` = @Id;", scriptVersion);
        }
        #endregion

        #region Delete
        #endregion
    }
}