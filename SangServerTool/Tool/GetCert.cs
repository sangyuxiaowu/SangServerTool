using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SangServerTool.Tool
{
    public class GetCert
    {
        private ILogger _logger;

        public GetCert(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<int> Run(string config_file)
        {
            _logger.LogInformation($"开始执行：{DateTime.Now.ToString()}");
            _logger.LogInformation($"配置文件：{config_file}");
            if (!File.Exists(config_file))
            {
                _logger.LogError("配置文件不存在");
                return 1;
            }
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(config_file, optional: false, reloadOnChange: false);
            IConfigurationRoot config = configBuilder.Build();

            // 获取配置的源站点信息
            var site = config["Certificate:site"];
            if (string.IsNullOrEmpty(site))
            {
                _logger.LogError("未配置源站点信息");
                return 1;
            }
            // 检查是否为合法的https URL
            if (!site.StartsWith("https://"))
            {
                _logger.LogError("源站点信息不是https站点");
                return 1;
            }

            // 获取存储证书位置
            var cert_file = config["Certificate:cerpath"];
            if (string.IsNullOrEmpty(cert_file))
            {
                _logger.LogError("未配置证书存储位置");
                return 1;
            }

            // 获取远端证书
            var cert = await GetRemoteCert(site);
            if (cert == null)
            {
                _logger.LogError("获取远端证书失败");
                return 1;
            }

            // 保存证书
            try
            {
                File.WriteAllText(cert_file, $"-----BEGIN CERTIFICATE-----\n{cert}\n-----END CERTIFICATE-----");
                _logger.LogInformation($"证书已保存到：{cert_file}");

                // shell脚本
                var shell = config["Certificate:okshell"];
                if (!string.IsNullOrEmpty(shell))
                {
                    Utils.RunShell(shell, _logger);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存证书失败：{ex.Message}");
                return 1;
            }

        }

        private byte[] _certificate;
        private async Task<string?> GetRemoteCert(string site)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = CertificateValidationCallback
                };

                using (var client = new HttpClient(handler))
                {
                    await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, site));
                }

                return Convert.ToBase64String(_certificate, Base64FormattingOptions.InsertLineBreaks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取证书失败: {ex.Message}");
                return null;
            }
        }

        private bool CertificateValidationCallback(HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            _logger.LogInformation($"证书信息: {certificate.Subject}");
            _certificate = certificate.GetRawCertData();
            return true;
        }
    }
}
