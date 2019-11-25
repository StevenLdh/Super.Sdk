using CC.DataAccess.Extend;
using CC.Framework.DAL;
using CC.Public;
using Super.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordPressPCL;
using WordPressPCL.Models;
namespace Super.SDK.ConsoleExe
{
    public class Program
    {
        public const string WordPressUri = "https://school.lk361.com/wp-json/";
        public const string Username = "lk361";
        public const string Password = "r6T3puJxiBH!6uPl";
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
            //var curr = DateTime.Now;
            //long millisecond = (long)(DateTime.Parse("2019-03-01").AddHours(curr.Hour).AddMinutes(curr.Minute).AddSeconds(curr.Second) - curr).TotalMilliseconds;

            #region Lamamda和Linq
            var products = GetProductListData();
            List<Product> take = products.Take(3).ToList();//顺序取3条记录
            List<Product> takeWhile = products.TakeWhile(p => p.ID <= 1).ToList();//只要不满足条件了，返回所有当前记录
            List<Product> skip = products.Skip(3).ToList();//顺序跳过3条记录
            List<Product> skipWhile = products.SkipWhile(p => p.Price < 100000).ToList();//只要不满足条件了，返回所有剩余记录
            foreach (var item in skip) {
                Console.WriteLine(item.ToJsonString());
            }
            #endregion
            Console.ReadLine();

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
        #region 静态数据源
        public class Product
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Region { get; set; }
            public decimal Price { get; set; }
            public bool IsFavorite { get; set; }
        }

        public static List<Product> GetProductListData() {
            List<Product> products = new List<Product> {
                new Product { ID=1, Name="路易十八比萨饼", Region="意大利", Price=79961, IsFavorite = false },
                new Product { ID=2, Name="澳洲胡桃", Region="澳洲", Price=195, IsFavorite = false },
                new Product { ID=3, Name="Almas鱼子酱", Region="伊朗", Price=129950, IsFavorite = true },
                new Product { ID=4, Name="和牛肉", Region="日本", Price=3250, IsFavorite = true },
                new Product { ID=5, Name="麝香猫咖啡豆", Region="印尼", Price=2000, IsFavorite = true },
                new Product { ID=6, Name="大红袍茶叶", Region="中国", Price=208000, IsFavorite = true },
                new Product { ID=7, Name="Kona Nigari矿泉水", Region="美国", Price=13000, IsFavorite = true },
                new Product { ID=8, Name="Diva伏特加", Region="北欧", Price=6500, IsFavorite = false },
                new Product { ID=9, Name="番红花的雄蕊", Region="地中海", Price=38986, IsFavorite = false },
            };
            return products;
        }
        #endregion

    }
}
