using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;

namespace SangServerTool
{
    public static class Utils
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
        public static string? CurrentIPAddress(bool isV6 = false)
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
            return ips.FirstOrDefault()?.Address.ToString();
        }
        /// <summary>
        /// 获取电脑外网IP
        /// </summary>
        /// <returns></returns>
        public static string CurrentIPAddressByWeb(bool isV6 = false) {
            using var client = new HttpClient();
            string ip = "";
            try
            {
                ip = client.GetStringAsync($"https://{(isV6 ? "6" : "4")}.ipw.cn/").Result;
            }
            catch {
                return ip;
            }
            return ip;
        }

        /// <summary>
        /// 根据要申请的域名信息，返回要设置的RR信息
        /// </summary>
        /// <param name="domains">配置的证书DNS</param>
        /// <param name="basedomain">基础域名</param>
        /// <returns></returns>
        public static string[] GetRRDomain(string domains, string basedomain)
        {
            string[] domain = domains.Split(' ');
            for (var i = 0; i < domain.Length; i++)
            {
                domain[i] = domain[i].StartsWith("*") ? domain[i].Replace("*", "_acme-challenge") : "_acme-challenge." + domain[i];
                var inx = domain[i].LastIndexOf(basedomain);
                if (inx > -1)
                {
                    domain[i] = domain[i].Substring(0, inx - 1);
                }
            }
            return domain;
        }

        /// <summary>
        /// 根据DDNS地址和域名获取要设置的RR信息
        /// </summary>
        /// <param name="domain">DDNS</param>
        /// <param name="basedomain">基础域名</param>
        /// <returns>RR，空为异常</returns>
        public static string GetRRDdns(string domain, string basedomain) {
            // 解析主域名
            if (domain == basedomain) return "@";
            int inx = domain.LastIndexOf(basedomain);
            if (inx > -1) { 
                return domain.Substring(0, inx - 1);
            }
            return "";
        }

    }
}
