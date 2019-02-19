using CC.DataAccess.Extend;
using CC.Framework.DAL;
using CC.Public;
using Super.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Super.SDK.ConsoleExe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //ViewData();// 测试操作数据库
            //TestTask();// 测试多线程
            //UseTxtLog();// 测试日志
            //UseCache();// 测试缓存
            //RedisTest.GetRedisData();// 测试redis

            #region 异步等待特性使用
            //AsyncAwaitMethond();
            //Console.WriteLine("Main Thread!");
            //Console.ReadLine();
            #endregion
            #region 压缩文件和解压
            //压缩文件
            //FileZip.CreateFromDirectory(@"D:\testfile", @"D:\testfile.zip");
            //解压文件
            //FileZip.ExtractToDirectory(@"D:\testfile.zip", @"D:\testfile");
            #endregion
        }
        #region 查询数据案例
        /// <summary>
        /// 查询数据案例
        /// </summary>
        public static void ViewData()
        {
            HashObjectList datasourcecombase =
                new DALBase("erp").GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
            Console.WriteLine(datasourcecombase.Count());
        }
        #endregion
        #region 多线程使用案例
        /// <summary>
        /// 多线程使用案例
        /// </summary>
        public static void TestTask()
        {
            List<Task> tasks = new List<Task>();
            HashObjectList datasourcecombase = new HashObjectList();
            var listtask = Task.Run(() =>
            {
                //1.查询列表
                UnitsClass.IsSetTimeout();
                datasourcecombase =
                 new DALBase("erp", DBrwType.Write, true).GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
            });
            tasks.Add(listtask);

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                throw;
            }

            Console.WriteLine(datasourcecombase.Count());
        }
        #endregion
        #region 使用文本日志
        /// <summary>
        /// 使用文本日志
        /// </summary>
        public static void UseTxtLog()
        {
            LogTools.SetConfig();
            LogTools.WriteLog("日志测试文件");
        }
        #endregion
        #region 使用缓存技术
        /// <summary>
        /// 使用缓存技术
        /// </summary>
        public static void UseCache()
        {
            CacheManager.SetCache("key_20180730", "20180730");
            Console.WriteLine(CacheManager.GetCache("key_20180730"));
        }
        #endregion
        #region 异步等待特性
        /// <summary>
        /// 异步等待
        /// </summary>
        public static async void AsyncAwaitMethond() {
            //等异步执行完成继续执行
            await System.Threading.Tasks.Task.Run(new Action(LogTask));
            await System.Threading.Tasks.Task.Run(new Action(LogTaskTwo));
            Console.WriteLine("New Thread");
        }
        /// <summary>
        /// 异步调用方法1
        /// </summary>
        public static void LogTask() {
            Console.WriteLine("LogTask Thread");
        }
        /// <summary>
        /// 异步调用方法2
        /// </summary>
        public static void LogTaskTwo()
        {
            Console.WriteLine("LogTaskTwo Thread");
        }
        #endregion
    }
}
