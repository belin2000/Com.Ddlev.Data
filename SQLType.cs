using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum SQLType
    {
        /// <summary>
        /// 微软的SQL server数据库（中型，大型）
        /// </summary>
        MSSQL=0,
        /// <summary>
        /// 微软的Access小型数据库（小型）
        /// </summary>
        ACCESS = 1,
        /// <summary>
        /// SQLLite 轻型数据库（微型）
        /// </summary>
        SQLite = 4,
        /// <summary>
        /// MYSQL数据库（小型，中型）
        /// </summary>
        MYSQL = 2,
        /// <summary>
        /// 甲骨文Oracle数据库（大型，超大型）
        /// </summary>
        ORACLE = 3
        
    }

}
