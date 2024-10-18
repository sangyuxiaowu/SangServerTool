using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;

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
            .Where(p => p.Address.AddressFamily == family && !IPAddress.IsLoopback(p.Address) &&
                (family == AddressFamily.InterNetwork || !IsNotGoodIPv6(p))
            );
            return ips.FirstOrDefault()?.Address.ToString();
        }

        /// <summary>
        /// 判断IPv6地址是否不太可用
        /// 排除Dhcp,本地和随机的临时地址
        /// </summary>
        /// <param name="unicastAddress">单播地址信息</param>
        /// <returns>不用则返回true</returns>
        private static bool IsNotGoodIPv6(UnicastIPAddressInformation unicastAddress)
        {
            // 判断平台
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return unicastAddress.Address.IsIPv6LinkLocal || unicastAddress.PrefixOrigin == PrefixOrigin.Dhcp || unicastAddress.SuffixOrigin == SuffixOrigin.Random;
            }
            else
            {
                // 其他暂时这样处理
                return unicastAddress.Address.IsIPv6LinkLocal || unicastAddress.Address.ToString().Length < 35;
            }
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

        /// <summary>
        /// 执行shell脚本
        /// </summary>
        /// <param name="file">脚本文件</param>
        /// <param name="logger">日志</param>
        public static void RunShell(string file,ILogger logger)
        {
            // 脚本文件存在，执行
            if (File.Exists(file))
            {
                //创建一个ProcessStartInfo对象 使用系统shell 指定命令和参数 设置标准输出
                var psi = new ProcessStartInfo(file) { RedirectStandardOutput = true };
                //启动
                var proc = Process.Start(psi);
                if (proc == null)
                {
                    logger.LogError("处理脚本启动失败");
                }
                else
                {
                    logger.LogInformation("-------------Start read standard output--------------");
                    //开始读取
                    using (var sr = proc.StandardOutput)
                    {
                        while (!sr.EndOfStream)
                        {
                            logger.LogInformation(sr.ReadLine());
                        }

                        if (!proc.HasExited)
                        {
                            proc.Kill();
                        }
                    }
                    logger.LogInformation("---------------Read end------------------");
                }
            }
            else
            {
                logger.LogInformation("脚本文件不存在");
            }
        }
    }
}
