using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// 数据库接口(对继承该接口的类使用单件模式)
    /// </summary>
    public interface IData : IDisposable
    {
        /// <summary>
        /// 连接的字符串
        /// </summary>
        string ConnectionString
        {
            get;
        }
        /// <summary>
        /// 数据库连接
        /// </summary>
        DbConnection Conn
        {
            get;
        }
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <returns></returns>
        void OpenData();
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <returns></returns>
        void CloseData();
        /// <summary>
        /// 执行SQL或者存储过程并返回受影响的行数
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        int ExecuteNonQuery( string cmdText, CommandType cmdType= CommandType.Text, List<DataItemType> ldt=null);

        /// <summary>
        /// 执行SQL或者存储过程并指定返回值的集合
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        List<DataItemType> ExecuteNonQuery(string cmdText, List<DataItemType> ldt, CommandType cmdType= CommandType.Text);

        /// <summary>
        /// 获取一个IDataReader
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns>IDataReader要手动关闭</returns>
        IDataReader ExecuteIReader(string cmdText, CommandType cmdType= CommandType.Text, List<DataItemType> ldt=null);

        /// <summary>
        /// 返回首行首列的值（object）
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        object ExecuteScalar( string cmdText, CommandType cmdType= CommandType.Text, List<DataItemType> ldt=null);
        /// <summary>
        /// 执行查询并返回数据集(DataTable)
        /// </summary>
        /// <param name="cmdType">sql文本还是存储过程</param>
        /// <param name="cmdText">sql语句或者存储过程的名称</param>
        /// <param name="ldt">参数集合</param>
        /// <returns></returns>
        DataTable ExecQuery( string cmdText, CommandType cmdType= CommandType.Text, List<DataItemType> ldt=null);
        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <param name="filename">备份的路径和名称</param>
        bool BackUp(string dbname, string filename);
        /// <summary>
        /// 恢复数据库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <param name="filename">要恢复的备份数据库的路径和名称</param>
        bool Recoverd(string dbname, string filename);

        /// <summary>
        /// 设置成员
        /// </summary>
        /// <param name="ldt"></param>
        /// <returns></returns>
        DbParameter[] SetDbParameter(List<DataItemType> ldt);

        /// <summary>
        /// 设置成员
        /// </summary>
        /// <param name="ldt"></param>
        /// <returns></returns>
        IDataParameter[] SetDbParameter(IList<DataItemType> ldt);
    }


}
