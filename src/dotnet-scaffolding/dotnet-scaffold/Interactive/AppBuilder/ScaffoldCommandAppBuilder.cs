// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Services;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;

/// <summary>
/// Builds and configures the <see cref="ScaffoldCommandApp"/>, including service registration and command setup.
/// </summary>
internal class ScaffoldCommandAppBuilder(IScaffoldRunner runnner, string[] args)
{
    // Command-line arguments passed to the application.
    private readonly string[] _args = args;
    // Backup version string for dotnet-scaffold, updated every release.
    private readonly string _backupDotNetScaffoldVersion = "10.0.0";

    private readonly IScaffoldRunner _scaffoldRunner = runnner;

    /// <summary>
    /// Builds and configures the <see cref="ScaffoldCommandApp"/> with all required services and commands.
    /// </summary>
    /// <returns>A configured <see cref="ScaffoldCommandApp"/> instance.</returns>
    public ScaffoldCommandApp Build(string? startupErrors = null)
    {
        TypeRegistrar? serviceRegistrations = GetDefaultServices(startupErrors);
        CommandApp<ScaffoldCommand> commandApp = new(serviceRegistrations);
        commandApp.Configure(config =>
        {
            config
                .SetApplicationName("dotnet scaffold")
                .SetApplicationVersion(ToolHelper.GetToolVersion() ?? _backupDotNetScaffoldVersion)
                .AddBranch<ToolSettings>("tool", tool =>
                {
                    tool.AddCommand<ToolInstallCommand>("install");
                    tool.AddCommand<ToolListCommand>("list");
                    tool.AddCommand<ToolUninstallCommand>("uninstall");
                });
        });

        return new ScaffoldCommandApp(commandApp, _args);
    }

    /// <summary>
    /// Registers the default services required for the scaffold command application.
    /// </summary>
    /// <returns>A <see cref="TypeRegistrar"/> with all required services registered.</returns>
    private TypeRegistrar? GetDefaultServices(string? startupErrors = null)
    {
        TypeRegistrar registrar = new TypeRegistrar();
        registrar.Register(typeof(IFileSystem), typeof(FileSystem));
        registrar.Register(typeof(IEnvironmentService), typeof(EnvironmentService));
        registrar.Register(typeof(IFlowProvider), typeof(FlowProvider));
        registrar.Register(typeof(IDotNetToolService), typeof(DotNetToolService));
        registrar.Register(typeof(IToolManager), typeof(ToolManager));
        registrar.Register(typeof(IToolManifestService), typeof(ToolManifestService));
        registrar.Register(typeof(IFirstTimeUseNoticeSentinel), typeof(FirstTimeUseNoticeSentinel));

        // Create the instance of StartUpErrorService and set the error if provided
        StartUpErrorService startUpErrorService = new StartUpErrorService();
        if (!string.IsNullOrWhiteSpace(startupErrors))
        {
            startUpErrorService.SetError(startupErrors);
        }
        registrar.RegisterInstance(typeof(IStartUpErrorService), startUpErrorService);

        // Register a lazy singleton for the first time use notice sentinel.
        registrar.RegisterLazy(typeof(IFirstTimeUseNoticeSentinel), (serviceProvider) =>
        {
            return new FirstTimeUseNoticeSentinel(serviceProvider.GetRequiredService<IFileSystem>(),
                                                  serviceProvider.GetRequiredService<IEnvironmentService>(),
                                                  "dotnetScaffold");
        });
        registrar.Register(typeof(ITelemetryService), typeof(TelemetryService));
        registrar.RegisterInstance(typeof(IScaffoldRunner), _scaffoldRunner);
        return registrar;
    }
}
