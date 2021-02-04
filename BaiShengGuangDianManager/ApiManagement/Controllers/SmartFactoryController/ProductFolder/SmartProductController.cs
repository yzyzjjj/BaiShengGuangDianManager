using ApiManagement.Base.Helper;
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
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]"), ApiController]
    public class SmartProductController : ControllerBase
    {
        // GET: api/SmartProduct
        [HttpGet]
        public DataResult GetSmartProduct([FromQuery]int qId, int wId, bool menu)
        {
            var result = new DataResult();
            if (menu)
            {
                result.datas.AddRange(SmartProductHelper.GetMenu(qId, wId));
            }
            else
            {
                var data = SmartProductHelper.GetDetail(qId, wId);
                var processCodeIds = data.SelectMany(x => x.ProcessCodeIdsList).Distinct();
                if (processCodeIds.Any())
                {
                    var processCodeIdsList = SmartProcessCodeHelper.Instance.GetAllByIds<SmartProcessCode>(processCodeIds);
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
                            smartProduct.Processes.AddRange(SmartProductProcessHelper.GetDetail(qId, smartProduct.ProcessCodeIdsList));
                        }

                        smartProduct.Capacities.AddRange(SmartProductCapacityHelper.GetDetail(smartProduct.CapacityId, qId, smartProduct.CategoryId));
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
        public object PutSmartProduct([FromBody] IEnumerable<SmartProductDetail> products)
        {
            if (products == null || !products.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (products.Any(x => x.Id == 0))
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (products.Any(x => x.Product.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProductNotEmpty);
            }
            if (products.GroupBy(x => x.Product).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProductDuplicate);
            }
            if (products.SelectMany(x => x.Capacities).Any(y =>
            {
                var rate = 0m;
                if (y.DeviceList.Any())
                {
                    rate = y.DeviceList.First().Rate;
                }
                else if (y.OperatorList.Any())
                {
                    rate = y.OperatorList.First().Rate;
                }

                return rate <= 0;
            }))
            {
                return Result.GenError<Result>(Error.SmartCapacityRateError);
            }
            //if (smartProducts.Any(x => x.CapacityId == 0))
            //{
            //    return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            //}
            if (products.SelectMany(x => x.Capacities).Any(y => y.Id == 0 && y.ProcessId == 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }

            var wId = products.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = products.Select(x => x.Product);
            var ids = products.Select(x => x.Id);
            var result = new DataResult();
            var data = SmartProductHelper.CommonGetSames(wId, sames, ids);
            if (data.Any())
            {
                result.errno = Error.SmartProductDuplicate;
                result.datas.AddRange(data);
                return result;
            }

            var cnt = SmartProductHelper.Instance.GetCountByIds(ids);
            if (cnt != products.Count())
            {
                return Result.GenError<Result>(Error.SmartProductNotExist);
            }

            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in products)
            {
                smartProduct.MarkedDateTime = markedDateTime;
                smartProduct.Remark = smartProduct.Remark ?? "";
                if (smartProduct.Processes != null && smartProduct.Processes.Any())
                {
                    foreach (var process in smartProduct.Processes)
                    {
                        process.ProductId = smartProduct.Id;
                    }
                }
                if (smartProduct.Capacities != null && smartProduct.Capacities.Any())
                {
                    foreach (var capacity in smartProduct.Capacities)
                    {
                        capacity.ProductId = smartProduct.Id;
                    }
                }
            }

            SmartProductHelper.Instance.Update(products);

            var smartProductCapacities = SmartProductCapacityHelper.GetSmartProductCapacities(ids);
            var productCapacities = products.SelectMany(x => x.Capacities);
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
                    x.CreateUserId = userId;
                    x.MarkedDateTime = markedDateTime;
                    return x;
                }));
            }
            WorkFlowHelper.Instance.OnSmartProductCapacityNeedUpdate(products);
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/SmartProduct
        [HttpPost]
        public object PostSmartProduct([FromBody] IEnumerable<SmartProductDetail> products)
        {
            if (products == null || !products.Any())
            {
                return Result.GenError<Result>(Error.ParamError);
            }
            if (products.Any(x => x.Product.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.SmartProductNotEmpty);
            }
            if (products.GroupBy(x => x.Product).Any(y => y.Count() > 1))
            {
                return Result.GenError<Result>(Error.SmartProductDuplicate);
            }
            if (products.SelectMany(x => x.Capacities).Any(y =>
            {
                var rate = 0m;
                if (y.DeviceList.Any())
                {
                    rate = y.DeviceList.First().Rate;
                }
                else if (y.OperatorList.Any())
                {
                    rate = y.OperatorList.First().Rate;
                }

                return rate <= 0;
            }))
            {
                return Result.GenError<Result>(Error.SmartCapacityRateError);
            }
            //if (smartProducts.Any(x => x.CapacityId == 0))
            //{
            //    return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            //}
            if (products.SelectMany(x => x.Capacities).Any(y => y.Id == 0 && y.ProcessId == 0))
            {
                return Result.GenError<Result>(Error.SmartCapacityListNotEmpty);
            }
            var wId = products.FirstOrDefault()?.WorkshopId ?? 0;
            var sames = products.Select(x => x.Product);
            var ids = products.Select(x => x.Id);
            var result = new DataResult();
            var data = SmartProductHelper.CommonGetSames(wId, sames, ids);
            if (data.Any())
            {
                result.errno = Error.SmartProductDuplicate;
                result.datas.AddRange(data);
                return result;
            }
            var productProcesses = products.SelectMany(x => x.Processes);
            if (productProcesses.Any())
            {
                var productProcessIds = productProcesses.Select(x => x.Id);
                var processCodes = SmartProcessCodeHelper.Instance.GetByIds<SmartProcessCode>(productProcessIds);
                foreach (var smartProduct in products)
                {
                    if (processCodes.Where(x => smartProduct.ProcessCodeIdsList.Contains(x.Id)).GroupBy(y => y.CategoryId).Count() > 1)
                    {
                        result.errno = Error.SmartProductProcessCodeCategoryMustBeSame;
                        result.datas.Add(smartProduct.Product);
                    }
                }
            }
            var productCapacities = products.SelectMany(x => x.Capacities);

            if (result.errno != Error.Success)
            {
                return result;
            }
            var userId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var smartProduct in products)
            {
                smartProduct.CreateUserId = userId;
                smartProduct.MarkedDateTime = markedDateTime;
            }
            SmartProductHelper.Instance.Add(products);

            IEnumerable<SmartProduct> productList = null;
            if (productProcesses.Any())
            {
                productList = SmartProductHelper.GetDetail(wId, sames);
                foreach (var smartProduct in products)
                {
                    var product = productList.FirstOrDefault(x => x.Product == smartProduct.Product);
                    if (product != null)
                    {
                        foreach (var process in smartProduct.Processes)
                        {
                            process.CreateUserId = userId;
                            process.MarkedDateTime = markedDateTime;
                            process.ProductId = product.Id;
                        }
                    }
                }
                SmartProductProcessHelper.Instance.Add(productProcesses.Where(y => y.ProductId != 0));
            }
            if (productCapacities.Any())
            {
                if (productList == null)
                {
                    productList = SmartProductHelper.GetDetail(wId, sames);
                }

                foreach (var smartProduct in products)
                {
                    var product = productList.FirstOrDefault(x => x.Product == smartProduct.Product);
                    if (product != null)
                    {
                        foreach (var capacity in smartProduct.Capacities)
                        {
                            capacity.ProductId = product.Id;
                            capacity.CreateUserId = userId;
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
            SmartProductProcessHelper.Instance.DeleteFromParent(ids);
            SmartProductCapacityHelper.Instance.DeleteFromParent(ids);
            return Result.GenError<Result>(Error.Success);
        }
    }
}