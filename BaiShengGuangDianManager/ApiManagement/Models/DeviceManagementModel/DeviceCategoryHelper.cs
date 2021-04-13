using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceCategoryHelper : DataHelper
    {
        private DeviceCategoryHelper()
        {
            Table = "device_category";
            InsertSql =
                "INSERT INTO `device_category` (`CreateUserId`, `MarkedDateTime`, `CategoryName`, `Description`) VALUES (@CreateUserId, @MarkedDateTime, @CategoryName, @Description);";

            UpdateSql = "UPDATE `device_category` SET `MarkedDateTime` = @MarkedDateTime, `CategoryName` = @CategoryName, `Description` = @Description WHERE `Id` = @Id;";

            SameField = "CategoryName";
            MenuFields.AddRange(new[] { "Id", "CategoryName" });
        }
        public static readonly DeviceCategoryHelper Instance = new DeviceCategoryHelper();
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

            return Instance.CommonGet<DeviceCategory>(args, true).Select(x => new { x.Id, x.CategoryName });
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<DeviceCategory> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<DeviceCategory>(args, true);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("CategoryName", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<DeviceCategory> GetHaveSameCode(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("CategoryName", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonGet<DeviceCategory>(args);
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