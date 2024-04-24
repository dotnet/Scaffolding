using Microsoft.DotNet.Scaffolding.Helpers.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Spectre.Console.Cli;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;
using System.Diagnostics;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet;
public static class Program
{
    public static void Main(string[] args)
    {
        //Debugger.Launch();
        var serviceRegistrations = GetDefaultServices();
        var app = new CommandApp(serviceRegistrations);
        app.Configure(config =>
        {
            config.AddCommand<MinimalApiCommand>("minimalapi");
            config.AddCommand<AreaCommand>("area");
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
        registrar.Register(typeof(IEnvironmentVariableProvider), typeof(MacMsbuildEnvironmentVariableProvider));
        registrar.Register(typeof(IEnvironmentVariableProvider), typeof(WindowsEnvironmentVariableProvider));
        registrar.Register(typeof(IHostService), typeof(HostService));
        registrar.Register(typeof(ICodeService), typeof(CodeService));
        return registrar;
    }
}

