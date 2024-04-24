using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
/// <summary>
/// check for first initialization in ValidateUserInputAsync, 
/// do first time initialization in ValidateUserInputAsync
///   - check for .dotnet-scaffold folder in USER
///   - check for .dotnet-scaffold/manifest.json file
///   - initialize msbuild
///   - read and check for 1st party .NET scaffolders, update them if needed
/// </summary>
public class StartupService : IService
{
    private readonly IAppSettings _appSettings;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostService _hostService;
    private readonly ILogger _logger;
    public StartupService(IAppSettings appSettings, IEnvironmentService environmentService, IHostService hostService, ILogger logger)
    {
        _appSettings = appSettings;
        _environmentService = environmentService;
        _hostService = hostService;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        await AnsiConsole.Status()
            .WithSpinner()
            .Start("Initializing dotnet-scaffold-aspnet!", async statusContext =>
            {
                statusContext.Refresh();
                statusContext.Status = "Initializing msbuild!";
                new MsBuildInitializer(_logger).Initialize();
                statusContext.Status = "DONE\n\n";
                statusContext.Status = "Gathering environment variables";
                var environmentVariableProvider = new EnvironmentVariablesStartup(_hostService, _environmentService, _appSettings);
                await environmentVariableProvider.StartupAsync();
                statusContext.Status = "DONE\n\n";
            });
    }
}
