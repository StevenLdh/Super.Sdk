using CC.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Framework.DAL
{
    /// <summary>
    /// 连接配置
    /// </summary>
    class ConnectionConfig
    {
        /// <summary>
        /// ConnectionConfig表ID
        /// </summary>
        public int DBID { get; set; }

        /// <summary>
        /// 分区ID（几号分区）
        /// </summary>
        public int ShardID { get; set; }

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DBName { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DBType   DBType { get; set; }

        /// <summary>
        /// 读写类型，1--读，2--写
        /// </summary>
        public int ReadWriteType { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        ///// <summary>
        ///// 分区列名称（为空则为单库）
        ///// </summary>
        //public string ShardColName { get; set; }

        ///// <summary>
        ///// 分区表名称
        ///// </summary>
        //public string ShardTable { get; set; }

        /// <summary>
        /// 是否开启日志记录
        /// </summary>
        public bool LogEnable { get; set; }

        /// <summary>
        /// 状态 ,0 -- 关闭,1--开启
        /// </summary>
        public int Status { get; set; }

      

        //private static Dictionary<string, List<ConnectionConfig>> ReadFromConfig()
        //{
        //    Dictionary<string, List<ConnectionConfig>> dic = new Dictionary<string, List<ConnectionConfig>>();
        //    if (ConfigurationManager.ConnectionStrings.Count > 0)
        //    {
        //        foreach (ConnectionStringSettings cs in ConfigurationManager.ConnectionStrings)
        //        {
        //            string name = cs.Name.ToLower();
        //            if (name != "cc.globalconnstring")
        //            {
        //                ConnectionConfig config = new ConnectionConfig();

                        

        //                config.ShardID = 1;
        //                config.DBName = name;
        //                config.DBType = (DBType)Enum.Parse(typeof(DBType), cs.ProviderName);
        //                config.ReadWriteType =1;
        //                config.ConnectionString = cs.ConnectionString;
        //                config.LogEnable = false;

        //                //dic.Add(name, config);

        //            }
        //        }
        //    }

        //}

        ///// <summary>
        ///// 从global 数据库读取配置
        ///// </summary>
        ///// <returns></returns>
        //private static Dictionary<string, List<ConnectionConfig>> ReadFromGlobalDB()
        //{
           
             
        //}
    }
}
