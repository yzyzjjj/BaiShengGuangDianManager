using ApiManagement.Base.Server;
using System;
using System.Collections.Generic;
using ApiManagement.Models.BaseModel;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceModelHelper : DataHelper
    {
        private DeviceModelHelper()
        {
            Table = "device_library";
            InsertSql =
                "INSERT INTO device_library (`CreateUserId`, `MarkedDateTime`, `Code`, `DeviceName`, `MacAddress`, `Ip`, `Port`, `Identifier`, `ClassId`, `DeviceModelId`, " +
                "`ScriptId`, `FirmwareId`, `HardwareId`, `ApplicationId`, `SiteId`, `Administrator`, `Remark`, `Icon`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Code, @DeviceName, @MacAddress, @Ip, @Port, @Identifier, @ClassId, @DeviceModelId, " +
                "@ScriptId, @FirmwareId, @HardwareId, @ApplicationId, @SiteId, @Administrator, @Remark, @Icon);";

            UpdateSql = "UPDATE device_library SET `MarkedDateTime` = @MarkedDateTime, `Code` = @Code, `DeviceName` = @DeviceName, `MacAddress` = @MacAddress, " +
                        "`Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                        "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `Administrator` = @Administrator, " +
                        "`Remark` = @Remark, `Icon` = @Icon WHERE `Id` = @Id";

            SameField = "Code";
            MenuFields.AddRange(new[] { "Id", "Code" });
        }
        public static readonly DeviceModelHelper Instance = new DeviceModelHelper();
        #region Get
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
        #endregion

        #region Add
        #endregion

        #region Update
        #endregion

        #region Delete
        #endregion
    }
}