using ApiManagement.Models.BaseModel;
using ModelBase.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.FlowCardManagementModel
{
    public class RawMateriaHelper : DataHelper
    {
        private RawMateriaHelper()
        {
            Table = "raw_materia";
            InsertSql =
                "INSERT INTO raw_materia (`CreateUserId`, `MarkedDateTime`, `RawMateriaName`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @RawMateriaName);";
            UpdateSql =
                "UPDATE raw_materia SET `MarkedDateTime` = @MarkedDateTime, `RawMateriaName` = @RawMateriaName WHERE `Id` = @Id;";

            SameField = "RawMateriaName";
            MenuFields.AddRange(new[] { "Id", "MarkedDateTime", "RawMateriaName" });
        }
        public static readonly RawMateriaHelper Instance = new RawMateriaHelper();
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

            return Instance.CommonGet<RawMateria>(args, true).Select(x => new { x.Id, x.MarkedDateTime, x.RawMateriaName }).OrderByDescending(x => x.Id);
        }
        public static IEnumerable<RawMateria> GetDetail(int id = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }
            if (startTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("MarkedDateTime", ">=", startTime.DayBeginTime()));
            }
            if (endTime != default(DateTime))
            {
                args.Add(new Tuple<string, string, dynamic>("MarkedDateTime", "<=", endTime.DayEndTime()));
            }
            return Instance.CommonGet<RawMateria>(args).OrderByDescending(x => x.MarkedDateTime).ThenByDescending(x => x.Id);
        }
        public static bool GetHaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("RawMateriaName", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
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
