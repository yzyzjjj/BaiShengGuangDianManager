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
            var sql = $"SELECT a.*, IFNULL(ProcessCodeIds, '') ProcessCodeIds FROM `t_product` a " +
                      $"LEFT JOIN (SELECT ProductId, GROUP_CONCAT(DISTINCT ProcessCodeId) ProcessCodeIds FROM `t_product_process` WHERE MarkedDelete = 0 GROUP BY ProductId ORDER BY ProcessCodeId) b ON a.Id = b.ProductId " +
                      $"WHERE a.MarkedDelete = 0{(qId == 0 ? "" : " AND a.Id = @qId")} ORDER BY a.Id Desc;";
            var data = ServerConfig.ApiDb.Query<SmartProductDetail>(sql, new { qId });
            if (menu)
            {
                result.datas.AddRange(data.Select(x => new { x.Id, x.Product }));
            }
            else
            {
                var processCodeIds = data.SelectMany(x => x.ProcessCodeIdsList).Distinct();
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

                if (qId != 0)
                {
                    var smartProduct = data.FirstOrDefault();
                    if (smartProduct != null)
                    {
                        //var smartProductDetail = ClassExtension.ParentCopyToChild<SmartProduct, SmartProductDetail>(smartProduct);
                        //smartProductDetail.ProductProcesses.AddRange(ServerConfig.ApiDb.Query<SmartProductProcessDetail>(
                        //    "SELECT a.*, b.Process FROM `t_product_process` a JOIN `t_process` b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND a.ProductId = @ProductId AND a.ProcessCodeId IN @ProcessCodeId ORDER BY a.ProcessCodeId"
                        //    , new
                        //    {
                        //        ProductId = qId,
                        //        ProcessCodeId = smartProduct.ProcessCodeIdsList
                        //    }));
                        //result.datas.Add(smartProductDetail);
                        smartProduct.ProductProcesses.AddRange(ServerConfig.ApiDb.Query<SmartProductProcessDetail>(
                            "SELECT a.*, b.Process FROM `t_product_process` a JOIN (SELECT a.Id, b.Process FROM `t_process_code_category_process` a JOIN `t_process` b ON a.ProcessId = b.Id) b ON a.ProcessId = b.Id WHERE a.MarkedDelete = 0 AND a.ProductId = @ProductId AND a.ProcessCodeId IN @ProcessCodeId ORDER BY a.ProcessCodeId"
                            , new
                            {
                                ProductId = qId,
                                ProcessCodeId = smartProduct.ProcessCodeIdsList
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
            }

            var smartProductProcesses = SmartProductProcessHelper.Instance.GetSameSmartProductProcesses(productIds);
            SmartProductHelper.Instance.Update(smartProducts);
            //删除 
            var delete = smartProductProcesses.Where(z => smartProducts.SelectMany(x => x.ProductProcesses).Where(y => y.Id != 0).All(a => a.Id != z.Id));
            if (delete.Any())
            {
                SmartProductProcessHelper.Instance.Delete(delete.Select(x=>x.Id));
            }
            //更新 
            var update = smartProducts.SelectMany(x => x.ProductProcesses).Where(y => y.Id != 0);
            if (update.Any())
            {
                SmartProductProcessHelper.Instance.Update(update.Select(x =>
                {
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
            }

            //新增
            var add = smartProducts.SelectMany(x => x.ProductProcesses).Where(y => y.Id == 0);
            if (add.Any())
            {
                SmartProductProcessHelper.Instance.Add(add.Select(x =>
                {
                    x.CreateUserId = createUserId;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
            }

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
            if (productProcesses.Any())
            {
                var productList = SmartProductHelper.Instance.GetSmartProductsByProducts(products);
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
            return Result.GenError<Result>(Error.Success);
        }
    }
}