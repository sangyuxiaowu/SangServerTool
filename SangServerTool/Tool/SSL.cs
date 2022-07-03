using System;
using SangServerTool.Domain;
using Microsoft.Extensions.Configuration;
using Certes;
using Certes.Acme;

namespace SangServerTool.Tool
{

    /// <summary>
    /// SSL免费证书申请
    /// 文档 https://github.com/fszlin/certes
    /// </summary>
    public class SSL
    {
        static readonly string runPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        public async static Task<int> Run(AUTO_SSL opt)
        {
            Console.WriteLine($"配置文件：{opt.ConfigFile}");
            if (!File.Exists(opt.ConfigFile))
            {
                Console.WriteLine("配置文件不存在");
                return 1;
            }
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(opt.ConfigFile, optional: false, reloadOnChange: false);
            IConfigurationRoot config = configBuilder.Build();

            // 申请证书的信息
            CertificateInfo cer_info = config.GetSection("Certificate").Get<CertificateInfo>();
            // 证书的CSR信息
            CsrInfo cer_csr = config.GetSection("CSR").Get<CsrInfo>();
            // ACME 账户信息
            CerAcme cer_acme = config.GetSection("ACME").Get<CerAcme>();

            // 是否存在 证书，不存在就直接创建申请了
            bool isHaved = File.Exists(cer_info.cerpath);
            //获取是否还有5天内就过期
            if (isHaved && !Utils.isCerWillExp(cer_info.cerpath))
            {
                Console.WriteLine("无需处理");
                return 0;
            }

            AcmeAccount acmeinfo;
            try
            {
                acmeinfo = await getAcmeAccountAsync(cer_acme.email, cer_acme.account);
            }
            catch (Exception ex)
            {
                Console.WriteLine("登录账户：" + ex.Message);
                return 1;
            }

            //获取申请单上下文
            var orderListContext = await acmeinfo.account.Orders();
            var orders = await orderListContext.Orders();
            //https://acme-staging-v02.api.letsencrypt.org/acme/order/59498234/3028278734

            // 开始请求证书，获取DNS验证的配置信息
            DnsTask DnsTask;
            try
            {
                DnsTask = await getDnsAuthInfoAsync(acmeinfo.acme, cer_info.domains);
            }
            catch (Exception ex)
            {
                Console.WriteLine("提交申请：" + ex.Message);
                return 1;
            }


            await Task.Delay(100);
            Console.WriteLine(opt.ToString());
            return 1;
        }

        /// <summary>
        /// 获取DNS验证信息
        /// </summary>
        /// <param name="acme">ACME账户对象</param>
        /// <param name="domains">申请的域名信息，多个用空格隔开</param>
        /// <returns></returns>
        public static async Task<DnsTask> getDnsAuthInfoAsync(AcmeContext acme, string domains)
        {
            string[] domain = domains.Split(' ');
            var order = await acme.NewOrder(domain);
            var authz = await order.Authorizations();
            IChallengeContext[] dnsChallenges = new IChallengeContext[domain.Length];
            string[] dnsTxts = new string[domain.Length];
            int i = 0;
            foreach (var z in authz)
            {
                dnsChallenges[i] = await z.Dns();
                dnsTxts[i] = acme.AccountKey.DnsTxt(dnsChallenges[i].Token);
                i++;
            }
            return new DnsTask(dnsChallenges, order, dnsTxts);
        }


        /// <summary>
        /// 获取Acme登录后对象
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <param name="pemKeyFile">邮箱账户pem密钥文件地址</param>
        /// <returns></returns>
        public static async Task<AcmeAccount> getAcmeAccountAsync(string email, string pemKeyFile)
        {
            string pemKey = File.Exists(pemKeyFile) ? await File.ReadAllTextAsync(pemKeyFile) : "";
            var acme = pemKey == "" ? new AcmeContext(WellKnownServers.LetsEncryptStagingV2) : new AcmeContext(WellKnownServers.LetsEncryptStagingV2, KeyFactory.FromPem(pemKey));
            var account = pemKey == "" ? await acme.NewAccount(email, true) : await acme.Account();

            // 若没有账户，则保存一下账户的KEY
            if (pemKey == "")
            {
                pemKey = acme.AccountKey.ToPem();
                await File.AppendAllTextAsync(pemKeyFile, pemKey);
            }

            return new AcmeAccount(acme, account);
        }

        /// <summary>
        /// DNS验证返回
        /// </summary>
        /// <param name="dnsChallenge"></param>
        /// <param name="order"></param>
        /// <param name="dnsTxt"></param>
        public record DnsTask(IChallengeContext[] dnsChallenge, IOrderContext order, string[] dnsTxt);

        /// <summary>
        /// 登录后的ACNE账户信息
        /// </summary>
        /// <param name="acme">Acme对象</param>
        /// <param name="account">账户</param>
        public record AcmeAccount(AcmeContext acme, IAccountContext account);

        /// <summary>
        /// 配置信息，申请的证书的相关信息
        /// </summary>
        public record CertificateInfo
        {
            /// <summary>
            /// 证书存放地址，这个文件不存在会新申请
            /// </summary>
            public string cerpath { get; set; }
            /// <summary>
            /// 证书的私钥文件，这个文件不存在时会自动生成
            /// </summary>
            public string privatekey { get; set; }
            /// <summary>
            /// 申请证书的域名信息，多个用空格分开，必须同一个域
            /// </summary>
            public string domains { get; set; }
            /// <summary>
            /// 证书的主域名信息
            /// </summary>
            public string basedomain { get; set; }
        }


        /// <summary>
        /// 配置信息，申请证书用的ACME账户
        /// </summary>
        public record CerAcme {
            /// <summary>
            /// 邮箱
            /// </summary>
            public string email { get; set; }
            /// <summary>
            /// 用户密钥文件存放路径
            /// </summary>
            public string account { get; set; }
        }

    }
}
