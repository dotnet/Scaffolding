// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Reflection;
using Microsoft.DotNet.Scaffolding.Helpers.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold;

public class ScaffoldCommandAppBuilder(string[] args)
{
    private readonly string[] _args = args;
    //try to update this every release
    private readonly string _backupDotNetScaffoldVersion = "0.1.0-dev";

    public ScaffoldCommandApp Build()
    {
        var serviceRegistrations = GetDefaultServices();
        var commandApp = new CommandApp<ScaffoldCommand>(serviceRegistrations);
        commandApp.Configure(config =>
        {
            config
                .SetApplicationName("dotnet-scaffold")
                .SetApplicationVersion(GetToolVersion());
        });

        return new ScaffoldCommandApp(commandApp, _args);
    }

    private ITypeRegistrar? GetDefaultServices()
    {
        var registrar = new TypeRegistrar();
        registrar.Register(typeof(IFileSystem), typeof(FileSystem));
        registrar.Register(typeof(IEnvironmentService), typeof(EnvironmentService));
        registrar.Register(typeof(IFlowProvider), typeof(FlowProvider));
        registrar.Register(typeof(IDotNetToolService), typeof(DotNetToolService));
        registrar.Register(typeof(ILogger), typeof(AnsiConsoleLogger));
        registrar.Register(typeof(IAppSettings), typeof(AppSettings));
        registrar.Register(typeof(IEnvironmentVariableProvider), typeof(MacMsbuildEnvironmentVariableProvider));
        registrar.Register(typeof(IEnvironmentVariableProvider), typeof(WindowsEnvironmentVariableProvider));
        registrar.Register(typeof(IHostService), typeof(HostService));
        return registrar;
    }

    private string GetToolVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return assemblyAttr?.InformationalVersion ?? _backupDotNetScaffoldVersion;
    }
}
