using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Web;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// 非sql的数据库操作(不允许继承和初始化)，使用OledbHelp.IHelp()来初始化;
    /// </summary>
    public sealed class OledbHelp:IData
    {
        static string connectionstring = null;
        bool isdispose = false;
        private OleDbConnection _conn;

        private static OledbHelp ohelp = null;
        static readonly object padlock = new object();


        public static OledbHelp IHelp(string _connectionstring)
        {

            lock (padlock)
            {
                if (ohelp == null)
                {
                    ohelp = new OledbHelp(_connectionstring);
                }
                return ohelp;
            }
        }
        public OledbHelp(string _connectionstring)
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
        private void PrepareCommand(OleDbCommand cmd, OleDbConnection conn, OleDbTransaction trans, CommandType cmdType, string cmdText, OleDbParameter[] cmdParms)
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
                foreach (OleDbParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        /// <summary>
        /// 数据库连接
        /// </summary>
        public DbConnection Conn
        {
            get { if (_conn==null)
                _conn = new OleDbConnection(connectionstring);
            return (DbConnection)(_conn);
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
                    //conn = new SqlConnection();
                }
            }
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="dbname">数据库绝对路径</param>
        /// <param name="filename">备份的路径和名称</param>
        public bool BackUp(string dbname, string filename)
        {
            if (File.Exists(dbname))
            {
                try
                {
                    File.Copy(dbname, filename,true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }
        /// <summary>
        /// 恢复数据库
        /// </summary>
        /// <param name="dbname">数据库绝对路径</param>
        /// <param name="filename">要恢复的备份数据库的路径和名称</param>
        public bool Recoverd(string dbname, string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    File.Copy(filename, dbname, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~OledbHelp()
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

        #region IData 成员

        /// <summary>
        /// 配置参数
        /// </summary>
        /// <param name="ldt">包含的参数</param>
        /// <returns></returns>
        public DbParameter[] SetDbParameter(List<DataItemType> ldt)
        {
            OleDbParameter[] sp = null;
            if (ldt!=null && ldt.Count > 0)
            {
                sp = new OleDbParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue==null)
                    {
                        sp[i] = new OleDbParameter();//, ldt[i].DbType, 
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new OleDbParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
                    }
                }
            }
            return sp;
            //throw new NotImplementedException();
        }

        #endregion

        #region IData 成员


        public IDataParameter[] SetDbParameter(IList<DataItemType> ldt)
        {
            OleDbParameter[] sp = null;
            if (ldt != null && ldt.Count > 0)
            {
                sp = new OleDbParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue == null)
                    {
                        sp[i] = new OleDbParameter();//, ldt[i].DbType, 
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new OleDbParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
                    }
                }
            }
            return sp;
        }

        public int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            int val = 0;
            using (OleDbConnection conn = new OleDbConnection(connectionstring))
            {
                OleDbCommand cmd = new OleDbCommand();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    var tran = conn.BeginTransaction();
                    PrepareCommand(cmd, conn, tran, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
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
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
                    val = cmd.ExecuteNonQuery();
                }
                cmd.Parameters.Clear();
                return val;
            }
        }

        public List<DataItemType> ExecuteNonQuery(string cmdText, List<DataItemType> ldt, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (OleDbConnection conn = new OleDbConnection(connectionstring))
            {
                OleDbCommand cmd = new OleDbCommand();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    var tran = conn.BeginTransaction();
                    PrepareCommand(cmd, conn, tran, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
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
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
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
            OleDbCommand cmd = new OleDbCommand();
            OleDbConnection conn = new OleDbConnection(connectionstring);
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
                OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                return null;
            }
        }

        public object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            using (OleDbConnection connection = new OleDbConnection(connectionstring))
            {
                OleDbCommand cmd = new OleDbCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                cmd.Dispose();
                return val;
            }
        }

        public DataTable ExecQuery(string cmdText, CommandType cmdType = CommandType.Text, List<DataItemType> ldt = null)
        {
            using (OleDbConnection connection = new OleDbConnection(connectionstring))
            {
                OleDbCommand cmd = new OleDbCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (OleDbParameter[])SetDbParameter(ldt));
                OleDbDataAdapter sda = new OleDbDataAdapter(cmd);
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
