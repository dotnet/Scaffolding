using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire;
public static class Program
{
    public static void Main(string[] args)
    {
        var serviceRegistrations = GetDefaultServices();
        var app = new CommandApp(serviceRegistrations);
        app.Configure(config =>
        {
            config.AddCommand<CachingCommand>("caching");
            config.AddCommand<DatabaseCommand>("database");
            config.AddCommand<StorageCommand>("storage");
            config.AddCommand<GetCmdsCommand>("get-commands");
        });

        app.Run(args);
    }

    private static ITypeRegistrar? GetDefaultServices()
    {
        var registrar = new TypeRegistrar();
        registrar.Register(typeof(IAppSettings), typeof(AppSettings));
        registrar.Register(typeof(IFileSystem), typeof(FileSystem));
        registrar.Register(typeof(IEnvironmentService), typeof(EnvironmentService));
        registrar.Register(typeof(IDotNetToolService), typeof(DotNetToolService));
        registrar.Register(typeof(ILogger), typeof(AnsiConsoleLogger));
        registrar.Register(typeof(IHostService), typeof(HostService));
        registrar.Register(typeof(ICodeService), typeof(CodeService));
        return registrar;
    }
}

