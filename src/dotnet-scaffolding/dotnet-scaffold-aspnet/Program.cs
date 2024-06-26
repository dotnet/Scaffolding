using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.API;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MVC;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.RazorPage;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet;
public static class Program
{
    public static void Main(string[] args)
    {
        var serviceRegistrations = GetDefaultServices();
        var app = new CommandApp(serviceRegistrations);
        app.Configure(config =>
        {
            config.AddCommand<BlazorEmptyCommand>("blazor-empty");
            config.AddCommand<MinimalApiCommand>("minimalapi");
            config.AddCommand<ApiControllerEmptyCommand>("apicontroller-empty");
            config.AddCommand<AreaCommand>("area");
            config.AddCommand<MvcControllerEmptyCommand>("mvccontroller-empty");
            config.AddCommand<RazorViewEmptyCommand>("razorview-empty");
            config.AddCommand<RazorPageEmptyCommand>("razorpage-empty");
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

