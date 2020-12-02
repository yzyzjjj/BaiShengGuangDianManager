using ApiManagement.Base.Server;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.BaseModel
{
    public interface IDataHelper
    {
        T Get<T>(int id) where T : CommonBase;
        IEnumerable<T> GetByIds<T>(IEnumerable<int> ids) where T : CommonBase;
        IEnumerable<T> GetAll<T>() where T : CommonBase;
    }
    public abstract class DataHelper : IDataHelper
    {
        /// <summary>
        /// 表名
        /// </summary>
        protected string Table = "";
        /// <summary>
        /// 添加语句
        /// </summary>
        protected string InsertSql = "";
        /// <summary>
        /// 更新语句
        /// </summary>
        protected string UpdateSql = "";
        //protected string DeleteSql = "";
        #region Get
        /// <summary>
        /// 单个获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get<T>(int id) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{Table}` WHERE `MarkedDelete` = 0 AND Id = @id;", new { id }).FirstOrDefault();
        }
        /// <summary>
        /// 通过id批量获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IEnumerable<T> GetByIds<T>(IEnumerable<int> ids) where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{Table}` WHERE `MarkedDelete` = 0 AND Id IN @ids;", new { ids });
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
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>() where T : CommonBase
        {
            return ServerConfig.ApiDb.Query<T>($"SELECT * FROM `{Table}` WHERE `MarkedDelete` = 0;");
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
        #endregion

    }
}
