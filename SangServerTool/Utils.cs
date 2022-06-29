using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace SangServerTool
{
    internal static class Utils
    {
        /// <summary>
        /// 证书是否在5日内过期
        /// </summary>
        /// <param name="certFilePath">证书路径</param>
        /// <returns>是否符合5日内过期</returns>
        public static bool isCerWillExp(string certFilePath)
        {
            var cer = new X509Certificate(certFilePath);
            DateTime expdate = Convert.ToDateTime(cer.GetExpirationDateString());
            TimeSpan span = DateTime.Now.Subtract(expdate);
            //Console.WriteLine(span.Days);
            if (span.Days >= -5) return true;
            return false;
        }


    }
}
