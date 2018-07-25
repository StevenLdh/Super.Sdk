using CC.DataAccess.Extend;
using CC.Framework.DAL;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Super.SDK.ConsoleExe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HashObjectList datasourcecombase = 
                new DALBase("erp").GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
            Console.WriteLine(datasourcecombase.Count());
            
        }
    }
}
