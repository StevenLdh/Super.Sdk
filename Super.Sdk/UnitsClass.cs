using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CC.DataAccess.Extend;
using CC.Public;
namespace Super.Sdk
{
    public class UnitsClass
    {
        /// <summary>
        /// 是否设置超时时间
        /// </summary>
        public static void IsSetTimeout()
        {
            //如果是多线程调用需要设置requestid
            if (System.Web.HttpContext.Current == null)
            {
                //CallContext.SetData("cc_request_id") = 
            }
            var IsReportDBCommand = System.Configuration.ConfigurationManager.AppSettings["IsReportDBCommand"];
            if (IsReportDBCommand == "1")
            {
                var ReportDbTimeOutCfg = System.Configuration.ConfigurationManager.AppSettings["IsReportDBCommandTimeout"];
                int ReportDbTimeOut;
                if (!string.IsNullOrEmpty(ReportDbTimeOutCfg) && int.TryParse(ReportDbTimeOutCfg, out ReportDbTimeOut))
                {
                    //CC.DataAccess.DbHelper.SetThreadScopeCommandTimeout(ReportDbTimeOut); //延迟超时时间
                }
            }
        }
    }
}
