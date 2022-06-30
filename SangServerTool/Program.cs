using CommandLine;
using SangServerTool;
using SangServerTool.Domain;
using Microsoft.Extensions.Configuration;


return Parser.Default.ParseArguments<AUTO_DDNS, AUTO_SSL>(args)
    .MapResult((AUTO_DDNS opt) =>
    {
        
        Console.WriteLine($"配置文件：{opt.ConfigFile}");
        if (!File.Exists(opt.ConfigFile)) {
            Console.WriteLine("配置文件不存在");
            return 1;
        }

        IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile(opt.ConfigFile, optional: false, reloadOnChange: false);
        IConfigurationRoot config = configBuilder.Build();
  
        var al = new AliyunDomain(config["AK"], config["SK"]);

        Console.WriteLine($"检查解析：{config["DDNS"]}");

        var Record = al.GetRecordsAsync(config["DDNS"], "").Result;
        //出错
        if (!Record.Success)
        {
            Console.WriteLine(Record.Msg);
            return 1;
        }
        
        var nowip = opt.IP == "" ? Utils.CurrentIPAddress(opt.IPV6) 
        : (opt.IP== "ifconfig" ? Utils.CurrentIPAddress() : opt.IP);

        //检查IP是否合规
        if (!System.Net.IPAddress.TryParse(nowip,out _)) {
            Console.WriteLine("设置解析IP配置获取失败");
            return 1;
        }

        Console.WriteLine($"设置解析：{nowip}");
        
        //域名没有解析记录，新建解析
        if (Record.Id == "") {
            Console.WriteLine("新建记录");

            //获取设置
            string RR = Utils.GetRRDdns(config["DDNS"], config["Domain"]);
            if (string.IsNullOrEmpty(RR)) {
                Console.WriteLine("配置的DDNS解析或Domain域名异常");
                return 1;
            }
            string Type = nowip.Length > 16 ? "AAAA" : "A";
            Console.WriteLine($"准备解析：{RR}\t{Type}\t{nowip}");
            var AddRecord = al.AddRecordsAsync(config["Domain"], RR, Type, nowip).Result;
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
        if (Record.Value != nowip) {
            Console.WriteLine("修改记录");

        }
        
        return 0;
    },
    (AUTO_SSL opt) =>
    {
        Console.WriteLine(opt.ToString());
        return 1;
    }, errs => 1);
