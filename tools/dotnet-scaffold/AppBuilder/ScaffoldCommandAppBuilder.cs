// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold;

public class ScaffoldCommandAppBuilder(string[] args)
{
    private readonly string[] _args = args;
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
/*        var logger = new AnsiConsoleLogger();
        var envProviders = new List<IEnvironmentVariableProvider>()
        {
            new MacMsbuildEnvironmentVariableProvider()
        };*/

        registrar.Register(typeof(IFileSystem), typeof(FileSystem));
        registrar.Register(typeof(IEnvironmentService), typeof(EnvironmentService));
        registrar.Register(typeof(IFlowProvider), typeof(FlowProvider));
        registrar.Register(typeof(IDotNetToolService), typeof(DotNetToolService));
        registrar.Register(typeof(ILogger), typeof(AnsiConsoleLogger));
//        registrar.RegisterInstance(typeof(IHostService), new HostService(logger, );
        return registrar;
    }

    private string GetToolVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return assemblyAttr?.InformationalVersion ?? _backupDotNetScaffoldVersion;
    }
}
