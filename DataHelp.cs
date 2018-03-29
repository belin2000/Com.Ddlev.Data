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

        public static DataHelp GetDataHelpget(string key, Config config =null)
        {
            if (DataHelpDic.ContainsKey(key))
            {
                var d = DataHelpDic[key];
                return d;
            }
            else
            {
                var dhelp = new DataHelp(key, config);
                AddNew(key, dhelp);
                return dhelp;
            }
        }
        /// <summary>
        /// 添加新的连接
        /// </summary>
        /// <param name="key"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static bool AddNew(string key, DataHelp d)
        {
            var isok = false;
            try
            {
                lock (DataHelpType)
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
                lock (DataHelpType)
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
            }
            finally
            {
                isready = true;
            }
            return isok;
        }

        public DataHelp(string _key, Config _config = null)
        {
            this.key = _key;
            this.Config = _config;
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
        public bool Add(object t, string[] dir)
        {
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
                list.Add(new DataItemType("@" + pi.Name, pi.GetValue(t, null)));
                coms += "["+pi.Name + "],";
                values += "@" + pi.Name + ",";
            }
            coms = coms.Substring(0, coms.Length - 1);
            values = values.Substring(0, values.Length - 1);
            using (IData data = DataConnect.get(key, Config))
            {
                ok = data.ExecuteNonQuery("insert into " + t.GetType().Name + "(" + coms + ") values(" + values + ")", CommandType.Text, list);
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
            PropertyInfo[] pis = t.GetType().GetProperties();
            List<DataItemType> list = new List<DataItemType>();
            string coms = "";
            string values = "";
            foreach (PropertyInfo pi in pis)
            {
                if (dir != null)
                {
                    if (dir.Contains(pi.Name))
                    {
                        continue;
                    }
                }
                if (pi.Name.ToLower() == idname.ToLower())
                {
                    list.Add(new DataItemType("@" + idname, ParameterDirection.Output, DbType.Int32,8));
                }
                else
                {
                    list.Add(new DataItemType("@" + pi.Name, pi.GetValue(t, null)));
                    coms += "["+pi.Name + "],";
                    values += "@" + pi.Name + ",";
                }
            }
            coms = coms.Substring(0, coms.Length - 1);
            values = values.Substring(0, values.Length - 1);
            using (IData data = DataConnect.get(key, Config))
            {
                list = data.ExecuteNonQuery( "insert into " + t.GetType().Name + "(" + coms + ") values(" + values + ");select @" + idname + "=SCOPE_IDENTITY()", list, CommandType.Text);
            }

            return Convert.ToInt32(list.Find(m => m.DataItemName == "@" + idname).DataItemValue);
        }


        /// <summary>
        /// 修改数据
        /// </summary>
        /// <param name="t">类对象</param>
        /// <param name="dir">用作对象修改的数据,做where条件</param>
        /// <returns></returns>
        public bool Edit(object t, string[] dir)
        {
            int ok = 0;
            PropertyInfo[] pis = t.GetType().GetProperties();
            List<DataItemType> list = new List<DataItemType>();
            string coms = "";
            string where=" 1=1 ";
            foreach (PropertyInfo pi in pis)
            {
                list.Add(new DataItemType("@" + pi.Name, pi.GetValue(t, null)));
                if (dir.Contains(pi.Name))
                {
                    where +=" and ["+ pi.Name + "]=@" + pi.Name ;
                }
                else
                {
                    coms +="["+ pi.Name + "]=@" + pi.Name + ",";
                }
            }
            coms = coms.Substring(0, coms.Length - 1);

            using (IData data = DataConnect.get(key, Config))
            {
                ok = data.ExecuteNonQuery("update " + t.GetType().Name+" set "+coms +" where "+where, CommandType.Text, list);
            }

            return ok > 0 ? true : false;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="t">类名字</param>
        /// <param name="dic">数据集合,类的属性</param>
        /// <returns></returns>
        public bool Delete(string t, Dictionary<string, string> dic)
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
                ok = data.ExecuteNonQuery( "delete  from "+t+" where " + where, CommandType.Text, list);
            }

            return ok > 0 ? true : false;
        }
        /// <summary>
        /// 获取相关的信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table">表名或者 (sql) 语句:括号sql语句括号</param>
        /// <param name="dic"></param>
        /// <returns></returns>
        public T GetInfo<T>(string table, Dictionary<string, string> dic) where T:new()
        {
            var t = new T();

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
            using (IData data = DataConnect.get(key, Config))
            {
                IDataReader dr = data.ExecuteIReader("select *  from " + table + " as T_ where " + where, CommandType.Text, list);
                if (dr.Read())
                {
                    ReaderToObject(dr, t);
                }
                dr.Close();
                dr.Dispose();
            }

            return t;
        }
        /// <summary>
        /// 返回一条记录的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句，可包含@参数</param>
        /// <param name="list">对应@参数和值的集合</param>
        /// <returns></returns>
        public T GetInfo<T>(string sql, List<DataItemType> list=null) where T : new()
        {
            var t = new T();
            using (IData data = DataConnect.get(key, Config))
            {
                IDataReader dr = data.ExecuteIReader(sql, CommandType.Text, list);
                if (dr.Read())
                {
                    ReaderToObject(dr, t);
                }
                dr.Close();
                dr.Dispose();
            }

            return t;
        }
        /// <summary>
        /// 返回一组记录的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">Sql语句 或者表名字，sql可包含@参数</param>
        /// <param name="list">对应@参数和值的集合</param>
        /// <returns></returns>
        public List<T> GetList<T>(string sql, List<DataItemType> list=null) where T : new()
        {

            List<T> listT = new List<T>();
            using (IData data = DataConnect.get(key, Config))
            {
                IDataReader dr = data.ExecuteIReader(sql, CommandType.Text, list);
                listT = (List<T>)ReaderToObject<T>(dr);
                dr.Close();
                dr.Dispose();
            }
            return listT;
            
        }
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
        /// 分页
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="Table">表名或者用（sql）组成的表</param>
        /// <param name="PageSize">每页的条数</param>
        /// <param name="PageNum">单前页,必须大于0</param>
        /// <param name="com">用于排序的列</param>
        /// <param name="desc">desc/asc(倒序或者顺序)</param>
        /// <returns></returns>
        public List<T> GetList<T>(string Table, int PageSize, int PageNum,string com, string desc, List<DataItemType> list = null) where T : new()
        {
            string sql = "select top " + PageSize + " * from (select  ROW_NUMBER() OVER(ORDER BY  " + com +" "+ desc + ") AS R_row_num ,* from " + Table + " as t_able) as a_able where R_row_num>" + ((PageNum - 1) * PageSize).ToString();
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
        public List<T> GetList<T>(string Table, int PageSize, int PageNum, string OrderBy, List<DataItemType> list=null) where T : new()
        {
            string sql = "select top " + PageSize + " * from (select  ROW_NUMBER() OVER(ORDER BY  " + OrderBy + ") AS R_row_num ,* from " + Table + " as t_able) as a_able where R_row_num>" + ((PageNum - 1) * PageSize).ToString();
            return GetList<T>(sql, list);
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
            string sql = " select count(*) from " + table + " as _t " + where;
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
                count = Convert.ToInt32(data.ExecuteScalar(sql, CommandType.Text, list));
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

        public CurPage GetCurPage<T>(string q, int page, int rows, string tablename, string orderby,List<DataItemType> lists=null) where T : new()
        {
            CurPage cp = new CurPage();
            List<T> list = new List<T>();
            string sql = "select * from " + tablename + "";
            if (!string.IsNullOrEmpty(q) && q != "")
            {
                sql += " where " + q;
            }
            list = GetList<T>("(" + sql + ")", rows, page, orderby, lists);
            cp.rows = list;
            cp.total = Count("(" + sql + ")", lists);
            return cp;
        }

        public CurPage GetCurPage<T>(string Table, int PageSize, int PageNum, string orderby, List<DataItemType> lists = null) where T : new()
        {
            CurPage cp = new CurPage();
            DataTable dt = new DataTable();
            string sql = "select top " + PageSize + " * from (select  ROW_NUMBER() OVER(ORDER BY  "+ orderby + ") AS R_row_num ,* from " + Table + " as t_able) as a_able where R_row_num>" + ((PageNum - 1) * PageSize).ToString();
            var list = GetList<T>("(" + sql + ")", PageSize, PageNum, orderby,lists);
            cp.rows = list;
            cp.total = Count("(" + sql + ")", lists);
            return cp;

        }

        /// <summary>
        /// 执行SQL或者存储过程并返回受影响的行数
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            return DataConnect.get(key, Config).ExecuteNonQuery(cmdText, cmdType, ldt);
        }
        public List<DataItemType> ExecuteNonQuery(string cmdText,  List<DataItemType> ldt , CommandType cmdType = CommandType.Text)
        {
            return DataConnect.get(key, Config).ExecuteNonQuery(cmdText, ldt, cmdType);
        }

        /// <summary>
        /// 返回首行首列的值（object）
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        public object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            return DataConnect.get(key, Config).ExecuteScalar(cmdText, cmdType, ldt);
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
            return DataConnect.get(key, Config).ExecQuery(cmdText, cmdType, ldt);
        }

    }
}
