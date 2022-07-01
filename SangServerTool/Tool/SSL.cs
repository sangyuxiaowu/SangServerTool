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

            // 是否存在 证书，不存在就直接创建申请了
            bool isHaved = File.Exists(config["Certificate:cerpath"]);
            //获取是否还有5天内就过期
            if (isHaved && !Utils.isCerWillExp(config["Certificate:cerpath"]))
            {
                Console.WriteLine("无需处理");
                return 0;
            }

            var acme = await getAcmeAccountAsync(config["ACME:email"], config["ACME:account"]);

            var order = await acme.NewOrder(new[] { "*.your.domain.name" });

            await Task.Delay(100);
            Console.WriteLine(opt.ToString());
            return 1;
        }


        /// <summary>
        /// 获取Acme登录后对象
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <param name="pemKeyFile">邮箱账户pem密钥文件地址</param>
        /// <returns></returns>
        public static async Task<AcmeContext> getAcmeAccountAsync(string email, string pemKeyFile)
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

            return acme;
        }
    }
}
