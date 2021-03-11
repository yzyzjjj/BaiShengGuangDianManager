using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static IEnumerable<DataNameDictionaryDetail> GetDataNameDictionaryDetails(IEnumerable<int> scriptIds, IEnumerable<int> variableNameIds = null)
        {
            scriptIds = scriptIds.Distinct();
            var usuallyDictionaries = UsuallyDictionaryHelper.GetUsuallyDictionaries(scriptIds, variableNameIds);
            var args = new List<Tuple<string, string, dynamic>>();
            if (scriptIds != null)
            {
                args.Add(new Tuple<string, string, dynamic>("ScriptId", "IN", scriptIds));
            }
            var dataNameDictionaries = Instance.CommonGet<DataNameDictionaryDetail>(args);
            var res = new List<DataNameDictionaryDetail>();
            var filter = variableNameIds != null && variableNameIds.Any();
            foreach (var scriptId in scriptIds)
            {
                var dnds = dataNameDictionaries.Where(x => x.ScriptId == scriptId);
                var uds = usuallyDictionaries.Where(x => x.ScriptId == scriptId);
                foreach (var dnd in dnds)
                {
                    var ud = uds.FirstOrDefault(x => x.VariableTypeId == dnd.VariableTypeId && x.DictionaryId == dnd.PointerAddress);
                    dnd.VariableNameId = ud?.VariableNameId ?? 0;
                    if (filter)
                    {
                        if (!variableNameIds.Contains(dnd.VariableNameId))
                        {
                            continue;
                        }
                        res.Add(dnd);
                        continue;
                    }
                    res.Add(dnd);
                }
            }
            return res;
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