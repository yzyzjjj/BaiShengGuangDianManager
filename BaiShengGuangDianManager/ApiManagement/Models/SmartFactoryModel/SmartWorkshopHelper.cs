using System.Collections.Generic;
using System.Linq;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartWorkshopHelper : DataHelper
    {
        private SmartWorkshopHelper()
        {
            Table = "t_workshop";
            InsertSql =
                "INSERT INTO `t_workshop` (`CreateUserId`, `MarkedDateTime`, `Workshop`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @Workshop, @Remark);";
            UpdateSql = "UPDATE `t_workshop` SET `MarkedDateTime` = @MarkedDateTime, `Workshop` = @Workshop, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Workshop";
            MenuFields.AddRange(new[] { "Id", "Workshop" });
        }
        public static readonly SmartWorkshopHelper Instance = new SmartWorkshopHelper();
        #region Get
        public static IEnumerable<dynamic> GetMenu(int id)
        {
            return Instance.CommonGet<SmartWorkshop>(id, true).Select(x => new { x.Id, x.Workshop });
        }

        #endregion

        #region Add
        #endregion

        #region Update
        public static void UpdateWorkshopSet(IEnumerable<SmartWorkshop> workshops)
        {
            var args = new List<string>
            {
                "MarkedDateTime","Frequency","Unit","Length"
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
