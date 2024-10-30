// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold;

internal class ScaffoldCommandAppBuilder(string[] args)
{
    private readonly string[] _args = args;
    //try to update this every release
    private readonly string _backupDotNetScaffoldVersion = "9.0.0";

    public ScaffoldCommandApp Build()
    {
        var serviceRegistrations = GetDefaultServices();
        var commandApp = new CommandApp<ScaffoldCommand>(serviceRegistrations);
        commandApp.Configure(config =>
        {
            config
                .SetApplicationName("dotnet-scaffold")
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

    private static TypeRegistrar? GetDefaultServices()
    {
        var registrar = new TypeRegistrar();
        registrar.Register(typeof(IFileSystem), typeof(FileSystem));
        registrar.Register(typeof(IEnvironmentService), typeof(EnvironmentService));
        registrar.Register(typeof(IFlowProvider), typeof(FlowProvider));
        registrar.Register(typeof(IDotNetToolService), typeof(DotNetToolService));
        registrar.Register(typeof(IToolManager), typeof(ToolManager));
        registrar.Register(typeof(IToolManifestService), typeof(ToolManifestService));
        registrar.Register(typeof(IFirstTimeUseNoticeSentinel), typeof(FirstTimeUseNoticeSentinel));
        registrar.RegisterLazy(typeof(IFirstTimeUseNoticeSentinel), (serviceProvider) =>
        {
            return new FirstTimeUseNoticeSentinel(serviceProvider.GetRequiredService<IFileSystem>(),
                                                  serviceProvider.GetRequiredService<IEnvironmentService>(),
                                                  "dotnetScaffold");
        });
        registrar.Register(typeof(ITelemetryService), typeof(TelemetryService));
        return registrar;
    }
}
