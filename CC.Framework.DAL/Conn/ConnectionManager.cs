using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using CC.Public;
using System.IO;
using System.Xml;
using CC.DataAccess;
using System.Threading;
using System.Data;
using System.Collections;
using CC.DataAccess.Extend;

namespace CC.Framework.DAL
{


    static class ConnectionManager
    {
        //key = 分区 - 数据库名称 - 读写
        private static Hashtable _hashConn = Hashtable.Synchronized(new Hashtable());
       // private static 
        private static Random _random = new Random();
        //private static Dictionary<string, int> _dicShard = new Dictionary<string, int>();
        //private static 
        //private static string _preCacheKey = "CC.ShardCache.";
        private static Thread _monitorThread;

        static ConnectionManager()
        {
            if (string.IsNullOrEmpty(DALConfig.CoreConnString))
            {
                throw new Exception("必须配置在web.config 中配置CC.DAL.CoreConn");
            }


            InitConn();
            //需要补充定时更新字符串逻辑

            /*  暂不开启监视
            _monitorThread = new Thread(new ThreadStart(MonitorConn));
            _monitorThread.Start();
             * */
        }

        ///// <summary>
        ///// 暂时放到conn.xml 中保存，未来可放入数据库
        ///// </summary>
        ///// <returns></returns>
        //private static List<ConnectionConfig> GetConnConfig()
        //{
        //    List<ConnectionConfig> list = new List<ConnectionConfig>();
        //    string filePath = AppDomain.CurrentDomain.BaseDirectory;//web程序默认在根目录，非WEB程序默认在bin\debug
        //    if (!filePath.EndsWith(@"\"))//非WEB程序默认不含\结束，
        //    {
        //        filePath = filePath + @"\";
        //    }
        //    filePath = filePath + "conn.xml";

        //    try
        //    {
        //        var sqlDoc = new XmlDocument();
        //        sqlDoc.Load(filePath);
        //        var sqlNodes = sqlDoc.DocumentElement.SelectNodes("//Root/DBConfig");
        //        foreach (XmlNode element in sqlNodes)
        //        {
        //            ConnectionConfig connConfig = new ConnectionConfig();
        //            connConfig.ShardID = Convert.ToInt32(element.Attributes["ShardID"].Value);
        //            connConfig.DBName = element.Attributes["DBName"].Value;
        //            connConfig.ReadWriteType = Convert.ToInt32(element.Attributes["ReadWriteType"].Value);
        //            connConfig.Status = Convert.ToInt32(element.Attributes["Status"].Value);
        //            connConfig.ConnectionString = element.InnerText;
        //            if (connConfig.Status == 1)//状态开启
        //            {
        //                list.Add(connConfig);
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        throw new Exception("conn.xml 初始化失败!");
        //    }
 
        //    return list;
        //}


        //private void init()
        //{
        //    //List<ConnectionConfig> connList = ConnectionConfig.GetConnectionConfig();
        //    //根据配置组装放入静态字典
        //    //foreach (ConnectionConfig config in connList)
        //    //{
        //    //    if (config.ReadWriteType == 1 || config.ReadWriteType == 3)
        //    //    {
        //    //        //分区 - 数据库名称 - 读写 例如：1_test_1
        //    //        string key = string.Format("{0}_{1}_1", config.ShardID, config.DBName).ToLower();
        //    //        List<string> list;
        //    //        if (!_dicConn.ContainsKey(key))
        //    //        {
        //    //            list = new List<string>();
        //    //            _dicConn.Add(key, list);
        //    //        }
        //    //        else
        //    //        {
        //    //            list = _dicConn[key];
        //    //        }
        //    //        list.Add(config.ConnectionString);
        //    //    }

        //    //    if (config.ReadWriteType == 2 || config.ReadWriteType == 3)
        //    //    {
        //    //        //分区 - 数据库名称 - 读写 例如：1_test_2
        //    //        string key = string.Format("{0}_{1}_2", config.ShardID, config.DBName).ToLower();
        //    //        List<string> list;
        //    //        if (!_dicConn.ContainsKey(key))
        //    //        {
        //    //            list = new List<string>();
        //    //            _dicConn.Add(key, list);
        //    //        }
        //    //        else
        //    //        {
        //    //            list = _dicConn[key];
        //    //        }
        //    //        list.Add(config.ConnectionString);
        //    //    }

