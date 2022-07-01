using CommandLine;
using SangServerTool;
using SangServerTool.Tool;

return await Parser.Default.ParseArguments<AUTO_DDNS, AUTO_SSL>(args)
    .MapResult(
    async (AUTO_DDNS opt) =>await DDNS.Run(opt),
    async (AUTO_SSL opt) => await SSL.Run(opt),
    async errs => {
        await Task.Delay(100);
        return 1;
    });
