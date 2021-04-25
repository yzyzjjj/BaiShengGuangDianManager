using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.DeviceManagementModel
{
    public class UsuallyDictionaryHelper : DataHelper
    {
        private UsuallyDictionaryHelper()
        {
            Table = "usually_dictionary";
            InsertSql =
                "INSERT INTO usually_dictionary (`CreateUserId`, `MarkedDateTime`, `ScriptId`, `VariableNameId`, `VariableTypeId`, `DictionaryId`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @ScriptId, @VariableNameId, @VariableTypeId, @DictionaryId);";
            UpdateSql = "UPDATE usually_dictionary SET `MarkedDateTime` = @MarkedDateTime, `VariableTypeId` = @VariableTypeId, `DictionaryId` = @DictionaryId WHERE `Id` = @Id;";

            SameField = "VariableNameId";
            MenuFields.AddRange(new[] { "Id", "ScriptId", "VariableNameId", "DictionaryId", "VariableTypeId" });
            //MenuQueryFields.AddRange(new[] { "Id", "CategoryId" });
            //SameQueryFields.AddRange(new[] { "CategoryId", "Model" });
            //SameQueryFieldConditions.AddRange(new[] { "=", "IN" });
        }
        public static readonly UsuallyDictionaryHelper Instance = new UsuallyDictionaryHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="sId">脚本</param>
        /// <param name="vId">常用变量类型id</param>
        /// <param name="vType">1 变量 2输入口 3输出口</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int sId = -1, int vId = 0, int vType = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (sId != -1)
            {
                args.Add(new Tuple<string, string, dynamic>("ScriptId", "=", sId));
            }
            if (vId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("VariableNameId", "=", vId));
            }
            if (vType != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("DictionaryId", "=", vType));
            }

            return Instance.CommonGet<UsuallyDictionary>(args, true).Select(x => new { x.Id, x.ScriptId, x.VariableNameId, x.DictionaryId, x.VariableTypeId });
        }
        //public static IEnumerable<UsuallyDictionaryDetail> GetDetail(int id = 0, int cId = 0, int wId = 0)
        //{
        //    return ServerConfig.ApiDb.Query<UsuallyDictionaryDetail>(
        //        $"SELECT a.*, b.`Category` FROM `usually_dictionary` a JOIN `t_device_category` b ON a.CategoryId = b.Id " +
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
        /// <summary>
        /// 获取常用脚本
        /// </summary>
        /// <param name="scriptIds"></param>
        /// <param name="variableNameIds"></param>
        /// <returns></returns>
        public static IEnumerable<UsuallyDictionary> GetUsuallyDictionaries(IEnumerable<int> scriptIds, IEnumerable<int> variableNameIds = null)
        {
            var sIds = new List<int> { 0 };
            sIds.AddRange(scriptIds);
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("ScriptId", "IN", sIds)
            };
            if (variableNameIds != null)
            {
                args.Add(new Tuple<string, string, dynamic>("VariableNameId", "IN", variableNameIds));
            }
            var data = Instance.CommonGet<UsuallyDictionary>(args);
            var tData = new List<UsuallyDictionary>();
            foreach (var scriptId in scriptIds)
            {
                var tmp = data.Where(x => x.ScriptId == 0 || x.ScriptId == scriptId).ToList();
                var tTmp = tmp.OrderByDescending(x => x.ScriptId).GroupBy(y => new { y.ScriptId, y.VariableNameId }).Select(z => z.First());
                tData.AddRange(tTmp.Select(x =>
                {
                    x.ScriptId = scriptId;
                    return x;
                }));
            }
            return tData;
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