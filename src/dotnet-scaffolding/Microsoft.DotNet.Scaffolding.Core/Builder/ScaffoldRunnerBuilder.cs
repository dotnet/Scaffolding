// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal class ScaffoldRunnerBuilder : IScaffoldRunnerBuilder
{
    private readonly ServiceCollection _serviceCollection = new();
    private readonly LoggingBuilder _logging;
    private readonly List<ScaffoldBuilder> _scaffoldBuilders = [];

    private IServiceProvider? _appServices;
    private bool _built;

    public ScaffoldRunnerBuilder()
    {
        _logging = new LoggingBuilder(Services);

        AddDefaultServices();
    }

    public ILoggingBuilder Logging => _logging;
    public IServiceCollection Services => _serviceCollection;
    public IEnumerable<IScaffoldBuilder> Scaffolders => _scaffoldBuilders;

    public IScaffoldRunner Build()
    {
        if (_built)
        {
            throw new InvalidOperationException("Build already called.");
        }
        _built = true;

        _appServices = Services.BuildServiceProvider();

        _serviceCollection.MakeReadOnly();

        var scaffoldRunner = _appServices.GetRequiredService<IScaffoldRunner>();

        scaffoldRunner.Scaffolders = Scaffolders.Select(s => s.Build(_appServices));

        scaffoldRunner.BuildRootCommand();

        return scaffoldRunner;
    }

    public IScaffoldBuilder AddScaffolder(string name)
    {
        var scaffoldBuilder = new ScaffoldBuilder(name);
        _scaffoldBuilders.Add(scaffoldBuilder);
        return scaffoldBuilder;
    }

    private void AddDefaultServices()
    {
        AddCoreServices();
        Services.AddSingleton<IScaffoldRunner, ScaffoldRunner>();
    }

    private void AddCoreServices()
    {
        Services.AddLogging();
        Logging.AddCleanConsoleFormatter();
        Logging.AddDebug();
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
