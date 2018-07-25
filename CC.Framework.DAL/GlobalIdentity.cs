using CC.DataAccess;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CC.DataAccess.Extend;
using System.Configuration;

namespace CC.Framework.DAL
{

    public class GlobalIdentity
    {
        /// <summary>
        /// 使用identity预先获取功能，这会导致程序重启时部分id浪费
        /// </summary>
        static bool USE_GLOBAL_IDENTITY_PRE_GET = true;
        static Dictionary<string, GlobalIdentityInfo> GlobalIdentityInfo_Map = new Dictionary<string, GlobalIdentityInfo>();
        static Dictionary<string, object> global_lock = new Dictionary<string, object>();
        static object rootLock = new object();

        static GlobalIdentity()
        {
            bool.TryParse(ConfigurationManager.AppSettings["USE_GLOBAL_IDENTITY_PRE_GET"] ?? "true", out USE_GLOBAL_IDENTITY_PRE_GET);
        }
        /// <summary>
        /// 返回一条自增列的值
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static long GetGetIdentity(string dbName, string tableName)
        {
            return Get(dbName, tableName, 1)[0];
            //if (USE_GLOBAL_IDENTITY_PRE_GET)
            //{
            //    string key = dbName + "_" + tableName;
            //    object syncKey = null;
            //    lock (rootLock)
            //    {
            //        if (global_lock.ContainsKey(key))
            //        {
            //            syncKey = global_lock[key];
            //        }
            //        else
            //        {
            //            syncKey = new object();
            //            global_lock[key] = syncKey;
            //        }
            //    }

            //    lock (syncKey)
            //    {
            //        GlobalIdentityInfo ret = null;
            //        if (!GlobalIdentityInfo_Map.ContainsKey(key))
            //        {
            //            GlobalIdentityInfo_Map[key] = ret = new GlobalIdentityInfo();
            //        }
            //        else
            //        {
            //            ret = GlobalIdentityInfo_Map[key];
            //        }
            //        if (ret.NeedNew())
            //        {
            //            //数据库取
            //            var ids = Get(dbName, tableName, 100);
            //            //reset
            //            ret.Reset(ids[0], 100);
            //        }
            //        return ret.GetNextIdentity();
            //    }
            //}
            //else
            //{
            //    return Get(dbName, tableName, 1)[0];
            //}
        }

        /// <summary>
        /// 返回多条自增列的值
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static long[] GetGetIdentity(string dbName, string tableName, int count)
        {
            return Get(dbName, tableName, count);
        }

