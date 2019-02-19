using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
namespace Super.Sdk
{
    public class FileZip
    {
        /// <summary>
        /// 压缩指定的文件到指定目录
        /// </summary>
        /// <param name="sourceDirectoryName"></param>
        /// <param name="destinationArchiveFileName"></param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) {
            try {
                ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
            } catch (Exception ex) {
                throw new Exception(ex.Message, ex);
            }
        }
        /// <summary>
        /// 解压指定的文件到指定目录
        /// </summary>
        /// <param name="sourceDirectoryName"></param>
        /// <param name="destinationArchiveFileName"></param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            try
            {
                ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
