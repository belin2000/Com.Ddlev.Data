using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    public static  class DataConnect
    {
        static Dictionary<string, IData> DataCon= new Dictionary<string, IData>();
        static readonly object DataConType = new object();
        static Boolean isready = true;

        /// <summary>
        /// 添加新的连接
        /// </summary>
        /// <param name="key"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool AddNewCon(string key, IData d)
        {
            var isok = false;
            try
            {
                lock (DataConType)
                {
                    while (isready)
                    {
                        isready = false;
                        if (DataCon.ContainsKey(key))
                        {
                            var d1 = DataCon[key];
                            DataCon.Remove(key);
                            d1.Dispose();
                        }
                        DataCon.Add(key, d);
                        isready = true;
                        break;
                    }
                }
                isok = true;
            }
            finally
            {
                isready = true;
            }
            return isok;
        }
        /// <summary>
        /// 移除不用的连接
        /// </summary>
        /// <param name="key"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool RemoveCon(string key)
        {
            var isok = false;
            try
            {
                lock (DataConType)
                {
                    while (isready)
                    {
                        if (DataCon.ContainsKey(key))
                        {
                            var d = DataCon[key];
                            DataCon.Remove(key);
                            d.Dispose();
                        }
                        isready = true;
                        break;
                    }
                }
                isok = true;
            }
            finally
            {
                isready = true;
            }
            return isok;
        }

        /// <summary>
        /// 从数据中获取连接（没有的话，返回null）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="config">配置</param>
        /// <returns></returns>
        public static IData get(string key, Config config =null)
        {
            if (DataCon.ContainsKey(key))
            {
                var d = DataCon[key];
                return d;
            }
            else
            {
                if (config == null || config.ConnectStr==null)
                {
                    string configstr = System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString;
                    string Datatype = System.Configuration.ConfigurationManager.AppSettings[key + "_DbType"];
                    if (string.IsNullOrWhiteSpace(Datatype))
                    {
                        Datatype = "SQL";
                    }
                    var d = getidata(Datatype, configstr);
                    AddNewCon(key, d);
                    return d;
                }
                else
                {
                    var d = getidata(config.Datatype, config.ConnectStr);
                    AddNewCon(key, d);
                    return d;
                }
            }
        }

        static IData getidata(string DbType,string constr)
        {
            IData data = null;
            switch (DbType)
            {
                case "SQL":
                    data = SQLHelp.IHelp(constr);
                    break;
                case "Access":
                    data = OledbHelp.IHelp(constr);
                    break;
                default:
                    data = null;
                    break;
            }
            return data;
        }

    }
}
