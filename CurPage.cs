using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{

    public interface ICurPage<T> {
        int total { set; get; }
        T rows { set; get; }
        Dictionary<string, dynamic> otherdatas { set; get; }
    }
    public interface ICurPage: ICurPage<object>
    {
    }

    public class BaseCurPage<T>: ICurPage<T>
    {
        /// <summary>
        /// 总条数
        /// </summary>
        public int total { set; get; }

        /// <summary>
        /// 当前页的数据集(List集合)
        /// </summary>
        public T rows { set; get; }

        /// <summary>
        /// 辅佐数据
        /// </summary>
        public Dictionary<string, dynamic> otherdatas { set; get; }
    }
    public class CurPage : BaseCurPage<object>, ICurPage
    {
        
    }

    public class CurPage<T> :  BaseCurPage<List<T>>, ICurPage<List<T>> where T:class, new()
    {
    }
}
