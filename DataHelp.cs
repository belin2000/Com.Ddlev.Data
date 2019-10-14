using System;
using System.Reflection;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// 数据反射等操作
    /// </summary>
    public class DataHelp
    {
        static Dictionary<string, DataHelp> DataHelpDic = new Dictionary<string, DataHelp>();
        static readonly object DataHelpType = new object();
        static Boolean isready = true;
        public string key { set; get; }
        public Config Config { set; get; }

        private SQLType SqlType { set; get; }
        private int YearVer = 0;


        public static DataHelp GetDataHelpget(string key, Config config =null, SQLType sqltype= SQLType.MSSQL)
        {
            try
            {
                //判断是否存在链接
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key]==null || string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString))
                {
                    //不存在则使用默认链接
                    key = "strConn";
                }
            }
            catch
            {
                key = "strConn";
            }

            if (DataHelpDic.ContainsKey(key))
            {
                var d = DataHelpDic[key];
                return d;
            }
            else
            {
                lock (DataHelpType)
                {
                    if (DataHelpDic.ContainsKey(key))
                    {
                        var d = DataHelpDic[key];
                        return d;
                    }
                    var dhelp = new DataHelp(key, config, sqltype);
                    AddNew(key, dhelp);
                    return dhelp;
                }
            }
        }
        /// <summary>
        /// 添加新的连接
        /// </summary>
        /// <param name="key"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static bool AddNew(string key, DataHelp d,SQLType _SQLType= SQLType.MSSQL)
        {
            var isok = false;
            try
            {
                while (isready)
                {
                    isready = false;
                    if (DataHelpDic.ContainsKey(key))
                    {
                        var d1 = DataHelpDic[key];
                        DataHelpDic.Remove(key);
                        IData data = DataConnect.get(d.key, d.Config);
                        data.Dispose();
                    }
                    DataHelpDic.Add(key, d);
                    isready = true;
                    break;
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
        static bool Remove(string key)
        {
            var isok = false;
            try
            {
                
                while (isready)
                {
                    isready = false;
                    if (DataHelpDic.ContainsKey(key))
                    {
                        var d = DataHelpDic[key];
                        DataHelpDic.Remove(key);
                        IData data = DataConnect.get(d.key, d.Config);
                        data.Dispose();
                    }
                    isready = true;
                    break;
                }
                isok = true;
                
            }
            finally
            {
                isready = true;
            }
            return isok;
        }

        public DataHelp(string _key, Config _config = null,SQLType stype= SQLType.MSSQL)
        {
            this.key = _key;
            this.Config = _config;
            this.SqlType = stype;
        }

        public Lambda.IDataQuery<T> GetLambdaHelper<T>() where T : class, new()
        {
            return LambdaHelper.GetHelper<T>(SqlType);
        }

        /// <summary>
        /// 反射数据(先用 DataReader.Read() 判断),返回后要关闭DataReader
        /// </summary>
        /// <param name="reader">DataReader</param>
        /// <param name="targetObj">类（对应的表的属性）</param>
        public void ReaderToObject(IDataReader reader, object targetObj)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                PropertyInfo propertyInfo = targetObj.GetType().GetProperty(reader.GetName(i), BindingFlags.Public| BindingFlags.Instance| BindingFlags.IgnoreCase);
                if (propertyInfo != null)
                {
                    if (reader.GetValue(i) != DBNull.Value)
                    {
                        if (propertyInfo.PropertyType.IsEnum)
                        {
                            propertyInfo.SetValue(targetObj, Enum.ToObject(propertyInfo.PropertyType, reader.GetValue(i)), null);
                        }
                        else
                        {
                            propertyInfo.SetValue(targetObj, Convert.ChangeType(reader.GetValue(i),propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 反射数据到类中，返回List&lt;T&gt;,返回后要关闭DataReader
        /// </summary>
        /// <typeparam name="T">类名称</typeparam>
        /// <param name="dr">dr集合</param>
        /// <returns></returns>
        public IList<T> ReaderToObject<T>(IDataReader dr) where T : new()
        {
            List<T> li = new List<T>();
                while (dr.Read())
                {
                    var ClassName = new T();
                    ReaderToObject(dr, ClassName);
                    li.Add(ClassName);
                }
            dr.Close();
            return li;
        }
        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="t">类对象</param>
        /// <param name="dir">不加入的数据集合,类的属性</param>
        /// <returns></returns>
        public bool Add(object t, string[] dir=null)
        {
            if (dir == null)
            {
                dir = new string[] { };
            }
            int ok = 0;
            PropertyInfo[] pis = t.GetType().GetProperties();
            List<DataItemType> list = new List<DataItemType>();
            string coms = "";
            string values = "";
            foreach (PropertyInfo pi in pis)
            {
                if (dir.Contains(pi.Name))
                {
                    continue;
                }
                var v = pi.GetValue(t, null);
                list.Add(new DataItemType("@" + pi.Name, v));
                coms += "[" + pi.Name + "],";
                values += "@" + pi.Name + ",";
            }
            coms = coms.Substring(0, coms.Length == 0 ? 0 : (coms.Length - 1));
            values = values.Substring(0, values.Length == 0 ? 0 : (values.Length - 1));
            using (IData data = DataConnect.get(key, Config))
            {
                ok = data.ExecuteNonQuery("insert into " + t.GetType().Name + "(" + coms + ") values(" + values + ")", list);
            }

            return ok > 0 ? true : false;
        }
        /// <summary>
        /// 添加数据(返回自增列)
        /// </summary>
        /// <param name="t">类对象</param>
        /// <param name="dir">不加入的数据集合,类的属性</param>
        /// <param name="idname">自增列的列名</param>
        /// <returns></returns>
        public int Add(object t, string[] dir,string idname)
        {
            var id = 0;
            switch (SqlType)
            {
                case SQLType.MSSQL:
                    id = Com.Ddlev.Data.Helper.SQLHelper.Add(t, dir, idname, key, Config);
                    break;
            }
            return id;
        }
        /// <summary>
        /// 使用指定的数据插入到表中
        /// </summary>
        /// <typeparam name="T">类</typeparam>
        /// <param name="exp">表达式</param>
        /// <returns></returns>
        public bool Insert<T>(Expression<Func<T, bool>> exp) where T : class, new()
        {
            var lr= LambdaHelper.GetHelper<T>(SqlType);
            lr.Insert(exp);
            return ExecuteNonQuery(lr.Res.SQL, lr.Res.Para) >0;
        }


        /// <summary>
        /// 修改数据
        /// </summary>
        /// <param name="t">类对象</param>
        /// <param name="dir">用作对象修改的数据,做where条件</param>
        /// <param name="exdir">不进行判断的属性集合</param>
        /// <returns></returns>
        public bool Edit(object t, string[] dir,string[] exdir=null)
        {
            int ok = 0;
            PropertyInfo[] pis = t.GetType().GetProperties();
            List<DataItemType> list = new List<DataItemType>();
            string coms = "";
            string where=" 1=1 ";
            foreach (PropertyInfo pi in pis)
            {
                if (exdir==null || !exdir.Contains(pi.Name))
                {
                    list.Add(new DataItemType("@" + pi.Name, pi.GetValue(t, null)));
                    if (dir.Contains(pi.Name))
                    {
                        where += " and [" + pi.Name + "]=@" + pi.Name;
                    }
                    else
                    {
                        coms += "[" + pi.Name + "]=@" + pi.Name + ",";
                    }
                }
            }
            coms = coms.Substring(0, coms.Length - 1);

            using (IData data = DataConnect.get(key, Config))
            {
                ok = data.ExecuteNonQuery("update " + t.GetType().Name+ "  set " + coms +" where "+where, list);
            }

            return ok > 0 ? true : false;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="t">类名字</param>
        /// <param name="dic">数据集合,类的属性</param>
        /// <returns></returns>
        public bool Delete(string t, Dictionary<string, string> dic=null)
        {
            int ok = 0;
            string where = "1=1 ";
            List<DataItemType> list = new List<DataItemType>();
            if (dic != null && dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> k in dic)
                {
                    list.Add(new DataItemType("@" + k.Key, k.Value));
                    where += " and [" + k.Key + "]=@" + k.Key;
                }
            }

            using (IData data = DataConnect.get(key, Config))
            {
                ok = data.ExecuteNonQuery( "delete  from "+t+" where " + where, list);
            }

            return ok > 0 ? true : false;
        }
        
        /// <summary>
        /// 返回一条记录的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句，可包含@参数</param>
        /// <param name="list">对应@参数和值的集合</param>
        /// <returns></returns>
        public T GetInfo<T>(string sql, List<DataItemType> list=null) where T : class, new()
        {
            T t = default(T);
            //var isok = false;
            using (IData data = DataConnect.get(key, Config))
            {
                IDataReader dr = data.ExecuteIReader(sql,  list);
                if (dr.Read())
                {
                    t = new T();
                    ReaderToObject(dr, t);
                    //isok = true;
                }
                dr.Close();
                dr.Dispose();
            }

            return t;
        }
        /// <summary>
        /// 获取相关的信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table">表名或者 (sql) 语句:括号sql语句括号</param>
        /// <param name="dic"></param>
        /// <returns></returns>
        public T GetInfo<T>(string table, Dictionary<string, string> dic) where T : class, new()
        {
            string where = "1=1 ";
            List<DataItemType> list = new List<DataItemType>();
            if (dic != null && dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> k in dic)
                {
                    list.Add(new DataItemType("@" + k.Key, k.Value));
                    where += " and " + k.Key + "=@" + k.Key;
                }
            }

            return GetInfo<T>(("select *  from " + table + " as T_  where " + where), list);
        }
        public T GetInfo<T>(Dictionary<string, string> dic) where T : class, new()
        {
            string where = "1=1 ";
            List<DataItemType> list = new List<DataItemType>();
            if (dic != null && dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> k in dic)
                {
                    list.Add(new DataItemType("@" + k.Key, k.Value));
                    where += " and " + k.Key + "=@" + k.Key;
                }
            }

            return GetInfo<T>(("select *  from " + typeof(T).Name + " as T_  where " + where), list);
        }


        public T GetInfo<T>(Expression<Func<T, bool>> exp) where T : class, new()
        {
            return Single<T>(exp);
        }
        /// <summary>
        /// 返回DataTalbe对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句 或者表名字，sql可包含@参数</param>
        /// <param name="list">对应@参数和值的集合</param>
        /// <returns></returns>
        public DataTable GetList(string sql, List<DataItemType> list = null)
        {

            var dt = new DataTable();
            using (IData data = DataConnect.get(key, Config))
            {
                dt = data.ExecQuery(sql,  list);
            }
            return dt;

        }
        public DataTable GetList(string sql, Dictionary<string, string> dic)
        {
            List<DataItemType> list = new List<DataItemType>();
            if (dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> k in dic)
                {
                    list.Add(new DataItemType("@" + k.Key, k.Value));
                }
            }

            return GetList(sql, list);

        }
                

        /// <summary>
        /// 返回一组记录的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句，sql可包含@参数</param>
        /// <param name="list">对应@参数和值的集合</param>
        /// <returns></returns>
        public List<T> GetList<T>(string sql, List<DataItemType> list=null) where T : new()
        {

            List<T> listT = new List<T>();
            using (IData data = DataConnect.get(key, Config))
            {
                IDataReader dr = data.ExecuteIReader(sql, list);
                listT = (List<T>)ReaderToObject<T>(dr);
                dr.Close();
                dr.Dispose();
            }
            return listT;
            
        }
        public List<T> GetList<T>(Expression<Func<T, bool>> exp = null) where T : class,  new()
        {
            return Select<T>(exp);

        }
        /// <summary>
        /// 返回一组记录的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句 或者表名字，sql可包含@参数</param>
        /// <param name="dic">对应参数和值的集合</param>
        public List<T> GetList<T>(string sql, Dictionary<string,string> dic) where T : new()
        {
            List<DataItemType> list = new List<DataItemType>();
            if (dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> k in dic)
                {
                    list.Add(new DataItemType("@" + k.Key, k.Value));
                }
            }
            
            return GetList<T>(sql,list);

        }
        /// <summary>
        /// 获取相关的信息集合
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="Table">组合的sql语句或者表名</param>
        /// <param name="where">条件(有where)</param>
        /// <param name="orderby">排序(有order by)</param>
        /// <returns></returns>
        public  List<T> GetList<T>(string Table,string where,string orderby, List<DataItemType> lists = null) where T : new()
        {
            List<T> list = new List<T>();
            string sql = "select * from " + Table + " ";
            if (!string.IsNullOrEmpty(where) && where!="")
            {
                sql += where;
            }
            if (!string.IsNullOrEmpty(orderby) && orderby!="")
            {
                sql +=" "+ orderby;
            }
            return GetList<T>(sql, lists);
        }
        /// <summary>
        /// 获取相关的信息集合
        /// </summary>
        /// <param name="Table">表名或者用（sql）组成的表</param>
        /// <param name="PageSize">每页的条数</param>
        /// <param name="PageNum">单前页,必须大于0</param>
        /// <param name="OrderBy"> id desc/ id asc(倒序或者顺序)</param>
        /// <returns></returns>
        public DataTable GetList(string Table, int PageNum, int PageSize, string OrderBy, List<DataItemType> list = null)
        {
            string sql = "";
            switch (SqlType)
            {
                case SQLType.MSSQL:
                    sql = Helper.SQLHelper.GetListSql(Table, PageNum, PageSize, OrderBy, key, Config);
                    break;
            }

            return GetList(sql, list);
        }

        
        /// <summary>
        /// 分页
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="Table">表名或者用（sql）组成的表</param>
        /// <param name="PageSize">每页的条数</param>
        /// <param name="PageNum">单前页,必须大于0</param>
        /// <param name="OrderBy"> id desc/ id asc(倒序或者顺序)</param>
        /// <returns></returns>
        public List<T> GetList<T>(string Table,  int PageNum, int PageSize, string OrderBy, List<DataItemType> list=null) where T : new()
        {
            string sql = "";
            switch (SqlType)
            {
                case SQLType.MSSQL:
                    sql = Helper.SQLHelper.GetListSql(Table, PageNum, PageSize, OrderBy, key, Config);
                    break;
            }

            return GetList<T>(sql, list);
        }
        /// <summary>
        /// 分页
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="Table">表名或者用（sql）组成的表</param>
        /// <param name="PageSize">每页的条数</param>
        /// <param name="PageNum">单前页,必须大于0</param>
        /// <param name="com">用于排序的列</param>
        /// <param name="desc">desc/asc(倒序或者顺序)</param>
        /// <returns></returns>
        public List<T> GetList<T>(string Table, int PageNum, int PageSize, string com, string desc, List<DataItemType> list = null) where T : new()
        {
            return GetList<T>(Table, PageNum, PageSize, com + " " + desc);
        }

        /// <summary>
        /// 初始化类
        /// </summary>
        /// <param name="obj">实体类</param>
        /// <returns></returns>
        public object InitClass(object obj)
        {
            PropertyInfo[] pis = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                if (pi.PropertyType==typeof(string)||pi.PropertyType==typeof(String))
                {
                    pi.SetValue(obj, "", null);
                    continue;
                }
                if (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(Int16) || pi.PropertyType == typeof(Int32) || pi.PropertyType == typeof(Int64) || pi.PropertyType == typeof(decimal) || pi.PropertyType == typeof(Decimal) || pi.PropertyType == typeof(double) || pi.PropertyType == typeof(Double) || pi.PropertyType == typeof(float))
                {
                    pi.SetValue(obj, Convert.ChangeType(0,pi.PropertyType), null);
                    continue;
                }
                if (pi.PropertyType == typeof(DateTime))
                {
                    pi.SetValue(obj, DateTime.Now, null);
                    continue;
                }
                if (pi.PropertyType == typeof(bool) || pi.PropertyType == typeof(Boolean))
                {
                    pi.SetValue(obj, false, null);
                    continue;
                }
                if (pi.PropertyType == typeof(Enum))
                {
                    continue;
                }
                pi.SetValue(obj, Activator.CreateInstance(pi.PropertyType), null);
            }
            return obj;
        }
        public T InitClass<T>(T obj) where T:new()
        {
            PropertyInfo[] pis = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                if (pi.PropertyType == typeof(string) || pi.PropertyType == typeof(String))
                {
                    pi.SetValue(obj, "", null);
                    continue;
                }
                if (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(Int16) || pi.PropertyType == typeof(Int32) || pi.PropertyType == typeof(Int64) || pi.PropertyType == typeof(decimal) || pi.PropertyType == typeof(Decimal) || pi.PropertyType == typeof(double) || pi.PropertyType == typeof(Double) || pi.PropertyType == typeof(float))
                {
                    pi.SetValue(obj, Convert.ChangeType(0, pi.PropertyType), null);
                    continue;
                }
                if (pi.PropertyType == typeof(DateTime))
                {
                    pi.SetValue(obj, DateTime.Now, null);
                    continue;
                }
                if (pi.PropertyType == typeof(bool) || pi.PropertyType == typeof(Boolean))
                {
                    pi.SetValue(obj, false, null);
                    continue;
                }
                if (pi.PropertyType == typeof(Enum))
                {
                    continue;
                }
                pi.SetValue(obj, Activator.CreateInstance(pi.PropertyType), null);
            }
            return obj;
        }
        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="dic">where的相关的参数集合</param>
        /// <returns></returns>
        public int Count(string table,Dictionary<string, string> dic)
        {
            List<DataItemType> list = new List<DataItemType>();
            string where = "";
            if (dic!=null && dic.Count > 0)
            {
                where = " where 1=1 ";
                foreach (KeyValuePair<string, string> kvp in dic)
                {
                    where += " and " + kvp.Key + "=@" + kvp.Key;
                    list.Add(new DataItemType("@"+kvp.Key,kvp.Value));
                }
            }
            string sql = " select count(*) from " + table + " as _t  " + where;
            return Count(sql, list);
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="list">相关的参数集合</param>
        /// <returns></returns>
        public int Count(string sql, List<DataItemType> list=null)
        {
            int count = 0;
            using (IData data = DataConnect.get(key, Config))
            {
                count = Convert.ToInt32(data.ExecuteScalar(sql, list, CommandType.Text));
            }
            return count;
        }


        /// <summary>
        /// 把obj1的值copy到obj里面
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="obj1"></param>
        public void Copy(object obj, object obj1)
        {
            PropertyInfo[] pis = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                try
                {
                    pi.SetValue(obj, Convert.ChangeType(obj1.GetType().GetProperty(pi.Name).GetValue(obj1, null), pi.PropertyType), null);
                }
                catch
                {
                    continue;
                }
            }
        }

        public List<T> DataTableToList<T>(DataTable dt) where T:new()
        {
            List<T> ts = new List<T>();
            Type type = typeof(T);
            string tempName = "";
            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                // 获得此模型的公共属性
                PropertyInfo[] propertys = t.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;
                    // 检查DataTable是否包含此列
                    if (dt.Columns.Contains(tempName))
                    {
                        // 判断此属性是否有Setter
                        if (!pi.CanWrite) continue;
                        object value = dr[tempName];
                        if (value != DBNull.Value)
                            pi.SetValue(t, value, null);
                    }
                }
                ts.Add(t);
            }
            return ts;
        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="tablename">表名</param>
        /// <param name="q">where条件(不带where的)</param>
        /// <param name="PageNum">第几页</param>
        /// <param name="PageSize">每页数</param>
        /// <param name="Orderby">排序（不带orderby）</param>
        /// <param name="lists"></param>
        /// <returns></returns>
        public ICurPage GetCurPage(string tablename, string q, int PageNum, int PageSize,  string Orderby, List<DataItemType> lists = null)
        {
            CurPage cp = new CurPage();
            string sql = "select * from " + tablename + "";
            if (!string.IsNullOrEmpty(q) && q != "")
            {
                sql += " where " + q;
            }
            cp.rows = GetList("(" + sql + ")", PageNum, PageSize, Orderby, lists); 
            cp.total = Count("select COUNT(*) from (" + sql + ") as _c_t ", lists);
            return cp;
        }
        /// <summary>
        /// 分页
        /// </summary>
        /// <typeparam name="T">返回实例化的对象</typeparam>
        /// <param name="tablename">表名</param>
        /// <param name="q">where条件(不带where的)</param>
        /// <param name="PageNum">第几页</param>
        /// <param name="PageSize">每页数</param>
        /// <param name="Orderby">排序（不带orderby）</param>
        /// <param name="lists"></param>
        /// <returns></returns>
        public CurPage<T> GetCurPage<T>(string tablename,string q, int PageNum, int PageSize,  string Orderby, List<DataItemType> lists=null) where T : class, new()
        {
            CurPage<T> cp = new CurPage<T>();
            string sql = "select * from " + tablename + "";
            if (!string.IsNullOrWhiteSpace(q.Trim()))
            {
                sql += " where " + q;
            }
            List<T> list = new List<T>();
            list = GetList<T>("(" + sql + ")", PageNum, PageSize, Orderby, lists);
            cp.rows = list;
            cp.total = Count("select COUNT(*) from (" + sql + ") as _c_t ", lists);
            return cp;
        }
        /// <summary>
        /// 分页
        /// </summary>
        /// <typeparam name="T">返回的类型</typeparam>
        /// <param name="Table">表或者带()的sql语句</param>
        /// <param name="PageSize">每页数</param>
        /// <param name="PageNum">当前页</param>
        /// <param name="OrderBy">排序（不带order by字符）</param>
        /// <param name="lists">条件参数值</param>
        /// <returns></returns>
        public CurPage<T> GetCurPage<T>(string Table, int PageNum, int PageSize,  string OrderBy, List<DataItemType> lists = null) where T : class, new()
        {
            CurPage<T> cp = new CurPage<T>();
            
            var list = GetList<T>(Table,PageNum,PageSize, OrderBy, lists);
            cp.rows = list;
            cp.total = Count("select COUNT(*) from " + Table + " as _c_t ", lists);
            return cp;

        }

        public CurPage<T> GetCurPage<T>(Expression<Func<T, bool>> exp, int PageNum, int PageSize, string OrderBy) where T : class, new()
        {
            string sql = "select * from " + typeof(T).Name + " with(READPAST)";
            var lists = new List<DataItemType>();
            if (exp != null)
            {
                var lr = new Lambda.LambdaRouter();
                var str = lr.ExpressionRouter(exp);
                sql += (str.Length > 2 ? (" where " + str) : "");
                lists = lr.Parameters;
            }

            CurPage<T> cp = new CurPage<T>();
            var Table = "(" + sql + ")";
            var list = GetList<T>(Table, PageNum, PageSize, OrderBy, lists);
            cp.rows = list;
            cp.total = Count("select COUNT(*) from " + Table + " as _c_t ", lists);
            return cp;

        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="Table">表或者带()的sql语句</param>
        /// <param name="PageSize">每页数</param>
        /// <param name="PageNum">当前页</param>
        /// <param name="OrderBy">排序（不带order by字符）</param>
        /// <param name="lists">条件参数值</param>
        /// <returns></returns>
        public ICurPage GetCurPage(string Table, int PageNum, int PageSize, string OrderBy, List<DataItemType> lists = null)
        {
            CurPage cp = new CurPage();
            var list = GetList(Table, PageNum, PageSize, OrderBy, lists); ;
            cp.rows = list;
            cp.total = Count("select COUNT(*) from " + Table + " as _c_t", lists);
            return cp;

        }

        /// <summary>
        /// 执行SQL或者存储过程并返回受影响的行数
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string cmdText,  List<DataItemType> ldt=null , CommandType cmdType = CommandType.Text)
        {
            return DataConnect.get(key, Config).ExecuteNonQuery(cmdText, ldt, cmdType);
        }
        /// <summary>
        /// 执行多条sql语句或者存储过程
        /// </summary>
        /// <param name="cmdTexts">存储过程或者sql,不能有参数的语句，否则发生错误</param>
        /// <param name="ldt">这个目前必须是null</param>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string[] cmdTexts,  List<DataItemType> ldt=null , CommandType cmdType = CommandType.Text)
        {
            return DataConnect.get(key, Config).ExecuteNonQuery(cmdTexts, ldt, cmdType);
        }

        /// <summary>
        /// 返回首行首列的值（object）
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        public object ExecuteScalar(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            return DataConnect.get(key, Config).ExecuteScalar(cmdText,  ldt, cmdType);
        }


        /// <summary>
        /// 执行查询并返回数据集(DataTable)
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        public DataTable ExecQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            return DataConnect.get(key, Config).ExecQuery(cmdText, ldt, cmdType);
        }

        public List<T> Select<T>(Expression<Func<T, bool>> exp=null) where T:class,new()
        {
            string sql = "select * from " + typeof(T).Name + " with(READPAST)";
            var list = new List<DataItemType>();
            if (exp !=null)
            {
                var lr = new Lambda.LambdaRouter();
                var str = lr.ExpressionRouter(exp);
                sql += (str.Length > 2 ? (" where " + str) : "");
                list = lr.Parameters;
            }
            return GetList<T>(sql, list);
        }

        public T Single<T>(Expression<Func<T, bool>> exp = null) where T : class, new()
        {
            var lr = LambdaHelper.GetHelper<T>(SqlType);
            lr.Select().Take(1).Where(exp);
            return GetInfo<T>(lr.Res.SQL, lr.Res.Para);

        }

        //获取有记录条数
        public int Count<T>(Expression<Func<T, bool>> exp = null) where T : class, new()
        {

            var lr = LambdaHelper.GetHelper<T>(SqlType);
            lr.Count().Where(exp);
            return Count(lr.Res.SQL, lr.Res.Para);

        }

        public bool Delete<T>(Expression<Func<T, bool>> exp = null) where T : class, new()
        {

            var lr = LambdaHelper.GetHelper<T>(SqlType);
            lr.Delete().Where(exp);
            return ExecuteNonQuery(lr.Res.SQL, lr.Res.Para)>0;
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setexp">set的表达式</param>
        /// <param name="whereExp">条件</param>
        /// <returns></returns>
        public bool Update<T>(Expression<Func<T, bool>> setexp, Expression<Func<T, bool>> whereExp=null) where T : class, new()
        {

            var lr = LambdaHelper.GetHelper<T>(SqlType);
            lr.Update(setexp).Where(whereExp);
            return ExecuteNonQuery(lr.Res.SQL, lr.Res.Para) > 0;
            //return true;

        }


    }


}
