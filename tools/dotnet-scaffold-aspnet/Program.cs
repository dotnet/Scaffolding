using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet;
public static class Program
{
    public static void Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<MinimalApiCommand>("minimalapi");
            config.AddCommand<AreaCommand>("area");
            config.AddCommand<GetCmdsCommand>("get-commands");
        });

        app.Run(args);
    }
}

