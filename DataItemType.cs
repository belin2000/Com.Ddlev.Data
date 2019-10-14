using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Ddlev.Data
{
    /// <summary>
    /// 对应存贮过程的数据类型
    /// </summary>
    public class DataItemType:IDataPara
    {
        public string DataItemName
        {
            set; get;
        }

        /// <summary>
        /// 变量对应的值
        /// </summary>
        public object DataItemValue
        {
            set; get;
        }

        /// <summary>
        /// 参数的类型（output）
        /// </summary>
        public ParameterDirection DBParameterDirection
        {
            set; get;
        }
        /// <summary>
        /// 指定数据库类型
        /// </summary>
        public DbType DbType
        {
            set; get;
        }
        /// <summary>
        /// 参数类型的长度(output 参数必填)
        /// </summary>
        public int SizeLength
        {
            set; get;
        }

        public DataItemType() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="DataItemName_">对应存贮过程的变量(如"@ID")</param>
        /// <param name="DataValue_">变量对应的值</param>
        public DataItemType(string DataItemName_, dynamic DataItemValue_)
        {
            this.DataItemName = DataItemName_;
            this.DataItemValue = DataItemValue_;
            this.DBParameterDirection = ParameterDirection.Input;
        }
        /// <summary>
        /// 有output参数使用
        /// </summary>
        /// <param name="DataItemName_">output参数或者return参数</param>
        /// <param name="DBParameterDirection_">参数类型(output)</param>
        /// <param name="DbType_">类型</param>
        /// <param name="size_">类型的长度 (int类型为4)</param>
        public DataItemType(string DataItemName_, ParameterDirection DBParameterDirection_, DbType DbType_, int size_)
        {
            this.DataItemName = DataItemName_;
            this.DBParameterDirection = DBParameterDirection_;
            this.DbType = DbType_;
            this.SizeLength = size_;
        }

    }
}
