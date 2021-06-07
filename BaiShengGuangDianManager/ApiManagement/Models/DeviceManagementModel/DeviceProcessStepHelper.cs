using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DeviceProcessStepHelper : DataHelper
    {
        private DeviceProcessStepHelper()
        {
            Table = "device_process_step";
            InsertSql =
                "INSERT INTO `device_process_step` (`CreateUserId`, `MarkedDateTime`, `StepName`, `Abbrev`, `DeviceCategoryId`, `Description`, `IsSurvey`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @StepName, @Abbrev, @DeviceCategoryId, @Description, @IsSurvey);" +
                "INSERT INTO `device_process_step_other` (`Id`, `IsQualified`, `From`, `StepTypeStr`, `StepType`, `Errors`) " +
                "VALUES (LAST_INSERT_ID(), @IsQualified, @From, @StepTypeStr, @StepType, @Errors);";

            UpdateSql = "UPDATE `device_process_step` SET `MarkedDateTime` = @MarkedDateTime, `StepName` = @StepName, `Abbrev` = @Abbrev, " +
                        "`DeviceCategoryId` = @DeviceCategoryId, `Description` = @Description, `IsSurvey` = @IsSurvey WHERE `Id` = @Id; ; ";

            SameField = "StepName";
            MenuFields.AddRange(new[] { "Id", "StepName", "DeviceCategoryId" });
        }
        public static readonly DeviceProcessStepHelper Instance = new DeviceProcessStepHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        public static IEnumerable<dynamic> GetMenu(int wId, int id)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            return Instance.CommonGet<DeviceProcessStep>(args, true).Select(x => new { x.Id, x.DeviceCategoryId, x.StepName }).OrderByDescending(x => x.Id);
        }
        /// <summary>
        /// 菜单
        /// </summary>
        public static IEnumerable<DeviceProcessStep> GetMenu(int wId, IEnumerable<int> ids)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }

            if (ids != null && ids.Any())
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "IN", ids));
            }

            return Instance.CommonGet<DeviceProcessStep>(args, true).OrderByDescending(x => x.Id);
        }

        public static IEnumerable<DeviceProcessStepDetail> GetDetails(int wId, int id = 0)
        {
            return ServerConfig.ApiDb.Query<DeviceProcessStepDetail>(
                $"SELECT b.*, a.*, IFNULL(c.CategoryName, '') CategoryName FROM `device_process_step` a " +
                $"JOIN `device_process_step_other` b ON a.Id = b.Id " +
                $"LEFT JOIN `device_category` c ON a.DeviceCategoryId = c.Id WHERE {(id == 0 ? "" : " a.WorkshopId = @wId AND ")} {(id == 0 ? "" : " a.Id = @id AND ")} a.MarkedDelete = 0;", new { wId, id });
        }
        public static IEnumerable<DeviceProcessStepDetail> GetDetailsFrom(DataFrom from)
        {
            return ServerConfig.ApiDb.Query<DeviceProcessStepDetail>(
                $"SELECT b.*, a.*, IFNULL(c.CategoryName, '') CategoryName FROM `device_process_step` a " +
                $"JOIN `device_process_step_other` b ON a.Id = b.Id " +
                $"LEFT JOIN `device_category` c ON a.DeviceCategoryId = c.Id WHERE b.From = @from AND a.MarkedDelete = 0;", new { from });
        }
        public static IEnumerable<DeviceProcessStepDetail> GetDetails(int wId, IEnumerable<int> steps)
        {
            return ServerConfig.ApiDb.Query<DeviceProcessStepDetail>($"SELECT * FROM `device_process_step` " +
                                                                     $"WHERE " +
                                                                     $"{(wId == 0 ? "" : " WorkshopId = @wId AND ")}" +
                                                                     $"{(steps == null || !steps.Any() ? "" : " Id IN @steps AND ")}MarkedDelete = 0;", new { wId, steps });
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


        public static IEnumerable<DeviceProcessStepDetail> GetStepFromId(IEnumerable<int> ids = null)
        {
            //var res = new List<DeviceProcessStepDetail>();
            //foreach (var id in ids)
            //{
            //    var r = ServerConfig.ApiDb.Query<DeviceProcessStepDetail>("SELECT OtherId FromId, Step Id, StepAbbrev Abbrev, StepName FROM `flowcard_report_get` WHERE Step = @id ORDER BY OtherId + 0 DESC LIMIT 1;", new{ id }, 1000).FirstOrDefault();
            //    if (r != null)
            //    {
            //        res.Add(r);
            //    }
            //}

            //return res;
            return
                ServerConfig.ApiDb.Query<DeviceProcessStepDetail>(
                    $"SELECT MAX(OtherId) FromId, Step Id, StepAbbrev Abbrev, StepName FROM `flowcard_report_get` {(ids != null && ids.Any() ? "WHERE Step IN @ids" : "")} GROUP BY Step + 0;",
                    new { ids }, 1000);
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void Update(IEnumerable<DeviceProcessStepDetail> steps)
        {
            ServerConfig.ApiDb.Execute("UPDATE `device_process_step_other` SET `IsQualified` = @IsQualified, `From` = @From, `StepTypeStr` = @StepTypeStr" +
                                       ", `StepType` = @StepType, `Errors` = @Errors WHERE `Id` = @Id;", steps);
        }
        public static void UpdateFromId(IEnumerable<DeviceProcessStepDetail> steps)
        {
            ServerConfig.ApiDb.Execute("UPDATE `device_process_step_other` SET `FromId` = @FromId, `Api` = @Api WHERE `Id` = @Id;", steps);
        }
        #endregion

        #region Delete
        #endregion
    }
}