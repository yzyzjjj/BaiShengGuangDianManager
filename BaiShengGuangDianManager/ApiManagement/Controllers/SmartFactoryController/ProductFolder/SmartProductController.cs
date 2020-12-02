using ApiManagement.Base.Helper;
using ApiManagement.Base.Server;
using ApiManagement.Models.BaseModel;
using ApiManagement.Models.SmartFactoryModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Controllers.SmartFactoryController.ProductFolder
{
    /// <summary>
    /// 
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class SmartProductController : ControllerBase
    {
        // GET: api/SmartProduct
        [HttpGet]
        public DataResult GetSmartProduct([FromQuery]int qId, bool menu)
        {
            var result = new DataResult();
            var sql = $"SELECT a.*, IFNULL(ProcessCodeIds, '') ProcessCodeIds, IFNULL(Category, '') Category, IFNULL(Capacity, '') Capacity FROM `t_product` a " +
                      $"LEFT JOIN (SELECT ProductId, GROUP_CONCAT(DISTINCT ProcessCodeId) ProcessCodeIds FROM `t_product_process` WHERE MarkedDelete = 0 GROUP BY ProductId ORDER BY ProcessCodeId) b ON a.Id = b.ProductId " +
                      $"LEFT JOIN t_process_code_category c ON a.CategoryId = c.Id " +
                      $"LEFT JOIN t_capacity d ON a.CapacityId = d.Id " +
                      $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")} ORDER BY a.Id Desc;";
            var data = ServerConfig.ApiDb.Query<SmartProductDetail>(sql, new { qId });
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new { x.Id, x.Product }));
            }
            else
            {
                var processCodeIds = data.SelectMany(x => x.ProcessCodeIdsList).Distinct();
                if (processCodeIds.Any())
                {
                    var processCodeIdsList = ServerConfig.ApiDb.Query<SmartProcessCode>(
                        "SELECT * FROM `t_process_code` WHERE Id IN @processCodeIds", new
                        {
                            processCodeIds
                        });
                    if (processCodeIdsList.Any())
                    {
                        foreach (var d in data)
                        {
                            foreach (var processCodeId in d.ProcessCodeIdsList)
                            {
                                var processCode = processCodeIdsList.FirstOrDefault(x => x.Id == processCodeId);
                                if (processCode != null)
                                {
                                    d.ProcessCodesList.Add(processCode.Code);
                                }
                            }
                        }
                    }
                }

                if (qId != 0)
                {
                    var smartProduct = data.FirstOrDefault();
                    if (smartProduct != null)
                    {
                        if (processCodeIds.Any())
                        {
                            smartProduct.ProductProcesses.AddRange(ServerConfig.ApiDb.Query<SmartProductProcessDetail>(
                            "SELECT a.*, b.Process FROM `t_product_process` a " +
                            "JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id " +
                            "WHERE a.MarkedDelete = 0 AND a.ProductId = @ProductId AND a.ProcessCodeId IN @ProcessCodeId ORDER BY a.ProcessCodeId"
                            , new
                            {
                                ProductId = qId,
                                ProcessCodeId = smartProduct.ProcessCodeIdsList
                            }));
                        }

                        smartProduct.ProductCapacities.AddRange(ServerConfig.ApiDb.Query<SmartProductCapacityDetail>(
                            "SELECT IFNULL(d.Id, 0) Id, IFNULL(c.Id, 0) ListId, a.Id ProcessId, b.Id PId, b.Process, b.DeviceCategoryId, b.Category, IFNULL(d.Rate, 0) Rate, IFNULL(d.`Day`, 0) `Day`, IFNULL(d.`Hour`, 0) `Hour`, IFNULL(d.`Min`, 0) `Min`, IFNULL(d.`Sec`, 0) `Sec` FROM `t_process_code_category_process` a " +
                            "JOIN (SELECT a.*, IFNULL(b.Category, '') Category FROM `t_process` a LEFT JOIN `t_device_category` b ON a.DeviceCategoryId = b.Id) b ON a.ProcessId = b.Id " +
                            "LEFT JOIN (SELECT * FROM `t_capacity_list` WHERE  MarkedDelete = 0 AND CapacityId = @CapacityId) c ON a.Id = c.ProcessId  " +
                            "LEFT JOIN (SELECT * FROM `t_product_capacity` WHERE MarkedDelete = 0 AND ProductId = @ProductId) d ON a.Id = d.ProcessId " +
                            "WHERE a.MarkedDelete = 0 AND ProcessCodeCategoryId = @ProcessCodeCategoryId ORDER BY a.`Order`"
                            , new
                            {
                                ProductId = qId,
                                CapacityId = smartProduct.CapacityId,
                                ProcessCodeCategoryId = smartProduct.CategoryId,
                            }));
                        result.datas.Add(smartProduct);
                    }
                    else
                    {
                        result.errno = Error.SmartProductNotExist;
                        return result;
                    }
                }

                else
                {
                    result.datas.AddRange(data);
                }
            }

            if (qId != 0 && !result.datas.Any())
            {
                result.errno = Error.SmartProductNotExist;
                return result;
            }
            return result;
        }

        // PUT: api/SmartProduct
        [HttpPut]
        public object PutSmartProduct([FromBody] IEnumerable<SmartProductDetail> smartProducts)
        {
            if (smartProducts == null || !smartProducts.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (smartProducts.Any(x => x.Id == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }

            if (smartProducts.Any(x => x.CapacityId == 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            if (smartProducts.SelectMany(x => x.ProductCapacities).Any(y => y.Id == 0 && y.ProcessId == 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }
            var products = smartProducts.Select(x => x.Product);
            var productIds = smartProducts.Select(x => x.Id);
            if (products.Any(x => x.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProductNotEmpty);
            }

            if (smartProducts.Count() != products.GroupBy(x => x).Count())
            {
                return Result.GenError<Result>(Error.SmartProductDuplicate);
            }

            var smartProductIds = smartProducts.Select(x => x.Id);
            var data = SmartProductHelper.Instance.GetByIds<SmartProduct>(smartProductIds);
            if (data.Count() != smartProducts.Count())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var result = new DataResult();
            var duplicate = SmartProductHelper.Instance.GetSameSmartProducts(products, productIds);
            if (duplicate.Any())
            {
                result.errno = Error.SmartProductDuplicate;
                result.datas.AddRange(duplicate.Select(x => x.Product));
                return result;
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in smartProducts)
            {
                smartProduct.CreateUserId = createUserId;
                smartProduct.MarkedDateTime = markedDateTime;
                if (smartProduct.ProductProcesses != null && smartProduct.ProductProcesses.Any())
                {
                    foreach (var process in smartProduct.ProductProcesses)
                    {
                        process.ProductId = smartProduct.Id;
                    }
                }
                if (smartProduct.ProductCapacities != null && smartProduct.ProductCapacities.Any())
                {
                    foreach (var capacity in smartProduct.ProductCapacities)
                    {
                        capacity.ProductId = smartProduct.Id;
                    }
                }
            }

            SmartProductHelper.Instance.Update(smartProducts);

            var smartProductCapacities = SmartProductCapacityHelper.Instance.GetSmartProductCapacities(productIds);
            var productCapacities = smartProducts.SelectMany(x => x.ProductCapacities);
            //删除
            var deleteCapacities = smartProductCapacities.Where(z => productCapacities.Where(y => y.Id != 0).All(a => a.Id != z.Id));
            if (deleteCapacities.Any())
            {
                SmartProductCapacityHelper.Instance.Delete(deleteCapacities.Select(x => x.Id));
            }

            //更新 
            var updateCapacities = productCapacities.Where(y => y.Id != 0);
            if (updateCapacities.Any())
            {
                SmartProductCapacityHelper.Instance.Update(updateCapacities.Select(x =>
                {
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
            }

            //新增
            var addCapacities = productCapacities.Where(y => y.Id == 0);
            if (addCapacities.Any())
            {
                SmartProductCapacityHelper.Instance.Add(addCapacities.Select(x =>
                {
                    x.CreateUserId = createUserId;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
            }
            WorkFlowHelper.Instance.OnSmartProductCapacityNeedUpdate(smartProducts);

            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProduct
        [HttpPost]
        public object PostSmartProduct([FromBody] IEnumerable<SmartProductDetail> smartProducts)
        {
            if (smartProducts == null || !smartProducts.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            var products = smartProducts.Select(x => x.Product);
            if (products.Any(x => x.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProductNotEmpty);
            }

            if (smartProducts.Any(x => x.CapacityId == 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }
            if (smartProducts.SelectMany(x => x.ProductCapacities).Any(y => y.Rate <= 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityRateError);
            }

            if (smartProducts.Count() != products.GroupBy(x => x).Count())
            {
                return Result.GenError<Result>(Error.SmartProductDuplicate);
            }
            var result = new DataResult();
            var data = SmartProductHelper.Instance.GetSmartProductsByProducts(products);
            if (data.Any())
            {
                result.errno = Error.SmartProductDuplicate;
                result.datas.AddRange(data);
                return result;
            }
            var productProcesses = smartProducts.SelectMany(x => x.ProductProcesses);
            if (productProcesses.Any())
            {
                var productProcessIds = productProcesses.Select(x => x.Id);
                var processCodes = SmartProcessCodeHelper.Instance.GetByIds<SmartProcessCode>(productProcessIds);
                foreach (var smartProduct in smartProducts)
                {
                    if (processCodes.Where(x => smartProduct.ProcessCodeIdsList.Contains(x.Id)).GroupBy(y => y.CategoryId).Count() > 1)
                    {
                        result.errno = Error.SmartProductProcessCodeCategoryMustBeSame;
                        result.datas.Add(smartProduct.Product);
                    }
                }
            }
            var productCapacities = smartProducts.SelectMany(x => x.ProductCapacities);

            if (result.errno != Error.Success)
            {
                return result;
            }
            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in smartProducts)
            {
                smartProduct.CreateUserId = createUserId;
                smartProduct.MarkedDateTime = markedDateTime;
            }
            SmartProductHelper.Instance.Add(smartProducts);
            IEnumerable<SmartProduct> productList = null;
            if (productProcesses.Any())
            {
                productList = SmartProductHelper.Instance.GetSmartProductsByProducts(products);
                foreach (var smartProduct in smartProducts)
                {

                    var product = productList.FirstOrDefault(x => x.Product == smartProduct.Product);
                    if (product != null)
                    {
                        foreach (var process in smartProduct.ProductProcesses)
                        {
                            process.ProductId = product.Id;
                            process.CreateUserId = createUserId;
                            process.MarkedDateTime = markedDateTime;
                        }
                    }
                }
                SmartProductProcessHelper.Instance.Add(productProcesses.Where(y => y.ProductId != 0));
            }
            if (productCapacities.Any())
            {
                if (productList == null)
                {
                    productList = SmartProductHelper.Instance.GetSmartProductsByProducts(products);
                }

                foreach (var smartProduct in smartProducts)
                {
                    var product = productList.FirstOrDefault(x => x.Product == smartProduct.Product);
                    if (product != null)
                    {
                        foreach (var capacity in smartProduct.ProductCapacities)
                        {
                            capacity.ProductId = product.Id;
                            capacity.CreateUserId = createUserId;
                            capacity.MarkedDateTime = markedDateTime;
                        }
                    }
                }
                SmartProductCapacityHelper.Instance.Add(productCapacities.Where(y => y.ProductId != 0));
                WorkFlowHelper.Instance.OnSmartProductCapacityNeedUpdate(productList);
            }

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/SmartProduct
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteSmartProduct([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt = SmartProductHelper.Instance.GetCountByIds(ids);
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }
            SmartProductHelper.Instance.Delete(ids);
            SmartProductProcessHelper.Instance.DeleteByProductId(ids);
            SmartProductCapacityHelper.Instance.DeleteByProductId(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}