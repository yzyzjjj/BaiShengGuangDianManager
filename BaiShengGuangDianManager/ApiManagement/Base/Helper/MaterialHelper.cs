using System.Collections.Generic;
using ApiManagement.Base.Server;
using ApiManagement.Models.MaterialManagementModel;

namespace ApiManagement.Base.Helper
{
    public class MaterialHelper
    {
        public static void InsertLog(MaterialLog materialLog)
        {
            ServerConfig.ApiDb.Execute(
                "INSERT INTO material_log (`Time`, `BillId`, `Code`, `Type`, `Purpose`, `PlanId`, `Number`, `RelatedPerson`, `Manager`) " +
                "VALUES (@Time, @BillId, @Code, @Type, @Purpose, @PlanId, @Number, @RelatedPerson, @Manager);",
                materialLog);
        }

        public static void InsertLog(IEnumerable<MaterialLog> materialLogs)
        {
            ServerConfig.ApiDb.Execute(
                "INSERT INTO material_log (`Time`, `BillId`, `Code`, `Type`, `Purpose`, `PlanId`, `Number`, `RelatedPerson`, `Manager`) " +
                "VALUES (@Time, @BillId, @Code, @Type, @Purpose, @PlanId, @Number, @RelatedPerson, @Manager);",
                materialLogs);
        }
    }
}
