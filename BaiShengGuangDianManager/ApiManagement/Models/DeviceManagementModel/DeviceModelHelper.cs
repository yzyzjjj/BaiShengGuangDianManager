using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceModelHelper : DataHelper
    {
        private DeviceModelHelper()
        {
            Table = "device_model";
            InsertSql =
                "INSERT INTO `device_model` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `DeviceCategoryId`, `ModelName`, `Description`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @DeviceCategoryId, @ModelName, @Description);";

            UpdateSql = "UPDATE `device_model` SET `MarkedDateTime` = @MarkedDateTime, `WorkshopId` = @WorkshopId, " +
                        "`DeviceCategoryId` = @DeviceCategoryId, `ModelName` = @ModelName, `Description` = @Description WHERE `Id` = @Id;";

            SameField = "ModelName";
            MenuFields.AddRange(new[] { "Id", "DeviceCategoryId", "ModelName" });
        }
        public static readonly DeviceModelHelper Instance = new DeviceModelHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            return Instance.CommonGet<DeviceModel>(args, true).Select(x => new { x.Id, x.DeviceCategoryId, x.ModelName }).OrderByDescending(x => x.Id);
        }
        ///// <summary>
        ///// 菜单
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="cId"></param>
        ///// <param name="wId"></param>
        ///// <returns></returns>
        //public static IEnumerable<dynamic> GetMenu(int id = 0, int cId = 0, int wId = 0)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>();
        //    if (id != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
        //    }
        //    if (cId != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("CategoryId", "=", cId));
        //    }
        //    if (wId != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
        //    }

        //    return Instance.CommonGet<DeviceModel>(args, true).Select(x => new { x.Id, x.Model, x.CategoryId });
        //}
        public static IEnumerable<DeviceModel> GetDetail(int wId = 0, IEnumerable<string> codes = null)
        {
            return ServerConfig.ApiDb.Query<DeviceModel>(
                $"SELECT * FROM `device_library` WHERE " +
                //$"{(wId == 0 ? "" : "a.Id = @id AND ")}" +
                $"{(codes != null ? "" : "Code = @codes AND ")}MarkedDelete = 0;", new { wId, codes });
        }
        //public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>
        //    {
        //        new Tuple<string, string, dynamic>("Code", "IN", sames)
        //    };
        //    if (wId != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("wId", "=", wId));
        //    }
        //    if (ids != null)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
        //    }
        //    return Instance.CommonHaveSame(args);
        //}

        /// <summary>
        /// 获取设备使用次数
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static int GetUseCount(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("DeviceModelId", "IN", ids)
            };

            return DeviceLibraryHelper.Instance.CommonGetCount(args);
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