        //    //}
        //}

        ///// <summary>
        ///// 单库根据库名、读写状态返回合理的连接字符串（来自memcached）
        ///// </summary>
        ///// <param name="dbName">数据库名称</param>
        ///// <param name="dbrwType">读写状态</param>
        ///// <returns></returns>
        //public static string GetConnectionString(string dbName, DBrwType dbrwType)
        //{
        //    //单库始终1号分区
        //    //根据库名、读写状态返回合理的连接字符串（来自memcached）
        //    return GetConnectionString(dbName, dbrwType, 0);
        //}


        /// <summary>
        /// 分布式库根据库名、读写状态、分区ID返回合理的连接字符串（来自memcached）
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="dbrwType">读写状态</param>
        /// <param name="HostID">分区ID</param>
        /// <returns></returns>
        public static ConnectionConfig GetConnectionString(string dbName, DBrwType dbrwType, long shardID =1)
        {
            //int shardID = GetShardID(dbName, HostID);
            string key = string.Format("{0}_{1}_{2}", shardID, dbName, (int)dbrwType).ToLower();
            
            if (_hashConn.ContainsKey(key) == false)//没有合适的读写字符串
            {
                throw new Exception(string.Format("no matched sql connection:{0}",key));
            }
            else
            {
                List<ConnectionConfig> conns = (List<ConnectionConfig>)_hashConn[key];
                if (conns.Count == 1)
                {
                    return conns[0];
                }
                else//超过1个时，随机选择一个读取
                {
                    int i = _random.Next() % conns.Count;
                    return conns[i];
                }
            } 
        }


        

        

       


        /// <summary>
        /// 获取连接字符串列表
        /// </summary>
        /// <returns></returns>
        private static void InitConn()
        {
            //key =  分区号-库名-读写   从数据库读取
            //Dictionary<string, List<ConnectionConfig>> dic = new Dictionary<string, List<ConnectionConfig>>();
            DbHelperForMysql dbhelper = new DbHelperForMysql(DALConfig.CoreConnString);
            string sql = "select * from connectionconfig where status=1";
            DataSet ds = dbhelper.ExecuteDataSet(sql);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                _hashConn.Clear();//清空
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow row = ds.Tables[0].Rows[i];
                    ConnectionConfig config = new ConnectionConfig();
                    config.DBID = Convert.ToInt32(row["ID"]);
                    config.ShardID = Convert.ToInt32(row["ShardID"]);
                    config.DBName = row["DBName"].ToString();
                    config.DBType = (DBType)Enum.Parse(typeof(DBType), row["DBType"].ToString());
                    config.ReadWriteType = Convert.ToInt32(row["ReadWriteType"]);
                                      
                    config.ConnectionString = row["ConnectionString"].ToString() 
                        + (DALConfig.AllowUserVariables ? ";AllowUserVariables=true" : null);
                    config.LogEnable = Convert.ToBoolean(row["LogEnable"]);
                    

                    string key = string.Format("{0}_{1}_{2}", config.ShardID, config.DBName, config.ReadWriteType).ToLower();
                    if (!_hashConn.ContainsKey(key))
                    {
                        _hashConn.Add(key, new List<ConnectionConfig>());
                    }
                    ((List<ConnectionConfig>)_hashConn[key]).Add(config);
                }
            }
        }

        private static void MonitorConn()
        {
            while (true)
            {
                bool existsUpdate = CheckUpdate();
                if (existsUpdate)
                {
                    InitConn();
                    ClearUpdate();
                }

                Thread.Sleep(DALConfig.ConnMonitorSeconds * 1000);
            }
        }

        private static bool CheckUpdate()
        {
            DbHelperForMysql dbhelper = new DbHelperForMysql(DALConfig.CoreConnString);
            string sql = "select 1 from connectionChgLog where status = 1" ;
            bool existsUpdate = dbhelper.Exists(sql);
            return existsUpdate;
        }

        private static void ClearUpdate()
        {
            DbHelperForMysql dbhelper = new DbHelperForMysql(DALConfig.CoreConnString);
            string sql = "update connectionChgLog set status = 0 where status = 1";
            dbhelper.ExecuteNonQuery(sql);
        }
    }
}
