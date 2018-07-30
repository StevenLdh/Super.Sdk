using CC.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Super.Sdk
{
    public class CacheManager
    {
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="d"></param>
        public static void SetCache(string key, object data)
        {
            WebCache.SetCache(key, data);
        }
        /// <summary>
        /// 设置缓存(时间)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="d"></param>
        public static void SetCache(string key, object data,DateTime d)
        {
            WebCache.SetCache(key, data,d);
        }
        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveCache(string key) {
            WebCache.Remove(key);
        }
        /// <summary>
        /// 获取缓存的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>

        public static object GetCache(string key)
        {
            return WebCache.GetCache(key);
        }
    }
}
