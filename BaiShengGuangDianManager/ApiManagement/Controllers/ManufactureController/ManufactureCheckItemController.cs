using ApiManagement.Base.Server;
using ApiManagement.Models.ManufactureModel;
using Microsoft.AspNetCore.Mvc;
using ModelBase.Base.EnumConfig;
using ModelBase.Base.Utils;
using ModelBase.Models.Result;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using ModelBase.Models.BaseModel;

namespace ApiManagement.Controllers.ManufactureController
{
    /// <summary>
    /// 生产检验配置
    /// </summary>
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class ManufactureCheckItemController : ControllerBase
    {
        /// <summary>
        ///  获取检验单配置项
        /// </summary>
        /// <param name="checkId">检验单id</param>
        /// <returns></returns>
        // GET: api/ManufactureCheckItem?qId=0
        [HttpGet]
        public DataResult GetManufactureCheckItem([FromQuery] int checkId)
        {
            var result = new DataResult();
            var data = ServerConfig.ApiDb.Query<ManufactureCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @checkId AND `MarkedDelete` = 0;", new { checkId });
            result.datas.AddRange(data);
            return result;
        }

        // PUT: api/ManufactureCheckItem
        [HttpPut]
        public Result PutManufactureCheckItem([FromBody] IEnumerable<ManufactureCheckItem> manufactureCheckItems)
        {
            if (manufactureCheckItems == null)
            {
                return Result.GenError<Result>(Error.ManufactureCheckItemNotExist);
            }

            if (manufactureCheckItems.Any(x => x.CheckId == 0) || manufactureCheckItems.GroupBy(x => x.CheckId).Count() != 1)
            {
                return Result.GenError<Result>(Error.ManufactureCheckNotExist);
            }

            var checkId = manufactureCheckItems.First(x => x.CheckId != 0).CheckId;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check` WHERE Id = @Id AND `MarkedDelete` = 0;", new { Id = checkId }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureCheckNotExist);
            }

            if (manufactureCheckItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.ManufactureCheckItemNotEmpty);
            }

            var data =
                ServerConfig.ApiDb.Query<ManufactureCheckItem>("SELECT * FROM `manufacture_check_item` WHERE CheckId = @checkId AND MarkedDelete = 0;", new { checkId });

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            #region 更新
            var updateItems = manufactureCheckItems.Where(x => x.Id != 0 && data.Any(y => y.Id == x.Id));
            if (updateItems.Any())
            {
                var update = false;
                foreach (var updateItem in updateItems)
                {
                    var item = data.First(x => x.Id == updateItem.Id);
                    updateItem.CheckId = updateItem.CheckId != 0 ? updateItem.CheckId : item.CheckId;
                    updateItem.Item = updateItem.Item ?? item.Item;
                    updateItem.Method = updateItem.Method ?? item.Method;
                    if (updateItem.CheckId != item.CheckId || updateItem.Item != item.Item || updateItem.Method != item.Method)
                    {
                        update = true;
                        updateItem.MarkedDateTime = markedDateTime;
                    }
                }

                if (update)
                {
                    ServerConfig.ApiDb.Execute("UPDATE manufacture_check_item SET `MarkedDateTime` = @MarkedDateTime, `Item` = @Item, `Method` = @Method WHERE `Id` = @Id;", updateItems);
                }
            }
            #endregion

            #region 删除
            var delItems = data.Where(x => manufactureCheckItems.All(y => y.Id != x.Id));
            if (delItems.Any())
            {
                foreach (var delItem in delItems)
                {
                    delItem.MarkedDateTime = markedDateTime;
                    delItem.MarkedDelete = true;
                }

                ServerConfig.ApiDb.Execute("UPDATE `manufacture_check_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` = @Id;", delItems);
            }

            #endregion

            #region 添加
            var addItems = manufactureCheckItems.Where(x => x.Id == 0);
            if (addItems.Any())
            {
                foreach (var addItem in addItems)
                {
                    addItem.CreateUserId = createUserId;
                    addItem.MarkedDateTime = markedDateTime;
                    addItem.CheckId = checkId;
                    addItem.Item = addItem.Item ?? "";
                    addItem.Method = addItem.Method ?? "";
                }
                ServerConfig.ApiDb.Execute(
                    "INSERT INTO manufacture_check_item (`CreateUserId`, `MarkedDateTime`, `CheckId`, `Item`, `Method`) VALUES (@CreateUserId, @MarkedDateTime, @CheckId, @Item, @Method);",
                    addItems);
            }
            #endregion
            return Result.GenError<Result>(Error.Success);
        }

        // POST: api/ManufactureCheckItem
        [HttpPost]
        public Result PostManufactureCheckItem([FromBody] IEnumerable<ManufactureCheckItem> manufactureCheckItems)
        {
            if (manufactureCheckItems == null)
            {
                return Result.GenError<Result>(Error.ManufactureCheckItemNotExist);
            }

            if (manufactureCheckItems.Any(x => x.Item.IsNullOrEmpty()))
            {
                return Result.GenError<Result>(Error.ManufactureCheckItemNotEmpty);
            }
            var checkIdList = manufactureCheckItems.GroupBy(x => x.CheckId).Select(y => y.Key);
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check` WHERE Id IN @checkIdList AND `MarkedDelete` = 0;", new { checkIdList }).FirstOrDefault();
            if (cnt != checkIdList.Count())
            {
                return Result.GenError<Result>(Error.ManufactureCheckNotExist);
            }

            var createUserId = Request.GetIdentityInformation();
            var markedDateTime = DateTime.Now;
            foreach (var manufactureCheckItem in manufactureCheckItems)
            {
                manufactureCheckItem.CreateUserId = createUserId;
                manufactureCheckItem.MarkedDateTime = markedDateTime;
            }
            ServerConfig.ApiDb.Execute(
                "INSERT INTO manufacture_check_item (`CreateUserId`, `MarkedDateTime`, `CheckId`, `Item`, `Method`) VALUES (@CreateUserId, @MarkedDateTime, @CheckId, @Item, @Method);",
                manufactureCheckItems);

            return Result.GenError<Result>(Error.Success);
        }

        // DELETE: api/ManufactureCheckItem
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteManufactureCheckItem([FromBody] BatchDelete batchDelete)
        {
            var ids = batchDelete.ids;
            var cnt =
                ServerConfig.ApiDb.Query<int>("SELECT COUNT(1) FROM `manufacture_check_item` WHERE Id IN @id AND `MarkedDelete` = 0;", new { id = ids }).FirstOrDefault();
            if (cnt == 0)
            {
                return Result.GenError<Result>(Error.ManufactureCheckItemNotExist);
            }

            ServerConfig.ApiDb.Execute(
                "UPDATE `manufacture_check_item` SET `MarkedDateTime`= @MarkedDateTime, `MarkedDelete`= @MarkedDelete WHERE `Id` IN @Id;", new
                {
                    MarkedDateTime = DateTime.Now,
                    MarkedDelete = true,
                    Id = ids
                });

            return Result.GenError<Result>(Error.Success);
        }
    }
}