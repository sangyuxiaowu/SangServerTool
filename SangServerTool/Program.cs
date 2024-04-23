﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SangServerTool;
using SangServerTool.Tool;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;


ServiceCollection services = new();
services.AddLogging(logBuilder => {
    logBuilder.AddSimpleConsole(opt => {
        opt.SingleLine = true;
        opt.IncludeScopes = true;
        opt.TimestampFormat = "HH:mm:ss ";
    });
});

using var sp = services.BuildServiceProvider();
ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();

// 创建根命令
var rootCommand = new RootCommand("SangServerTool");

// 定义ssl命令
var sslCommand = new Command("ssl", "Get Let's Encrypt SSL Cert.");
sslCommand.AddOption(new Option<string>(new[]{"--config", "-c"}, "Set config json file.") { IsRequired = true });
sslCommand.AddOption(new Option<int>("--retry", () => 8, "How many retries?"));
sslCommand.AddOption(new Option<int>("--delay", () => 5, "How many seconds to retry?"));
sslCommand.Handler = CommandHandler.Create<string, int, int>(async (config, retry, delay) =>
{
    var opt = new AUTO_SSL { ConfigFile = config, Retry = retry, Delay = delay };
    var logger = loggerFactory.CreateLogger("SangServerTool_SSL");
    return await SSL.Run(opt, logger);
});
rootCommand.AddCommand(sslCommand);

// 定义ddns命令
var ddnsCommand = new Command("ddns", "Set DDNS.");
ddnsCommand.AddOption(new Option<string>(new[] { "--config", "-c" }, "Set config json file.") { IsRequired = true});
ddnsCommand.AddOption(new Option<int>("--delay", () => 0, "How many seconds delay?"));
ddnsCommand.AddOption(new Option<bool>("--del", () => false, "Is delete DDNS?"));
ddnsCommand.AddOption(new Option<bool>("--v6", () => false, "Is ipv6?"));
ddnsCommand.AddOption(new Option<string>("--ip", () => "", "If set will be used. Otherwise automatically obtained.\n You can set 'ifconfig', It will check from 'https://ifconfig.me/ip' to get you Internet IP."));
ddnsCommand.Handler = CommandHandler.Create<string, int, bool, bool, string>(async (config, delay, del, v6, ip) =>
{
    var opt = new AUTO_DDNS { ConfigFile = config, Delay = delay, Del = del, IPV6 = v6, IP = ip };
    var logger = loggerFactory.CreateLogger("SangServerTool_DDNS");
    return await DDNS.Run(opt, logger);
});
rootCommand.AddCommand(ddnsCommand);

// 定义获取 https 站点证书命令
var getcertCommand = new Command("getcert", "Get SSL Cert from https site.");
getcertCommand.AddOption(new Option<string>(new[] { "--config", "-c" }, "Set config json file.") { IsRequired = true });
getcertCommand.Handler = CommandHandler.Create<string>(async (config) =>
{
    var logger = loggerFactory.CreateLogger("SangServerTool_GetCert");
    var getCert = new GetCert(logger);
    return await getCert.Run(config);
});
rootCommand.AddCommand(getcertCommand);

// 解析并执行命令
return await rootCommand.InvokeAsync(args);