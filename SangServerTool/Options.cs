namespace SangServerTool
{

    /// <summary>
    /// 请求SSL证书的参数
    /// </summary>
    public class AUTO_SSL
    {
        /// <summary>
        /// 配置ASSK和证书信息
        /// </summary>
        public string? ConfigFile { get; set; }

        /// <summary>
        /// DNS验证重试多少次？
        /// </summary>
        public int Retry { get; set; }

        /// <summary>
        /// DNS验证失败等待多少秒重试？
        /// </summary>
        public int Delay { get; set; }
    }


    /// <summary>
    /// 配置DDNS的参数
    /// </summary>
    public class AUTO_DDNS
    {
        /// <summary>
        /// 配置AKSK相关,及DDNS域
        /// </summary>
        public string? ConfigFile { get; set; }

        /// <summary>
        /// 延迟多少秒执行
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// 是否为IPv6地址
        /// </summary>
        public bool Del { get; set; }

        /// <summary>
        /// 是否为IPv6地址
        /// </summary>
        public bool IPV6 { get; set; }

        /// <summary>
        /// 指定IP
        /// </summary>
        public string? IP { get; set; }


    }

}
