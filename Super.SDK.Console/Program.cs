﻿using CC.DataAccess.Extend;
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

            //ViewData();
            TestTask();

        }
        /// <summary>
        /// 查询数据案例
        /// </summary>
        public static void ViewData() {
            HashObjectList datasourcecombase =
                new DALBase("erp").GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
            Console.WriteLine(datasourcecombase.Count());
        }
        /// <summary>
        /// 多线程使用案例
        /// </summary>
        public static void TestTask() {
            List<Task> tasks = new List<Task>();
            HashObjectList datasourcecombase = new HashObjectList();
           var listtask = Task.Run(() =>
            {
                //1.查询列表
                UnitsClass.IsSetTimeout();
                datasourcecombase =
                 new DALBase("erp").GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
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
    }
}
