using ApiManagement.Base.Server;
using Dapper;
using ModelBase.Models.BaseModel;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiManagement.Models.BaseModel
{
    public class DataHelper : DataBaseHelper
    {
        public DataHelper()
        {
            DB = ServerConfig.ApiDb;
        }
    }
}
