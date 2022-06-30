using CommandLine;

namespace SangServerTool
{

    /// <summary>
    /// 请求SSL证书的参数
    /// </summary>
    [Verb("ssl", HelpText = "Get Let's Encrypt SSL Cert.")]
    internal class AUTO_SSL
    {
        /// <summary>
        /// 配置ASSK和证书信息
        /// </summary>
        [Option('c', "config", Required = true, HelpText = "Set config json file.")]
        public string? ConfigFile { get; set; }
    }


    /// <summary>
    /// 配置DDNS的参数
    /// </summary>
    [Verb("ddns", HelpText = "Set DDNS.")]
    internal class AUTO_DDNS
    {
        /// <summary>
        /// 配置AKSK相关,及DDNS域
        /// </summary>
        [Option('c', "config", Required = true, HelpText = "Set config json file.")]
        public string? ConfigFile { get; set; }

        /// <summary>
        /// 是否为IPv6地址
        /// </summary>
        [Option("v6", Default = false, HelpText = "Is ipv6?")]
        public bool IPV6 { get; set; }

        /// <summary>
        /// 指定IP
        /// </summary>
        [Option("ip", Default = "", HelpText = "If set will be used. Otherwise automatically obtained.\n You can set 'ifconfig', It will check from 'https://ifconfig.me/ip' to get you Internet IP.")]
        public string? IP { get; set; }

    }

}
