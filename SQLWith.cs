using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// SQL锁
    /// </summary>
    
    public enum SQLWith
    {
        /// <summary>
        /// 在读取或修改数据时不加任何锁,有可能读取到脏数据，
        /// </summary>
        NOLOCK = 1,
        /// <summary>
        /// 排除锁定的数据
        /// </summary>
        READPAST = 2,
        /// <summary>
        /// 行锁
        /// </summary>
        ROWLOCK = 4,
        /// <summary>
        /// 表锁
        /// </summary>
        TABLOCK =8,
        /// <summary>
        /// 更新锁
        /// </summary>
        UPDLOCK=16,

    }
}
