using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Framework.DAL
{

    public class DALSection : ConfigurationSection
    {
        /// <summary>
        /// core 数据库及分片相关
        /// </summary>
        [ConfigurationProperty("core", IsRequired = true)]
        public Core Core
        {
            get { return this["core"] as Core; }
            set { this["core"] = value; }
        }

        /// <summary>
        /// 数据库日志相关
        /// </summary>
        [ConfigurationProperty("dbLog")]
        public DbLog DbLog
        {
            get { return this["dbLog"] as DbLog; }
            set { this["dbLog"] = value; }
        }
        
        /// <summary>
        /// 是否允许客户端参数
        /// </summary>
        [ConfigurationProperty("allowUserVariables", DefaultValue = false)]
        public bool AllowUserVariables
        {
            get { return bool.Parse(this["allowUserVariables"] + ""); }
            set { this["allowUserVariables"] = value; }
        }
    }

    public class Core : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ShardDB();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            ShardDB shardDB = element as ShardDB;
            return shardDB.DbName;
        }


        /// <summary>
        /// 数据库日志监控频率（写入频率）,单位：秒 默认：1
        /// </summary>
        [ConfigurationProperty("connMonitorSeconds")]
        public int ConnMonitorSeconds
        {
            get
            {
                int result = Convert.ToInt32(this["connMonitorSeconds"]);
                return result == 0 ? 1 : result;
            }
            set { this["connMonitorSeconds"] = value; }
        }

        /// <summary>
        /// core 连接字符串
        /// </summary>
        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return this["connectionString"] as string; }
            set { this["connectionString"] = value; }
        }

        protected override string ElementName
        {
            get
            {
                return "shardDB";
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }
    }

    /// <summary>
    /// 采用分片的数据库
    /// </summary>
    public class ShardDB : ConfigurationElement
    {
        /// <summary>
        /// 数据库名称
        /// </summary>
        [ConfigurationProperty("dbName", IsRequired = true)]
        public string DbName
        {
            get { return this["dbName"] as string; }
            set { this["dbName"] = value; }
        }

        /// <summary>
        /// 分片字段
        /// </summary>
        [ConfigurationProperty("columnName", IsRequired = true)]
        public string ColumnName
        {
            get { return this["columnName"] as string; }
            set { this["columnName"] = value; }
        }

        /// <summary>
        /// 存储分区的数据库
        /// </summary>
        [ConfigurationProperty("routeTable", IsRequired = true)]
        public string RouteTable
        {
            get { return this["routeTable"] as string; }
            set { this["routeTable"] = value; }
        }


        /// <summary>
        /// 存储默认注册的分区
        /// </summary>
        [ConfigurationProperty("registerShard", IsRequired = false)]
        public int RegisterShard
        {
            get { return Convert.ToInt32(this["registerShard"]); }
            set { this["registerShard"] = value; }
        }
    }

    /// <summary>
    /// 数据库日志
    /// </summary>
    public class DbLog : ConfigurationElement
    {
        /// <summary>
        /// 数据库日志监控频率（写入频率）,单位：秒,默认1
        /// </summary>
        [ConfigurationProperty("dbLogMonitorSeconds")]
        public int DbLogMonitorSeconds
        {
            get
            {
                int result = Convert.ToInt32(this["dbLogMonitorSeconds"]);
                return result == 0 ? 1 : result;
            }
            set { this["dbLogMonitorSeconds"] = value; }
        }

        /// <summary>
        /// log数据库连接字符串
        /// </summary>
        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return this["connectionString"] as string; }
            set { this["connectionString"] = value; }
        }
    }




    /// <summary>
    /// 系统配置类
    /// </summary>
    public static class DALConfig
    {
        private static DALSection _dalConfig;

        /// <summary>
        /// Log数据库连接字符串
        /// </summary>
        public static string LogConnString
        {
            get
            {
                return _dalConfig.DbLog.ConnectionString;
            }
        }

        /// <summary>
        ///  Core数据库连接字符串
        /// </summary>
        public static string CoreConnString
        {
            get
            {
                return _dalConfig.Core.ConnectionString;
            }
        }


        /// <summary>
        /// 默认数据库监控的秒数
        /// </summary>
        public static int DBLogMonitorSeconds
        {
            get
            {
                return _dalConfig.DbLog.DbLogMonitorSeconds;
            }
        }

        /// <summary>
        /// 默认连接字符串监控的秒数 
        /// </summary>
        public static int GetRegisterShard(string dbName)
        {
            foreach (ShardDB shardDB in _dalConfig.Core)
            {
                if (dbName == shardDB.DbName)
                {
                    if (shardDB.RegisterShard == 0)
                        return 1;
                    return shardDB.RegisterShard;
                }
            }

            return 1;
        }


        /// <summary>
        /// 默认连接字符串监控的秒数 
        /// </summary>
        public static int ConnMonitorSeconds
        {
            get
            {
                return _dalConfig.Core.ConnMonitorSeconds;
            }
        }

        /// <summary>
        /// 是否允许客户端参数 
        /// </summary>
        public static bool AllowUserVariables
        {
            get
            {
                return _dalConfig.AllowUserVariables;
            }
        }


        static DALConfig()
        {
            _dalConfig = ConfigurationManager.GetSection("cc.com/cc.dal") as DALSection;
        }

        /// <summary>
        /// 获取某数据库的分区列名
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static string GetHostColName(string dbName)
        {
            foreach (ShardDB shardDB in _dalConfig.Core)
            {
                if (dbName == shardDB.DbName)
                    return shardDB.ColumnName;
            }

            return null;
        }

        /// <summary>
        /// 返回路由表
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static string GetRouteTable(string dbName)
        {
            foreach (ShardDB shardDB in _dalConfig.Core)
            {
                if (dbName == shardDB.DbName)
                    return shardDB.RouteTable;
            }

            return null;
        }
    }
}
