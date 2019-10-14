using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// MySql的数据库操作;
    /// </summary>
    public sealed class MySQLHelp:IData
    {
        string connectionstring = null;
        bool isdispose = false;
        private MySqlConnection _conn;
        static readonly object padlock = new object();

        public MySQLHelp(string _connectionstring)
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
        private void PrepareCommand(MySqlCommand cmd, MySqlConnection conn, MySqlTransaction trans, CommandType cmdType, string cmdText, MySqlParameter[] cmdParms=null)
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
                foreach (MySqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        /// <summary>
        /// 数据库连接
        /// </summary>
        public DbConnection Conn
        {
            get { if (_conn==null)
                _conn = new MySqlConnection(connectionstring);
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
        ~MySQLHelp()
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
            MySqlParameter[] sp = null;
            if (ldt!=null && ldt.Count > 0)
            {
                sp = new MySqlParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue==null)
                    {
                        sp[i] = new MySqlParameter();//, ldt[i].DbType, 
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new MySqlParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
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
            MySqlParameter[] sp = null;
            if (ldt != null && ldt.Count > 0)
            {
                sp = new MySqlParameter[ldt.Count];
                for (int i = 0; i < ldt.Count; i++)
                {
                    if (ldt[i].DataItemValue is DBNull || ldt[i].DataItemValue == null)
                    {
                        sp[i] = new MySqlParameter();//, ldt[i].DbType, 
                        sp[i].ParameterName = ldt[i].DataItemName;
                        sp[i].Size = ldt[i].SizeLength;
                        sp[i].DbType = ldt[i].DbType;
                        sp[i].Direction = ldt[i].DBParameterDirection;
                    }
                    else
                    {
                        sp[i] = new MySqlParameter(ldt[i].DataItemName, ldt[i].DataItemValue);
                    }
                }
            }
            return sp;
        }
        public int ExecuteNonQuery(string cmdText, List<DataItemType> ldt=null, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (MySqlConnection conn = new MySqlConnection(connectionstring))
            {
                MySqlCommand cmd = new MySqlCommand();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    var tran = conn.BeginTransaction();
                    PrepareCommand(cmd, conn, tran, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
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
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
                    val = cmd.ExecuteNonQuery();
                }
                if (val > 0 && ldt!=null &&  ldt.Count>0)
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
                return val;
            }
        }

        public IDataReader ExecuteIReader(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlConnection conn = new MySqlConnection(connectionstring);
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
                MySqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                return null;
            }
        }

        public object ExecuteScalar(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionstring))
            {
                MySqlCommand cmd = new MySqlCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                cmd.Dispose();
                return val;
            }
        }

        public DataTable ExecQuery(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionstring))
            {
                MySqlCommand cmd = new MySqlCommand();
                PrepareCommand(cmd, connection, null, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
                MySqlDataAdapter sda = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                cmd.Parameters.Clear();
                sda.Dispose();
                cmd.Dispose();
                return dt;
            }
        }
        public int ExecuteNonQuery(string[] cmdTexts, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (MySqlConnection conn = new MySqlConnection(connectionstring))
            {
                MySqlCommand cmd = new MySqlCommand();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                var tran = conn.BeginTransaction();
                try
                {
                    foreach (var cmdText in cmdTexts)
                    {
                        PrepareCommand(cmd, conn, tran, cmdType, cmdText, (MySqlParameter[])SetDbParameter(ldt));
                        cmd.ExecuteNonQuery();
                    }
                
                    tran.Commit();
                    val = 1;
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

                if (val > 0 && ldt != null && ldt.Count > 0)
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
                return val;
            }
        }

        #endregion
    }
}
