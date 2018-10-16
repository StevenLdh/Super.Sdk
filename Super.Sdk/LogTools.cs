using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Super.Sdk
{
    /// <summary>
    /// LogTools帮助类
    /// </summary>
    public class LogTools
    {
        public static readonly log4net.ILog Loginfo = log4net.LogManager.GetLogger("MyLogo4Net");   
      
        public static void SetConfig()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        /// <summary>
        /// 设置文件路径
        /// </summary>
        /// <param name="configFile"></param>
        public static void SetConfig(FileInfo configFile)
        {
            log4net.Config.XmlConfigurator.Configure(configFile);
        }
        /// <summary>
        /// 写系统信息日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteLog(string info)
        {
            if (Loginfo.IsInfoEnabled)
            {
                Loginfo.Info(info);
            }
        }
        /// <summary>
        /// 写错误日志
        /// </summary>
        /// <param name="error"></param>
        /// <param name="se"></param>
        public static void WriteErrorLog(string error, Exception se)
        {
            if (Loginfo.IsErrorEnabled)
            {
                Loginfo.Error(error, se);
            }
        }
        /// <summary>
        /// 写错误日志
        /// </summary>
        /// <param name="error"></param>
        /// <param name="se"></param>
        public static void WriteErrorLog(string error)
        {
            if (Loginfo.IsErrorEnabled)
            {
                Loginfo.Error(error);
            }
        }
    }
}
