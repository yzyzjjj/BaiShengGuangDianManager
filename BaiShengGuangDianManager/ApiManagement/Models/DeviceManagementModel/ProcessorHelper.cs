using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class ProcessorHelper : DataHelper
    {
        //private ProcessorHelper()
        //{
        //    Table = "processor";

        //    InsertSql =
        //        "INSERT INTO `processor` (`CreateUserId`, `MarkedDateTime`, `Name`, `IsShow`, `Type`, `DeviceIds`, `Order`, `UI`, `Second`, `Row`, `Col`, `Variables`, `Items`) " +
        //        "VALUES (@CreateUserId, @MarkedDateTime, @Name, @IsShow, @Type, @DeviceIds, @Order, @UI, @Second, @Row, @Col, @Variables, @Items);";
        //    UpdateSql =
        //        "UPDATE `processor` SET `MarkedDateTime` = @MarkedDateTime, `Name` = @Name, `IsShow` = @IsShow, " +
        //            "`DeviceIds` = @DeviceIds, `Order` = @Order, `UI` = @UI, `Second` = @Second, `Row` = @Row, `Row` = @Row, `Col` = @Col, `Variables` = @Variables, `Items` = @Items WHERE `Id` = @Id;";

        //    SameField = "Name";
        //    MenuFields.AddRange(new[] { "Id", "Name", "Type" });
        //}
        //public static readonly ProcessorHelper Instance = new ProcessorHelper();
        #region Get
        ///// <summary>
        ///// 菜单
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //public static IEnumerable<dynamic> GetMenu(int id = 0)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>();
        //    if (id != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
        //    }

        //    return Instance.CommonGet<Processor>(args, true).Select(x => new { x.Id, x.Name });
        //}
        //public static IEnumerable<Processor> GetDetail(int id = 0)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>();
        //    if (id != 0)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
        //    }

        //    return Instance.CommonGet<Processor>(args).OrderBy(x => x.Order);
        //}
        //public static bool GetHaveSame(int type, IEnumerable<string> sames, IEnumerable<int> ids = null)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>
        //    {
        //        new Tuple<string, string, dynamic>("Type", "=", type),
        //        new Tuple<string, string, dynamic>("Name", "IN", sames)
        //    };
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