        private static long[] Get(string dbName, string tableName, int count)
        {
            long[] result = new long[count];
            if (count == 0) return result;

            ConnectionConfig config = ConnectionManager.GetConnectionString(dbName + "_inc", DBrwType.Write);
            DbHelper dbHelper = DbHelper.Create(DBType.MySql, config.ConnectionString); ;

            if (count > 20)//超过30条时，锁定表，只插入一次(用于批量导入)
            {
                using (TransScope trans = new TransScope(TransScopeOption.RequiresNew))
                {
                    dbHelper.ExecuteNonQuery("LOCK TABLES " + tableName + " WRITE");
                    HashObject data = dbHelper.GetData("select id from " + tableName + " order by id desc limit 1");
                    long max = data.GetValue<long>("id", 0);//当前最大值
                    long newmax = max + count;
                    dbHelper.ExecuteNonQuery("insert into " + tableName + " values (" + newmax + ");UNLOCK TABLES;");
                    trans.Complete();

                    for (int i = 0; i < count; i++)
                    {
                        result[i] = max + i + 1;
                    }
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("insert into {0} values ", tableName);
                for (int i = 0; i < count; i++)
                {
                    sb.Append("(null),");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(";Select LAST_INSERT_ID() As ID;");
                string sql = sb.ToString();

                long firstId = 0;
                using (TransScope trans = new TransScope(TransScopeOption.RequiresNew))
                {
                    object temp = dbHelper.ExecuteScalar(sql);
                    firstId = Convert.ToInt64(temp);
                    trans.Complete();
                }

                for (int i = 0; i < count; i++)
                {
                    result[i] = firstId + i;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取当前最大ID
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static long GetMaxIdentity(string dbName, string tableName)
        {
            ConnectionConfig config = ConnectionManager.GetConnectionString(dbName + "_inc", DBrwType.Write);
            DbHelper dbHelper = DbHelper.Create(DBType.MySql, config.ConnectionString); ;
            HashObject data = dbHelper.GetData("select id from " + tableName + " order by id desc limit 1");
            long max = data.GetValue<long>("id", 0);//当前最大值
            return max;
        }

        class GlobalIdentityInfo
        {
            long start;
            long end;
            public GlobalIdentityInfo()
            {

            }
            public GlobalIdentityInfo(long start, int count)
            {
                this.Reset(start, count);
            }
            public void Reset(long start, int count)
            {
                this.start = start;
                this.end = start + count;
            }
            public bool NeedNew()
            {
                return this.start >= this.end;
            }
            public long GetNextIdentity()
            {
                return start++;
            }
        }
    }

    //    class GlobalIdentity2
    //    {
    //        public static long GetGetIdentity(string dbName, string tableName)
    //        {
    //            bool exists = Exists(dbName, tableName);
    //            if (!exists)
    //            {
    //                //初始化
    //                Init(dbName, tableName);
    //            }

    //            long top = Get(dbName, tableName, 1);

    //            return top + 1;
    //        }

    //        public static long[] GetGetIdentity(string dbName, string tableName, int count)
    //        {
    //            long[] result = new long[count];
    //            bool exists = Exists(dbName, tableName);
    //            if (!exists)
    //            {
    //                //初始化
    //                Init(dbName, tableName);
    //            }
    //            long top = Get(dbName, tableName, count);

    //            for (int i = 0; i < count; i++)
    //            {
    //                result[i] = top + 1 + i;
    //            }

    //            return result;
    //        }

    //        private static bool Exists(string dbName, string tableName)
    //        {
    //            DbHelperForMysql dbHelper = new DbHelperForMysql(DALConfig.GlobalConnString);
    //            string sql = "select count(1) from ccidentity where dbName=@dbName and tableName=@tableName";
    //            HashObject parameters = new HashObject();
    //            parameters.Add("dbName", dbName);
    //            parameters.Add("tableName", tableName);
    //            bool result = dbHelper.Exists(sql, parameters);
    //            return result;
    //        }

    //        private static void Init(string dbName, string tableName)
    //        {
    //            DbHelperForMysql dbHelper = new DbHelperForMysql(DALConfig.GlobalConnString);
    //            string sql = "insert into  ccidentity (dbName,tableName,topValue) values (@dbName,@tableName,0)";
    //            HashObject parameters = new HashObject();
    //            parameters.Add("dbName", dbName);
    //            parameters.Add("tableName", tableName);
    //            dbHelper.ExecuteNonQuery(sql, parameters);
    //        }

    //        private static long Get(string dbName, string tableName,int count)
    //        {
    //            object result = null;

    //            DbHelperForMysql dbHelper = new DbHelperForMysql(DALConfig.GlobalConnString);
    //            string sql = @"
    //SELECT topValue FROM ccidentity WHERE dbName = @dbName AND tableName = @tableName FOR UPDATE;
    //UPDATE ccidentity SET topValue = topValue + @count WHERE dbName = @dbName AND tableName = @tableName";
    //            HashObject parameters = new HashObject();
    //            parameters.Add("dbName", dbName);
    //            parameters.Add("tableName", tableName);
    //            parameters.Add("count", count);

    //            using (TransScope trans = new TransScope())
    //            {
    //                result = dbHelper.ExecuteScalar(sql, parameters);
    //                trans.Complete();
    //            }

    //            return Convert.ToInt32(result);
    //        }
    //    }
}
