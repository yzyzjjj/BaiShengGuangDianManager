using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceClassHelper : DataHelper
    {
        private DeviceClassHelper()
        {
            Table = "device_class";
            InsertSql =
                "INSERT INTO `device_class` (`CreateUserId`, `MarkedDateTime`, `Class`, `Description`) VALUES (@CreateUserId, @MarkedDateTime, @Class, @Description);";

            UpdateSql = "UPDATE `device_class` SET `MarkedDateTime` = @MarkedDateTime, `Class` = @Class, `Description` = @Description WHERE `Id` = @Id;";

            SameField = "Class";
            MenuFields.AddRange(new[] { "Id", "Class" });
        }
        public static readonly DeviceClassHelper Instance = new DeviceClassHelper();
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

            return Instance.CommonGet<DeviceClass>(args, true).Select(x => new { x.Id, x.Class });
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<DeviceClass> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<DeviceClass>(args, true);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Class", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<DeviceClass> GetDetails(int qId)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (qId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", qId));
            }
            return Instance.CommonGet<DeviceClass>(args);
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