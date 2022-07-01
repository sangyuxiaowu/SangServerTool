using SangServerTool.Domain;
using Microsoft.Extensions.Configuration;

namespace SangServerTool.Tool
{
    /// <summary>
    /// DDNS 动态域名解析
    /// </summary>
    public class DDNS
    {
        public async static Task<int> Run(AUTO_DDNS opt)
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

            var al = new AliyunDomain(config["Access:AK"], config["Access:SK"]);

            Console.WriteLine($"检查解析：{config["DDNS:ddns"]}");

            // 检查DDNS的解析信息
            var Record = await al.GetRecordsAsync(config["DDNS:ddns"], "");
            //出错
            if (!Record.Success)
            {
                Console.WriteLine(Record.Msg);
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
                        Console.WriteLine($"删除成功：{DelRecord.Id}");
                        return 0;
                    }
                    Console.WriteLine($"删除失败：{DelRecord.Msg}");
                    return 1;
                }
                Console.WriteLine($"无需删除");
                return 0;
            }

            var nowip = opt.IP == "" ? Utils.CurrentIPAddress(opt.IPV6)
            : opt.IP == "ifconfig" ? Utils.CurrentIPAddress() : opt.IP;

            //检查IP是否合规
            if (!System.Net.IPAddress.TryParse(nowip, out _))
            {
                Console.WriteLine("设置解析IP配置获取失败");
                return 1;
            }

            Console.WriteLine($"设置解析：{nowip}");

            //获取 RR 设置
            string RR = Utils.GetRRDdns(config["DDNS:ddns"], config["DDNS:basedomain"]);
            if (string.IsNullOrEmpty(RR))
            {
                Console.WriteLine("配置解析：配置的DDNS解析或Domain域名异常");
                return 1;
            }

            // 要解析的域名类型
            string Type = nowip.Length > 16 ? "AAAA" : "A";

            //域名没有解析记录，新建解析
            if (Record.Id == "")
            {


                Console.WriteLine($"准备解析：{RR}\t{Type}\t{nowip}");
                var AddRecord = await al.AddRecordsAsync(config["DDNS:basedomain"], RR, Type, nowip);
                if (AddRecord.Success)
                {
                    Console.WriteLine($"解析成功：{AddRecord.Id}");
                    return 0;
                }
                Console.WriteLine($"解析失败：{AddRecord.Msg}");
                return 1;
            }

            Console.WriteLine($"解析地址：{Record.Value}");

            //修改记录
            if (Record.Value != nowip)
            {
                Console.WriteLine("修改记录");
                var UpdateRecord = await al.UpdateRecordsAsync(Record.Id, RR, Type, nowip);
                if (UpdateRecord.Success)
                {
                    Console.WriteLine($"修改成功：{UpdateRecord.Id}");
                    return 0;
                }
                Console.WriteLine($"修改失败：{UpdateRecord.Msg}");
                return 1;
            }
            Console.WriteLine("无需处理");
            return 0;
        }
    }
}
