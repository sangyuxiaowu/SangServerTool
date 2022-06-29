using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;

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

        /// <summary>
        /// 获取电脑网卡IP
        /// </summary>
        /// <param name="isV6">是获取IPv6</param>
        /// <returns></returns>
        public static string CurrentIPAddress(bool isV6 = false)
        {
            var family = isV6? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            List<string> exps = new List<string> { "docker0", "lo", "l4tbr0" };
            var ips = NetworkInterface.GetAllNetworkInterfaces()
            .Where(p => !exps.Contains(p.Name)) // 排除docker、lo等
            .Select(p => p.GetIPProperties())
            .SelectMany(p => p.UnicastAddresses)
            .Where(p => p.Address.AddressFamily == family && !IPAddress.IsLoopback(p.Address));
            //IPv6 时去除本地的
            if (family == AddressFamily.InterNetworkV6)
            {
                ips = ips.Where(p => !p.Address.IsIPv6LinkLocal);
            }
            //((System.Net.NetworkInformation.UnixUnicastIPAddressInformation)(new System.Linq.SystemCore_EnumerableDebugView<System.Net.NetworkInformation.UnicastIPAddressInformation>(ips).Items[3])).Address.IsIPv6LinkLocal
            //((System.Net.NetworkInformation.UnixUnicastIPAddressInformation)(new System.Linq.SystemCore_EnumerableDebugView<System.Net.NetworkInformation.UnicastIPAddressInformation>(ips).Items[1])).Address.IsIPv6LinkLocal
            return ips.FirstOrDefault()?.Address.ToString();
        }
    }
}
