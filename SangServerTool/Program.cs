using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SangServerTool;
using SangServerTool.Tool;


ServiceCollection services = new();
services.AddLogging(logBuilder => {
    logBuilder.AddSimpleConsole(opt => {
        opt.SingleLine = true;
        opt.IncludeScopes = true;
        opt.TimestampFormat = "hh:mm:ss ";
    });
});

using var sp = services.BuildServiceProvider();
ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();

return await Parser.Default.ParseArguments<AUTO_DDNS, AUTO_SSL>(args)
.MapResult(
async (AUTO_DDNS opt) => {
    var logger = loggerFactory.CreateLogger("SangServerTool_DDNS");
    return await DDNS.Run(opt, logger);
},
async (AUTO_SSL opt) => {
    var logger = loggerFactory.CreateLogger("SangServerTool_SSL");
    return await SSL.Run(opt, logger); 
},
async errs =>
{
    await Task.Delay(100);
    return 1;
});