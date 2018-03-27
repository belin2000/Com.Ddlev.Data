using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    public class CurPage
    {
        /// <summary>
        /// 总条数
        /// </summary>
        public int total { set; get; }
        /// <summary>
        /// 当前页的数据集(List集合)
        /// </summary>
        public object rows { set; get; }
        /// <summary>
        /// 辅佐数据
        /// </summary>
        public Dictionary<string,dynamic> otherdatas { set; get; }
    }
}
