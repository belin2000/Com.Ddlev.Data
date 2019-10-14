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
    /// sql数据库操作;
    /// </summary>
    public sealed class SQLHelp:IData
    {
        string connectionstring = null;
        bool isdispose = false;
        private SqlConnection _conn;

        static readonly object padlock = new object();

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
        private void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms=null)
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
                ExecuteNonQuery(sql,sp, CommandType.Text);
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
                ExecuteNonQuery( sql,sp, CommandType.Text);
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
        /// <summary>
        /// 单条sql语句执行（如果是语句，则使用sql版的存储过程执行）
        /// </summary>
        /// <param name="cmdText">单条sql或者多条SQl在一起</param>
        /// <param name="cmdType"></param>
        /// <param name="commandParameters"></param>
        /// <returns></returns>
        private int ExecuteNonQuery(string cmdText,  SqlParameter[] commandParameters=null, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    cmdText = @"SET XACT_ABORT OFF
                    BEGIN TRY
                    BEGIN TRAN  
                    "+cmdText+ @"
                    COMMIT TRAN
                    END TRY  
                    BEGIN CATCH  
                    THROW
                    ROLLBACK  TRAN 
                    END CATCH  
                    ";
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                    try
                    {
                        val = cmd.ExecuteNonQuery();
                        val = 1;
                    }
                    catch(SqlException er)
                    {
                        val = 0;
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
                        var pik=new object() ;
                        if (ldt[i].DataItemValue.GetType().IsEnum)
                        {
                            pik = (int)ldt[i].DataItemValue;
                        }
                        else
                        {
                            pik = ldt[i].DataItemValue;
                        }
                        sp[i] = new SqlParameter(ldt[i].DataItemName, pik);
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

        /// <summary>
        /// 执行sql（用事务的方式）
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="ldt"></param>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string cmdText, List<DataItemType> ldt, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                if (cmdType == CommandType.Text) //使用sql语句时候，使用事务
                {
                    cmdText = @"SET XACT_ABORT OFF
                    BEGIN TRY
                    BEGIN TRAN  
                    " + cmdText + @"
                    COMMIT TRAN
                    END TRY  
                    BEGIN CATCH 
                    THROW 
                    ROLLBACK TRAN
                    END CATCH  
                    ";

                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                    try
                    {
                        val = cmd.ExecuteNonQuery();
                        val = 1;
                    }
                    catch (SqlException ex)
                    {
                        val = 0;
                    }

                }
                else
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, (SqlParameter[])SetDbParameter(ldt));
                    val = cmd.ExecuteNonQuery();
                }
                if (val > 0 && ldt!=null && ldt.Count>0)
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

        public object ExecuteScalar(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
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

        public DataTable ExecQuery(string cmdText, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
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
        /// <summary>
        /// 分开执行多条sql,如果有一条错误，全部回滚，如果是存储过程，则存储过程里面不能含有事务
        /// </summary>
        /// <param name="cmdTexts"></param>
        /// <param name="ldt"></param>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string[] cmdTexts, List<DataItemType> ldt = null, CommandType cmdType = CommandType.Text)
        {
            int val = 0;
            List<int> lit = new List<int>();
            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                SqlCommand cmd = new SqlCommand();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                var tran= conn.BeginTransaction();
                try {
                    foreach (var cmdText in cmdTexts)
                    {
                        PrepareCommand(cmd, conn, tran, cmdType, cmdText);
                        lit.Add(cmd.ExecuteNonQuery());
                    }
                    if (lit.Exists(m => m == 0))
                    {
                        tran.Rollback();
                        val = 0;
                    }
                    else
                    {
                        tran.Commit();
                        val = 1;
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    val = 0;
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
