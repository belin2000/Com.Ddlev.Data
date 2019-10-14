using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    public class Config
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectStr { set; get; }
        /// <summary>
        /// 类型(SQL/Access)
        /// </summary>
        public string Datatype { set; get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_ConnectStr">连接字符串</param>
        /// <param name="_Datatype">类型(SQL/Access/MySql)</param>
        public Config(string _ConnectStr=null, string _Datatype=null)
        {
            this.ConnectStr = _ConnectStr;
            this.Datatype = _Datatype;
        }

    }
}
