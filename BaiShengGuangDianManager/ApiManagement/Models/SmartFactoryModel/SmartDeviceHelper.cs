using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartDeviceHelper : DataHelper
    {
        private SmartDeviceHelper()
        {
            Table = "t_device";
            InsertSql =
                "INSERT INTO `t_device` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Code`, `CategoryId`, `ModelId`, `Remark`, `Priority`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Code, @CategoryId, @ModelId, @Remark, @Priority);";
            UpdateSql = "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `State` = @State, `Code` = @Code, `CategoryId` = @CategoryId, `ModelId` = @ModelId, `Remark` = @Remark, `Priority` = @Priority WHERE `Id` = @Id;";

            SameField = "Code";
            MenuFields.AddRange(new[] { "Id", "Code" });
            //MenuQueryFields.AddRange(new[] { "Id", "WorkshopId" });
        }
        public static readonly SmartDeviceHelper Instance = new SmartDeviceHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id">设备Id</param>
        /// <param name="wId">车间Id</param>
        /// <param name="cId">设备类型Id</param>
        /// <param name="mId">设备型号Id</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int wId = 0, int cId = 0, int mId = 0)
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
            if (mId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("ModelId", "=", mId));
            }
            return Instance.CommonGet<SmartDevice>(args, true).Select(x => new { x.Id, x.Code });
        }
        public static IEnumerable<SmartDeviceDetail> GetDetails(int id = 0, int wId = 0, int cId = 0, int mId = 0)
        {
            var sql = $"SELECT a.*, b.Category, c.Model FROM t_device a " +
                      $"JOIN t_device_category b ON a.CategoryId = b.Id " +
                      $"JOIN t_device_model c ON a.ModelId = c.Id " +
                      $"WHERE {(id == 0 ? "" : "a.Id = @qId AND ")}" +
                      $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                      $"{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}" +
                      $"{(mId == 0 ? "" : "a.ModelId = @mId AND ")}" +
                      $"a.MarkedDelete = 0 ORDER BY `Code`;";

            return ServerConfig.ApiDb.Query<SmartDeviceDetail>(sql, new { id, wId, cId, mId });
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Code", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<SmartDeviceModelCount> GetDeviceModelCount(IEnumerable<int> modelIds)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelCount>(
                "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelIds AND MarkedDelete = 0 GROUP BY ModelId;",
                new
                {
                    modelIds
                });
        }

        public static IEnumerable<SmartDeviceModelCount> GetNormalDeviceModelCount(IEnumerable<int> modelIds)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceModelCount>(
                "SELECT ModelId, COUNT(1) Count FROM `t_device` WHERE ModelId IN @modelIds AND State = @state AND MarkedDelete = 0 GROUP BY ModelId;",
                new
                {
                    modelIds,
                    state = SmartDeviceState.正常
                });
        }

        /// <summary>
        /// 获取状态正常的设备
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SmartDeviceDetail> GetNormalSmartDevices(int wId)
        {
            return ServerConfig.ApiDb.Query<SmartDeviceDetail>(
                $"SELECT a.*, b.Model FROM `t_device` a " +
                $"JOIN `t_device_model` b ON a.ModelId = b.Id " +
                $"WHERE a.State = @state AND " +
                $"{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}" +
                $"a.MarkedDelete = 0 ;"
                , new { wId, state = SmartDeviceState.正常 });
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateSmartDeviceCategory(IEnumerable<Tuple<int, int>> updates)
        {
            ServerConfig.ApiDb.Execute(
              "UPDATE `t_device` SET `MarkedDateTime` = @MarkedDateTime, `CategoryId` = @CategoryId WHERE `ModelId` = @ModelId;",
              updates.Select(x => new
              {
                  MarkedDateTime = DateTime.Now,
                  ModelId = x.Item1,
                  CategoryId = x.Item2,
              }));
        }
        #endregion

        #region Delete
        #endregion
    }
}