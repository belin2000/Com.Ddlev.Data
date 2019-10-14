using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    public static class LambdaHelper
    {
        public static Lambda.IDataQuery<T> GetHelper<T>(SQLType dbt= SQLType.MSSQL) where T:class,new()
        {
            Lambda.IDataQuery<T> ip = null; 
            switch (dbt)
            {
                case SQLType.MSSQL:
                    ip=new Lambda.MSSQLDataQuery<T>();
                    break;
            }
            return ip;
        }
    }
}
