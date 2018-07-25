using CC.DataAccess.Extend;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Framework.DAL
{
    /// <summary>
    /// sql对象
    /// </summary>
    public class SqlObj
    {
        /// <summary>
        /// sql名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 默认参数
        /// </summary>
        public HashObject DefaultParameters { get; set; }

        /// <summary>
        /// sql语句
        /// </summary>
        public string SqlText { get; set; }

        /// <summary>
        /// 读写类型
        /// </summary>
        public DBrwType DBrwType  { get; set; }
    }
}
