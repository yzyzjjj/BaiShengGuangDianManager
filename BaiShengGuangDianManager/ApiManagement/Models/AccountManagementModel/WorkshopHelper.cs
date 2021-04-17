using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.AccountManagementModel
{
    public class WorkshopHelper : DataHelper
    {
        private WorkshopHelper()
        {
            Table = "workshop";
            InsertSql =
                "INSERT INTO `workshop` (`CreateUserId`, `MarkedDateTime`, `Name`, `Abbrev`, `Shifts`, `ShiftTimes`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Name, @Abbrev, @Shifts, @ShiftTimes);";
            UpdateSql = "UPDATE `workshop` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `Abbrev` = @Abbrev, `Shifts` = @Shifts, " +
                        "`ShiftTimes` = @ShiftTimes, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Name";
            MenuFields.AddRange(new[] { "Id", "Name" });
        }
        public static readonly WorkshopHelper Instance = new WorkshopHelper();
        #region Get
        public static IEnumerable<dynamic> GetMenu(int id = 0)
        {
            return Instance.CommonGet<Workshop>(id, true).Select(x => new { x.Id, x.Name }).OrderByDescending(x => x.Id);
        }

        public static IEnumerable<Workshop> GetDetails(int id = 0)
        {
            return Instance.CommonGet<Workshop>(id).OrderByDescending(x => x.Id);
        }
        public static Workshop GetDetail(int id)
        {
            return Instance.Get<Workshop>(id);
        }
        public static Workshop GetDetail(string name)
        {
            var args = new List<Tuple<string, string, dynamic>> { new Tuple<string, string, dynamic>("Name", "=", name) };
            return Instance.CommonGet<Workshop>(args).FirstOrDefault();
        }
        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateWorkshopSet(IEnumerable<Workshop> workshops)
        {
            var args = new List<string>
            {
                "MarkedDateTime", "Abbrev", "Shifts", "ShiftTimes"
            };
            var cons = new List<string>
            {
                "Id"
            };
            Instance.CommonUpdate(args, cons, workshops);
        }
        #endregion

        #region Delete
        #endregion
    }
}
