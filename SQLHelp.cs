using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// sql数据库操作(不允许继承和初始化)，使用SQLHelp.IHelp()来初始化;
    /// </summary>
    public sealed class SQLHelp:IData
    {

        static string connectionstring = null;
        bool isdispose = false;
        private SqlConnection _conn;

        private static SQLHelp shelp = null;
        static readonly object padlock = new object();


        public static SQLHelp IHelp(string _connectionstring)
        {

            lock (padlock)
            {
                if (shelp == null)
                {
                    shelp = new SQLHelp(_connectionstring);
                }
                return shelp;
            }
        }
        public SQLHelp(string _connectionstring)
        {
            connectionstring = _connectionstring;
        }
        
        /// <summary>
        /// 数据库连接
        /// </summary>
        public string ConnectionString
        {
            get { return connectionstring; }
        }
        /// <summary>
        /// 准备参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="conn"></param>
        /// <param name="trans"></param>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        private void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
        {

            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }



        /// <summary>
        /// 数据库连接
        /// </summary>
        public DbConnection Conn
        {
            get { if (_conn==null)
                _conn=new SqlConnection(connectionstring);
            return (DbConnection)(_conn);
        }
        }
        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <param name="filename">备份的路径和名称</param>
        public bool BackUp(string dbname, string filename)
        {
            string sql = "backup database @dbname to disk=@filename";
            SqlParameter[] sp =new SqlParameter[]{ 
                new SqlParameter("@dbname",SqlDbType.VarChar),
                new SqlParameter("@filename",SqlDbType.VarChar)
            };
            sp[0].Value = dbname;
            sp[1].Value = filename;
            try
            {
                ExecuteNonQuery(sql, CommandType.Text, sp);
                return true;
            }
            catch
            {
                return false;
            }
            
        }
        /// <summary>
        /// 恢复数据库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <param name="filename">要恢复的备份数据库的路径和名称</param>
        public bool Recoverd(string dbname, string filename)
        {
            string sql = "use master restore database @dbname from disk=@filename";
            SqlParameter[] sp = new SqlParameter[]{ 
                new SqlParameter("@dbname",SqlDbType.VarChar),
                new SqlParameter("@filename",SqlDbType.VarChar)
            };
            sp[0].Value = dbname;
            sp[1].Value = filename;
            try
            {
                ExecuteNonQuery( sql, CommandType.Text, sp);
                return true;
            }
            catch
            {
                return false;
            }

        }
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <returns></returns>
        public void OpenData()
        {
            if (Conn.State != ConnectionState.Open)
            {
                try
                {
                    Conn.Open();
                }
                catch
                {
                    CloseData();
                    
                }
            }
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <returns></returns>
        public void CloseData()
        {
            if (Conn.State == ConnectionState.Open)
            {
                try
                {
                    Conn.Close();
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// 析构函数
        /// </summary>
        ~SQLHelp()
        {
            Dispose(!isdispose);
        }
        /// <summary>
        /// 释放类所占用的资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!isdispose)
            {
                if (disposing)
                {
                    if (Conn.State == ConnectionState.Open)
                    {
                        CloseData();
                    }
                }
            }
            if (Conn != null)
            {
                Conn.Dispose();
            }
            isdispose = true;
        }

        int ExecuteNonQuery(string cmdText, CommandType cmdType= CommandType.Text, SqlParameter[] commandParameters=null)
        {
            int val = 0;
            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    var tran = conn.BeginTransaction();
                    PrepareCommand(cmd, conn, tran, cmdType, cmdText, commandParameters);
                    try
                    {
                        val = cmd.ExecuteNonQuery();
                        tran.Commit();
                    }
                    catch 
                    {
                        tran.Rollback();
                        val = 0;
                    }
                    finally
                    {
                        tran.Dispose();
                    }
                }
                else
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                    val = cmd.ExecuteNonQuery();
                }
                cmd.Parameters.Clear();
                return val;
            }
        }


        #region IData 成员

        /// <summary>
        /// 配置参数
        /// </summary>
        /// <param name="ldt">包含的参数</param>
        /// <returns></returns>
        public DbParameter[] SetDbParameter(List<DataItemType> ldt)
        {
            SqlParameter[] sp = null;
            if (ldt != null && ldt.Count > 0)
            {
                sp = new SqlParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue==null)
                    {
                        sp[i] = new SqlParameter();//, ldt[i].DbType, 
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new SqlParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
                    }
                }
            }
            return sp;
        }
        #endregion
        #region IData 成员

        public IDataParameter[] SetDbParameter(IList<DataItemType> ldt)
        {
            SqlParameter[] sp = null;
            if (ldt != null && ldt.Count > 0)
            {
                sp = new SqlParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue == null)
                    {
                        sp[i] = new SqlParameter();
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new SqlParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
                    }
                }
            }
            return sp;
        }

        public int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            return ExecuteNonQuery(cmdText, cmdType, (SqlParameter[])SetDbParameter(ldt));
            
        }

        public List<DataItemType> ExecuteNonQuery(string cmdText, List<DataItemType> ldt, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    var tran = conn.BeginTransaction();
                    PrepareCommand(cmd, conn, tran, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                    try
                    {
                        val = cmd.ExecuteNonQuery();
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        val = 0;
                    }
                    finally
                    {
                        tran.Dispose();
                    }
                }
                else
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                    val = cmd.ExecuteNonQuery();
                }
                if (val > 0)
                {
                    for (int i = 0; i < ldt.Count; i++)
                    {
                        if (ldt[i].DBParameterDirection != ParameterDirection.Input)
                        {
                            ldt[i].DataItemValue = cmd.Parameters[ldt[i].DataItemName].Value;
                        }
                    }
                }
                cmd.Parameters.Clear();
                return ldt;
            }
        }

        public IDataReader ExecuteIReader(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn = new SqlConnection(connectionstring);
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        public object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                cmd.Dispose();
                return val;
            }
        }

        public DataTable ExecQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                cmd.Parameters.Clear();
                sda.Dispose();
                cmd.Dispose();
                return dt;
            }
        }
        #endregion
    }
}
