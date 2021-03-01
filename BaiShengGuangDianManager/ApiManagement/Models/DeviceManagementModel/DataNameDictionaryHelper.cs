using System;
using System.Collections.Generic;
using System.Linq;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class DataNameDictionaryHelper : DataHelper
    {
        private DataNameDictionaryHelper()
        {
            Table = "data_name_dictionary";
            InsertSql =
                "INSERT INTO data_name_dictionary (`CreateUserId`, `MarkedDateTime`, `ScriptId`, `VariableTypeId`, `PointerAddress`, `VariableName`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ScriptId, @VariableTypeId, @PointerAddress, @VariableName, @Remark);";
            UpdateSql = "UPDATE data_name_dictionary SET `MarkedDateTime` = @MarkedDateTime, `Precision` = @Precision WHERE `Id` = @Id;";

            SameField = "Model";
            MenuFields.AddRange(new[] { "Id", "Model", "CategoryId" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly DataNameDictionaryHelper Instance = new DataNameDictionaryHelper();
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

        //    return Instance.CommonGet<DataNameDictionary>(args, true).Select(x => new { x.Id, x.Model, x.CategoryId });
        //}
        //public static IEnumerable<DataNameDictionaryDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        //{
        //    return ServerConfig.ApiDb.Query<DataNameDictionaryDetail>(
        //        $"SELECT a.*, b.`Category` FROM `data_name_dictionary` a JOIN `t_device_category` b ON a.CategoryId = b.Id " +
        //        $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(cId == 0 ? "" : "a.CategoryId = @cId AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.CategoryId;",
        //        new { id, cId, wId });
        //}
        //public static bool GetHaveSame(int cId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        //{
        //    var args = new List<Tuple<string, string, dynamic>>
        //    {
        //        new Tuple<string, string, dynamic>("CategoryId", "=", cId),
        //        new Tuple<string, string, dynamic>("Model", "IN", sames)
        //    };
        //    if (ids != null)
        //    {
        //        args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
        //    }
        //    return Instance.CommonHaveSame(args);
        //}
        public static IEnumerable<DataNameDictionaryDetail> GetDataNameDictionaryDetails(IEnumerable<int> scriptIds)
        {
            return ServerConfig.ApiDb.Query<DataNameDictionaryDetail>("SELECT a.*, IFNULL(b.VariableNameId, 0) VariableNameId FROM `data_name_dictionary` a " +
                                                                      "LEFT JOIN usually_dictionary b ON a.ScriptId = b.ScriptId AND a.VariableTypeId = b.VariableTypeId AND a.PointerAddress = b.DictionaryId " +
                                                                      "WHERE a.ScriptId IN @scriptIds AND a.MarkedDelete = 0;", new { scriptIds });
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