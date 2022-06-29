using CommandLine;
using SangServerTool;

return Parser.Default.ParseArguments<AUTO_DDNS, AUTO_SSL>(args)
    .MapResult((AUTO_DDNS opt) => {
        Console.WriteLine(opt.ToString());
        return 1;
    },
    (AUTO_SSL opt) => {
        Console.WriteLine(opt.ToString());
        return 1;
    },errs => 1);