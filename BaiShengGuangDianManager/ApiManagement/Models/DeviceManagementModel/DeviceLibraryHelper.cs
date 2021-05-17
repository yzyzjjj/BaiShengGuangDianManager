using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceLibraryHelper : DataHelper
    {
        private DeviceLibraryHelper()
        {
            Table = "device_library";
            InsertSql =
                "INSERT INTO device_library (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Code`, `DeviceName`, `MacAddress`, `Ip`, `Port`, `Identifier`, `ClassId`, `DeviceModelId`, " +
                "`ScriptId`, `FirmwareId`, `HardwareId`, `ApplicationId`, `SiteId`, `Administrator`, `Remark`, `Icon`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Code, @DeviceName, @MacAddress, @Ip, @Port, @Identifier, @ClassId, @DeviceModelId, " +
                "@ScriptId, @FirmwareId, @HardwareId, @ApplicationId, @SiteId, @Administrator, @Remark, @Icon);";

            UpdateSql = "UPDATE device_library SET `MarkedDateTime` = @MarkedDateTime, `WorkshopId` = @WorkshopId, `Code` = @Code, `DeviceName` = @DeviceName, `MacAddress` = @MacAddress, " +
                        "`Ip` = @Ip, `Port` = @Port, `Identifier` = @Identifier, `DeviceModelId` = @DeviceModelId, `ScriptId` = @ScriptId, " +
                        "`FirmwareId` = @FirmwareId, `HardwareId` = @HardwareId, `ApplicationId` = @ApplicationId, `SiteId` = @SiteId, `Administrator` = @Administrator, " +
                        "`Remark` = @Remark, `Icon` = @Icon WHERE `Id` = @Id";

            SameField = "Code";
            MenuFields.AddRange(new[] { "Id", "Code", "ClassId", "DeviceModelId", "ScriptId", "FirmwareId", "HardwareId", "ApplicationId", "SiteId" });
        }
        public static readonly DeviceLibraryHelper Instance = new DeviceLibraryHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenus(int wId = 1, int id = 0, int cId = 0, bool script = false)
        {
            if (script)
            {
                var sql = "SELECT a.*, b.ScriptName FROM device_library a JOIN script_version b ON a.ScriptId = b.Id " +
                          "Where " +
                           $"{(id == 0 ? "" : "a.Id = @id AND ")}" +
                           $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                           $"{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}" +
                          "a.`MarkedDelete` = 0";
                return ServerConfig.ApiDb.Query<DeviceLibraryDetail>(sql, new { id, wId, cId }).Select(x => new { x.Id, x.Code, x.ScriptId, x.ScriptName });
            }
            else
            {
                var args = new List<Tuple<string, string, dynamic>>();
                if (id != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
                }
                if (wId != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
                }
                if (cId != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("CategoryId", "=", cId));
                }

                return Instance.CommonGet<DeviceLibrary>(args, true).Select(x => new { x.Id, x.ClassId, x.DeviceModelId, x.ScriptId, x.FirmwareId, x.HardwareId, x.ApplicationId, x.SiteId });
            }
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenus(int wId = 1, IEnumerable<int> ids = null, int cId = 0, bool script = false)
        {
            if (script)
            {
                var sql = "SELECT a.*, b.ScriptName FROM device_library a JOIN script_version b ON a.ScriptId = b.Id " +
                          "Where " +
                          $"{(ids != null && ids.Any() ? "a.Id IN @ids AND " : "")}" +
                          $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                          $"{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}" +
                          "a.`MarkedDelete` = 0";
                return ServerConfig.ApiDb.Query<DeviceLibraryDetail>(sql, new { ids, wId, cId }).Select(x => new { x.Id, x.Code, x.ScriptId, x.ScriptName });
            }
            else
            {
                var args = new List<Tuple<string, string, dynamic>>();
                if (ids != null && ids.Any())
                {
                    args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
                }
                if (wId != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
                }
                if (cId != 0)
                {
                    args.Add(new Tuple<string, string, dynamic>("CategoryId", "=", cId));
                }

                return Instance.CommonGet<DeviceLibrary>(args, true).Select(x => new { x.Id, x.ClassId, x.DeviceModelId, x.ScriptId, x.FirmwareId, x.HardwareId, x.ApplicationId, x.SiteId });
            }
        }
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="ids"></param>
        public static IEnumerable<DeviceLibrary> GetMenus(IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (ids == null || !ids.Any())
            {
                return new List<DeviceLibrary>();
            }
            args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            return Instance.CommonGet<DeviceLibrary>(args, true);
        }
        public static DeviceLibrary GetDetail(int wId, string code)
        {
            return ServerConfig.ApiDb.Query<DeviceLibrary>(
                $"SELECT * FROM `device_library` WHERE " +
                $"{(wId == 0 ? "" : "WorkshopId = @wId AND ")}" +
                $"{(code.IsNullOrEmpty() ? "" : "Code = @code AND ")}" +
                $"MarkedDelete = 0;", new { wId, code }).FirstOrDefault();
        }
        public static IEnumerable<DeviceLibrary> GetDetails(int wId, IEnumerable<string> codes = null)
        {
            return ServerConfig.ApiDb.Query<DeviceLibrary>(
                $"SELECT * FROM `device_library` WHERE " +
                $"{(wId == 0 ? "" : "WorkshopId = @wId AND ")}" +
                $"{(codes != null ? "" : "Code IN @codes AND ")}" +
                $"MarkedDelete = 0;", new { wId, codes });
        }
        public static int GetCountByClass(int classId)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ClassId", "=", classId)
            };
            return Instance.CommonGetCount(args);
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
        public static IEnumerable<DeviceLibrary> GetHaveSameCode(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("Code", "IN", sames)
            };
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonGet<DeviceLibrary>(args);
        }
        public static IEnumerable<DeviceLibrary> GetHaveSameIpPort(int wId, IEnumerable<string> ips, IEnumerable<int> ports, IEnumerable<int> ids = null)
        {
            return ServerConfig.ApiDb.Query<DeviceLibrary>("SELECT Id, `Code`, `Ip`, `Port` FROM `device_library` " +
                                                           "WHERE Ip IN @ips AND Port IN @ports AND Id NOT IN @ids AND `MarkedDelete` = 0;",
                new { ips, ports, ids });
        }
        public static DeviceLibraryDetail GetDetail(int id)
        {
            return ServerConfig.ApiDb.Query<DeviceLibraryDetail>(
                "SELECT a.Id, b.DeviceCategoryId FROM `device_library` a JOIN device_model b ON a.DeviceModelId = b.Id WHERE a.`Id` = @id;", new { id }).FirstOrDefault();
        }

        public static IEnumerable<DeviceLibraryDetail> GetDetails(IEnumerable<int> ids)
        {
            return ServerConfig.ApiDb.Query<DeviceLibraryDetail>(
                "SELECT a.Id, b.DeviceCategoryId FROM `device_library` a JOIN device_model b ON a.DeviceModelId = b.Id WHERE a.`Id` = @ids;", new { ids });
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