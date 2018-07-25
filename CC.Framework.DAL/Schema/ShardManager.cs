using CC.DataAccess;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Framework.DAL
{
    public class ShardManager
    {
        private static string _preCacheKey = "CC.ShardCache";

        /// <summary>
        /// 获取分区ID
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="hostID"></param>
        /// <returns></returns>
        public static int GetShardID(string dbName, HashObject parameters)
        {
            if (parameters.ContainsKey("__shardid"))//显式指定分区id
            {
                return Convert.ToInt32(parameters["__shardid"]);
            }

            long hostID = GetHostID(dbName, parameters);

            if (hostID == 0)//单库返回1号分区
                return 1;
            else
            {
                //return 1;
                string cacheKey = string.Format("{0}.{1}", _preCacheKey, hostID);
                //1 先从web缓存获取
                object result = CC.Caching.WebCache.GetCache(cacheKey);
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    //2.从数据库获取
                    int shardid = GetShardIDFromDB(hostID);
                    //3.放入缓存2分钟，滑动过期 
                    CC.Caching.WebCache.SetCache(cacheKey, shardid, new TimeSpan(0, 2, 0));

                    return shardid;
                }
            }
        }

        /// <summary>
        /// 注册时，根据公司ID，返回分区id
        /// </summary>
        /// <param name="hostid"></param>
        /// <returns></returns>
        public static int CreateShardId(int profileid)
        {
            //读取配置文件
            //return 1;
            int shardid = DALConfig.GetRegisterShard("erp");
            return shardid;
        }

        /// <summary>
        /// 改变分区(从缓存移除)
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="parameters"></param>
        public static void ChangeShard(string dbName, HashObject parameters)
        {
            long hostID = GetHostID(dbName, parameters);
            if (hostID == 0)
                return;
            else
            {
                //从缓存移除
                string cacheKey = string.Format("{0}.{1}", _preCacheKey,  hostID);
                CC.Caching.WebCache.Remove(cacheKey);
            }
        }

        /// <summary>
        /// 从数据库获取分区ID
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="HostID"></param>
        /// <returns></returns>
        private static int GetShardIDFromDB(long HostID)
        {
            DbHelper dbhelper = DbHelper.Create(DBType.MySql, DALConfig.CoreConnString);
            //string routeTable = DALConfig.GetRouteTable(dbName);
            string sql = "select shardid from erp_shard where hostid =" + HostID.ToString();
            object result = dbhelper.ExecuteScalar(sql);
            if (result == null)
                throw new Exception("shard is not exists");

            return (int)result;
            //return 1;//暂时都返回1号分区
        }

        private static long GetHostID(string dbName, HashObject parameters)
        {
            string colName = DALConfig.GetHostColName(dbName);//从配置文件获取此数据库的分区列
            if (!string.IsNullOrEmpty(colName))//如果有分区列，则参数中必须包含,且值不能为0
            {
                if (!parameters.ContainsKey(colName))
                {
                    throw new ArgumentNullException("parameters必须包含:" + colName);
                }
                long hostID = Convert.ToInt64(parameters[colName]);
                if (hostID == 0)
                {
                    throw new ArgumentNullException("分区id不能为0");
                }

                return hostID;
            }
            else//单库
            {
                return 0;
            }
        }
    }
}
