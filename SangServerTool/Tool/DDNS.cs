using SangServerTool.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SangServerTool.Tool
{
    /// <summary>
    /// DDNS 动态域名解析
    /// </summary>
    public class DDNS
    {

        public async static Task<int> Run(AUTO_DDNS opt,ILogger logger)
        {
            logger.LogInformation($"配置文件：{opt.ConfigFile}");
            if (!File.Exists(opt.ConfigFile))
            {
                logger.LogWarning("配置文件不存在");
                return 1;
            }

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(opt.ConfigFile, optional: false, reloadOnChange: false);
            IConfigurationRoot config = configBuilder.Build();

            var al = new AliyunDomain(config["Access:AK"], config["Access:SK"]);

            logger.LogInformation($"检查域名当前解析：{config["DDNS:ddns"]}");

            // 检查DDNS的解析信息
            var Record = await al.GetRecordsAsync(config["DDNS:ddns"], "");
            //出错
            if (!Record.Success)
            {
                logger.LogError(Record.Msg);
                return 1;
            }

            // 进入删除操作
            if (opt.Del)
            {
                if (Record.Id != "")
                {
                    var DelRecord = await al.DelRecordsAsync(Record.Id);
                    if (DelRecord.Success)
                    {
                        logger.LogInformation($"删除DDNS解析成功：{DelRecord.Id}");
                        return 0;
                    }
                    logger.LogError($"删除DDNS解析失败：{DelRecord.Msg}");
                    return 1;
                }
                logger.LogInformation($"无需删除记录");
                return 0;
            }

            var nowip = opt.IP == "" ? Utils.CurrentIPAddress(opt.IPV6)
            : opt.IP == "ifconfig" ? Utils.CurrentIPAddress() : opt.IP;

            //检查IP是否合规
            if (!System.Net.IPAddress.TryParse(nowip, out _))
            {
                logger.LogError($"设置解析IP配置获取失败，获取的IP信息 {nowip} 不是有效的IP地址");
                return 1;
            }

            logger.LogInformation($"获取IP地址为：{nowip}");

            //获取 RR 设置
            string RR = Utils.GetRRDdns(config["DDNS:ddns"], config["DDNS:basedomain"]);
            if (string.IsNullOrEmpty(RR))
            {
                logger.LogError("配置解析：配置的DDNS解析或Domain域名异常");
                return 1;
            }

            // 要解析的域名类型
            string Type = nowip.Length > 16 ? "AAAA" : "A";

            //域名没有解析记录，新建解析
            if (Record.Id == "")
            {


                logger.LogInformation($"准备解析：{RR}\t{Type}\t{nowip}");
                var AddRecord = await al.AddRecordsAsync(config["DDNS:basedomain"], RR, Type, nowip);
                if (AddRecord.Success)
                {
                    logger.LogInformation($"新建解析成功：{AddRecord.Id}");
                    return 0;
                }
                logger.LogError($"新建解析失败：{AddRecord.Msg}");
                return 1;
            }

            logger.LogInformation($"原解析地址为：{Record.Value}");

            //修改记录
            if (Record.Value != nowip)
            {
                logger.LogDebug("修改解析记录");
                var UpdateRecord = await al.UpdateRecordsAsync(Record.Id, RR, Type, nowip);
                if (UpdateRecord.Success)
                {
                    logger.LogInformation($"修改解析成功：{UpdateRecord.Id}");
                    return 0;
                }
                logger.LogError($"修改解析失败：{UpdateRecord.Msg}");
                return 1;
            }
            logger.LogInformation("无需处理");
            return 0;
        }
    }
}
