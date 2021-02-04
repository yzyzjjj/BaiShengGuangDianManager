using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.SmartFactoryModel
{
    public class SmartProductHelper : DataHelper
    {
        private SmartProductHelper()
        {
            Table = "t_product";
            InsertSql =
                "INSERT INTO `t_product` (`CreateUserId`, `MarkedDateTime`, `WorkshopId`, `Product`, `CategoryId`, `CapacityId`, `Remark`) " +
                "VALUES (@CreateUserId, @MarkedDateTime, @WorkshopId, @Product, @CategoryId, @CapacityId, @Remark);";
            UpdateSql =
                "UPDATE `t_product` SET `MarkedDateTime` = @MarkedDateTime, `Product` = @Product, `CategoryId` = @CategoryId, `CapacityId` = @CapacityId, `Remark` = @Remark WHERE `Id` = @Id;";

            SameField = "Product";
            MenuFields.AddRange(new[] { "Id", "Product" });
        }
        public static readonly SmartProductHelper Instance = new SmartProductHelper();
        #region Get
        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wId"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetMenu(int id = 0, int wId = 0)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (id != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "=", id));
            }

            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }

            return Instance.CommonGet<SmartProduct>(args, true).Select(x => new { x.Id, x.Product });
        }
        public static IEnumerable<SmartProductDetail> GetDetail(int id = 0, int wId = 0)
        {
            var sql = $"SELECT a.*, IFNULL(ProcessCodeIds, '') ProcessCodeIds, IFNULL(Category, '') Category, IFNULL(Capacity, '') Capacity FROM `t_product` a " +
                      $"LEFT JOIN (SELECT ProductId, GROUP_CONCAT(DISTINCT ProcessCodeId) ProcessCodeIds FROM `t_product_process` WHERE MarkedDelete = 0 GROUP BY ProductId ORDER BY ProcessCodeId) b ON a.Id = b.ProductId " +
                      $"LEFT JOIN t_process_code_category c ON a.CategoryId = c.Id " +
                      $"LEFT JOIN t_capacity d ON a.CapacityId = d.Id " +
                      $"WHERE {(id == 0 ? "" : "a.Id = @id AND ")}{(wId == 0 ? "" : "a.WorkshopId = @wId AND ")}a.MarkedDelete = 0 ORDER BY a.Id Desc;";
            return ServerConfig.ApiDb.Query<SmartProductDetail>(sql, new { id, wId });
        }
        public static IEnumerable<SmartProduct> GetDetail(int wId = 0, IEnumerable<string> products = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            if (products != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Product", "IN", products));
            }
            return Instance.CommonGet<SmartProduct>(args);
        }
        public static bool GetHaveSame(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>
            {
                new Tuple<string, string, dynamic>("WorkshopId", "=", wId),
                new Tuple<string, string, dynamic>("Product", "IN", sames)
            };
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonHaveSame(args);
        }
        public static IEnumerable<string> CommonGetSames(int wId, IEnumerable<string> sames, IEnumerable<int> ids = null)
        {
            var args = new List<Tuple<string, string, dynamic>>();
            if (wId != 0)
            {
                args.Add(new Tuple<string, string, dynamic>("WorkshopId", "=", wId));
            }
            args.Add(new Tuple<string, string, dynamic>("Product", "IN", sames));
            if (ids != null)
            {
                args.Add(new Tuple<string, string, dynamic>("Id", "NOT IN", ids));
            }
            return Instance.CommonGetSames(args);
        }

        public static IEnumerable<SmartProduct> GetSameSmartProducts(IEnumerable<string> products, IEnumerable<int> productIds)
        {
            return ServerConfig.ApiDb.Query<SmartProduct>(
                "SELECT Id, Product FROM `t_product` WHERE MarkedDelete = 0 AND Product IN @products AND Id NOT IN @productIds;"
                , new { products, productIds });
        }

        public static IEnumerable<SmartProduct> GetSmartProductsByProducts(IEnumerable<string> products)
        {
            return ServerConfig.ApiDb.Query<SmartProduct>(
                "SELECT * FROM `t_product` WHERE MarkedDelete = 0 AND Product IN @products;", new { products });
        }
        public static IEnumerable<SmartProduct> GetSmartProductsByCapacityIds(IEnumerable<int> capacityIds)
        {
            return ServerConfig.ApiDb.Query<SmartProduct>(
                "SELECT * FROM `t_product` WHERE MarkedDelete = 0 AND CapacityId IN @capacityIds;", new { capacityIds });
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
