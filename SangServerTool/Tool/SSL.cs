using System;
using SangServerTool.Domain;
using Microsoft.Extensions.Configuration;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace SangServerTool.Tool
{

    /// <summary>
    /// SSL免费证书申请
    /// 文档 https://github.com/fszlin/certes
    /// </summary>
    public class SSL
    {
        public async static Task<int> Run(AUTO_SSL opt, ILogger logger)
        {
            logger.LogInformation($"配置文件：{opt.ConfigFile}");
            if (!File.Exists(opt.ConfigFile))
            {
                logger.LogError("配置文件不存在");
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
                logger.LogInformation("无需处理");
                return 0;
            }

            if(cer_csr is null || cer_info is null || cer_acme is null)
            {
                logger.LogError("配置文件格式错误");
                return 1;
            }

            AcmeAccount acmeinfo;
            try
            {
                acmeinfo = await GetAcmeAccountAsync(cer_acme.email, cer_acme.account);
            }
            catch (Exception ex)
            {
                logger.LogInformation("登录申请账户：" + ex.Message);
                return 1;
            }

            //获取申请单上下文
            var orderListContext = await acmeinfo.account.Orders();
            var orders = await orderListContext.Orders();
            //https://acme-staging-v02.api.letsencrypt.org/acme/order/59498234/3028278734

            if (orders.Any())
            {
                foreach (var order in orders)
                {
                    // 打印订单
                    logger.LogInformation($"订单：{order.Location}");
                }
                logger.LogInformation("已存在订单");
                return 1;
            }

            // 开始请求证书，获取DNS验证的配置信息
            DnsTask DnsTask;
            try
            {
                DnsTask = await GetDnsAuthInfoAsync(acmeinfo.acme, cer_info.domains);

                logger.LogInformation($"订单：{DnsTask.order.Location}");
                DnsTask.dnsChallenge.ToList().ForEach(x => logger.LogInformation($"验证：{x.Location}"));
            }
            catch (Exception ex)
            {
                logger.LogError("提交申请失败：" + ex.Message);
                return 1;
            }

            //进行域名TXT信息设置
            string[] rrdomain = Utils.GetRRDomain(cer_info.domains, cer_info.basedomain);
            string[] RecordIds = new string[rrdomain.Length];

            var al = new AliyunDomain(config["Access:AK"], config["Access:SK"]);
            for (var i = 0; i < rrdomain.Length; i++)
            {
                logger.LogInformation($"添加解析验证：{rrdomain[i]}\tTXT\t{DnsTask.dnsTxt[i]}");
                var req = await al.AddRecordsAsync(cer_info.basedomain, rrdomain[i], "TXT", DnsTask.dnsTxt[i]);
                if (!req.Success)
                {
                    logger.LogError("添加域名解析出错：" + req.Msg);
                    return 1;
                }
                RecordIds[i] = req.Id;
            }

            //进行验证
            logger.LogInformation("准备验证域名，请稍后 ...");
            await Task.Delay(2000);

            // 执行Validate
            foreach (var challenge in DnsTask.dnsChallenge)
            {
                await challenge.Validate();
            }

            // 检查验证结果
            int retry = 0;
            int ok;
            do
            {
                if (retry > 0) {
                    logger.LogInformation($"正在查询 {retry.ToString()}/{opt.Retry.ToString()}");
                }
                ok = 0;
                foreach (var challenge in DnsTask.dnsChallenge)
                {
                    var result = await challenge.Resource();
    
                    ok += result.Status == Certes.Acme.Resource.ChallengeStatus.Valid ? 1 : 0;
                }

                retry++;
                // 延时后重试
                await Task.Delay(1000*opt.Delay);
            } while (retry < opt.Retry && ok != rrdomain.Length);

            //删除TXT记录
            logger.LogInformation("执行域名验证结束，清理用于验证的TXT记录");
            foreach (var record in RecordIds)
            {
                await al.DelRecordsAsync(record);
            }


            if (ok != rrdomain.Length) {
                logger.LogError($"验证域名出错：域名TXT记录未全部验证通过，{ok}/{rrdomain.Length}");
                return 1;
            }

            //生成证书
            IKey privateKey = File.Exists(cer_info.privatekey) ? KeyFactory.FromPem(File.ReadAllText(cer_info.privatekey)) : KeyFactory.NewKey(KeyAlgorithm.RS256);
            if (!File.Exists(cer_info.privatekey))
            {
                string pem = privateKey.ToPem();
                File.WriteAllText(cer_info.privatekey, pem);
            }
            var cert = await DnsTask.order.Generate(cer_csr, privateKey);

            File.WriteAllText(cer_info.cerpath, cert.ToPem());

            logger.LogInformation("证书申请成功");

            // 脚本文件存在，执行
            if (File.Exists(cer_info.okshell))
            {
                //创建一个ProcessStartInfo对象 使用系统shell 指定命令和参数 设置标准输出
                var psi = new ProcessStartInfo(cer_info.okshell) { RedirectStandardOutput = true };
                //启动
                var proc = Process.Start(psi);
                if (proc == null)
                {
                    logger.LogError("证书更新后，后续处理脚本启动失败");
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

            return 0;
        }


        /// <summary>
        /// 获取DNS验证信息
        /// </summary>
        /// <param name="acme">ACME账户对象</param>
        /// <param name="domains">申请的域名信息，多个用空格隔开</param>
        /// <returns></returns>
        public static async Task<DnsTask> GetDnsAuthInfoAsync(AcmeContext acme, string domains)
        {
            var domainArray = domains.Split(' ');
            var order = await acme.NewOrder(domainArray);
            var authorizationContexts = await order.Authorizations();
            var dnsChallenges = new IChallengeContext[domainArray.Length];
            var dnsTxts = new string[domainArray.Length];

            for (int i = 0; i < authorizationContexts.Count(); i++)
            {
                var authorizationContext = authorizationContexts.ElementAt(i);
                dnsChallenges[i] = await authorizationContext.Dns();
                dnsTxts[i] = acme.AccountKey.DnsTxt(dnsChallenges[i].Token);
            }

            return new DnsTask(dnsChallenges, order, dnsTxts);
        }



        /// <summary>
        /// 获取Acme登录后对象
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <param name="pemKeyFile">邮箱账户pem密钥文件地址</param>
        /// <returns></returns>
        public static async Task<AcmeAccount> GetAcmeAccountAsync(string email, string pemKeyFile)
        {
            string pemKey = File.Exists(pemKeyFile) ? await File.ReadAllTextAsync(pemKeyFile) : "";
#if DEBUG
            var acme = pemKey == "" ? new AcmeContext(WellKnownServers.LetsEncryptStagingV2) : new AcmeContext(WellKnownServers.LetsEncryptStagingV2, KeyFactory.FromPem(pemKey));
# else
            var acme = pemKey == "" ? new AcmeContext(WellKnownServers.LetsEncryptV2) : new AcmeContext(WellKnownServers.LetsEncryptV2, KeyFactory.FromPem(pemKey));
#endif
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
            /// <summary>
            /// 证书新建或更新成功后调用的脚本文件
            /// </summary>
            public string okshell { get; set; }
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
