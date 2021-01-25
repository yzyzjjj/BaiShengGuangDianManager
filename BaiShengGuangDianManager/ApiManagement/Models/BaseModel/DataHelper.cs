using ApiManagement.Base.Server;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.BaseModel
{
    public interface IDataHelper
    {
        T Get<T>(int id, bool simple = false) where T : CommonBase;
        IEnumerable<T> GetByIds<T>(IEnumerable<int> ids, bool simple = false) where T : CommonBase;
        IEnumerable<T> GetAll<T>(bool simple = false) where T : CommonBase;
        IEnumerable<T> CommonGet<T>(int id = 0, bool simple = false) where T : CommonBase;
        IEnumerable<string> GetSames(IEnumerable<string> sames, IEnumerable<int> ids = null);
        int GetSameCount(IEnumerable<string> sames, IEnumerable<int> ids = null);
        bool HaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null);
    }
    public abstract class DataHelper : IDataHelper
    {
        /// <summary>
        /// 表名
        /// </summary>
        protected string Table = "";
        /// <summary>
        /// 判重字段名
        /// </summary>
        protected string SameField = "";
        /// <summary>
        /// 菜单字段
        /// </summary>
        protected List<string> MenuFields = new List<string>();
        /// <summary>
        /// 菜单查询字段
        /// </summary>
        protected List<string> MenuQueryFields = new List<string>();
        /// <summary>
        /// 添加语句
        /// </summary>
        protected string InsertSql = "";
        /// <summary>
        /// 更新语句
        /// </summary>
        protected string UpdateSql = "";
        /// <summary>
        /// 根据父级删除
        /// </summary>
        protected string ParentField = "";
        //protected string DeleteSql = "";
        #region Get
        /// <summary>
        /// 获取(通用接口)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <param name="menu">菜单</param>
        /// <returns></returns>
        public IEnumerable<T> CommonGet<T>(string[] args, bool menu = false) where T : CommonBase
        {
            var param = new List<string>();
            foreach (var arg in args)
            {
                //param.Add($"{}");
            }


            return ServerConfig.ApiDb.Query<T>($"SELECT {(menu && MenuFields.Any() ? MenuFields.Join() : "*")} FROM `{Table}` WHERE `MarkedDelete` = 0{(id == 0 ? "" : " AND Id = @id")};", new { id });
        }
        /// <summary>
        /// 获取(通用接口)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="menu">菜单</param>
        /// <returns></returns>
        public IEnumerable<T> CommonGet<T>(int id = 0, bool menu = false) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT {(menu && MenuFields.Any() ? MenuFields.Join() : "*")} FROM `{Table}` WHERE `MarkedDelete` = 0{(id == 0 ? "" : " AND Id = @id")};", new { id });
        }
        /// <summary>
        /// 单个获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public T Get<T>(int id, bool menu = false) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT {(menu && MenuFields.Any() ? MenuFields.Join() : "*")} FROM `{Table}` WHERE `MarkedDelete` = 0 AND Id = @id;", new { id }).FirstOrDefault();
        }
        /// <summary>
        /// 通过id批量获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public IEnumerable<T> GetByIds<T>(IEnumerable<int> ids, bool menu = false) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT {(menu && MenuFields.Any() ? MenuFields.Join() : "*")} FROM `{Table}` WHERE `MarkedDelete` = 0 AND Id IN @ids;", new { ids });
        }
        /// <summary>
        /// 通过id批量获取数量
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public int GetCountByIds(IEnumerable<int> ids)
        {
            return ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `{Table}` WHERE `MarkedDelete` = 0 AND Id IN @ids;", new { ids }).FirstOrDefault();
        }
        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="menu"></param>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>(bool menu = false) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT {(menu && MenuFields.Any() ? MenuFields.Join() : "*")} FROM `{Table}` WHERE `MarkedDelete` = 0;");
        }
        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAllData<T>() where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{Table}`;");
        }
        /// <summary>
        /// 获取总数量
        /// </summary>
        /// <returns></returns>
        public int GetCountAll()
        {
            return ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `{Table}` WHERE `MarkedDelete` = 0;").FirstOrDefault();
        }

        /// <summary>
        /// 获取指定字段重复数据
        /// </summary>
        /// <param name="sames"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IEnumerable<string> GetSames(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            return ServerConfig.ApiDb.Query<string>($"SELECT `{SameField}` FROM `{Table}` WHERE `MarkedDelete` = 0 AND `{SameField}` IN @sames{(ids != null ? " AND `Id` NOT IN @ids" : "")};", new { sames, ids });
        }
        /// <summary>
        /// 获取指定字段重复数据数量（只判断使用HaveSame）
        /// </summary>
        /// <param name="sames"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public int GetSameCount(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            return ServerConfig.ApiDb.Query<int>($"SELECT COUNT(1) FROM `{Table}` WHERE `MarkedDelete` = 0 AND `{SameField}` IN @sames{(ids != null ? " AND `Id` NOT IN @ids" : "")};", new { sames, ids }).FirstOrDefault();
        }

        /// <summary>
        /// 指定字段是否有重复数据
        /// </summary>
        /// <param name="sames"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public bool HaveSame(IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            if (SameField.IsNullOrEmpty())
            {
                throw new Exception("DataHelper SameField IsNullOrEmpty!!!");
            }

            return GetSameCount(sames, ids) > 0;
        }

        #endregion

        #region Add
        /// <summary>
        /// 单个添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public void Add<T>(T t) where T : CommonBase
        {
            ServerConfig.ApiDb.Execute(InsertSql, t);
        }
        /// <summary>
        /// 批量添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public void Add<T>(IEnumerable<T> t) where T : CommonBase
        {
            ServerConfig.ApiDb.Execute(InsertSql, t);
        }

        #endregion

        #region Update
        /// <summary>
        /// 单个更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public void Update<T>(T t) where T : CommonBase
        {
            ServerConfig.ApiDb.Execute(UpdateSql, t);
        }
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public void Update<T>(IEnumerable<T> t) where T : CommonBase
        {
            ServerConfig.ApiDb.Execute(UpdateSql, t);
        }
        #endregion

        #region Delete
        /// <summary>
        /// 单个删除
        /// </summary>
        /// <param name="id"></param>
        public void Delete(int id)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `Id`= @id;", new { id });
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="ids"></param>
        public void Delete(IEnumerable<int> ids)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `Id`IN @ids;", new { ids });
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="id"></param>
        public void DeleteFromParent(int id)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `{ParentField}` = @id;", new { id });
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="ids"></param>
        public void DeleteFromParent(IEnumerable<int> ids)
        {
            ServerConfig.ApiDb.Execute($"UPDATE `{Table}` SET `MarkedDateTime`= NOW(), `MarkedDelete`= true WHERE `{ParentField}` IN @ids;", new { ids });
        }
        #endregion

    }
}
