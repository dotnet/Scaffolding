// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Builder for configuring and creating a scaffold runner with services and scaffolders.
/// </summary>
internal class ScaffoldRunnerBuilder : IScaffoldRunnerBuilder
{
    // Service collection for dependency injection
    private readonly ServiceCollection _serviceCollection = new();
    // Logging builder for configuring logging
    private readonly LoggingBuilder _logging;
    // List of scaffold builders
    private readonly List<ScaffoldBuilder> _scaffoldBuilders = [];

    private List<ScaffolderOption>? _options;

    private IServiceProvider? _appServices;
    private bool _built;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScaffoldRunnerBuilder"/> class.
    /// </summary>
    public ScaffoldRunnerBuilder()
    {
        _logging = new LoggingBuilder(Services);
        AddDefaultServices();
    }

    /// <inheritdoc/>
    public ILoggingBuilder Logging => _logging;
    /// <inheritdoc/>
    public IServiceCollection Services => _serviceCollection;
    /// <inheritdoc/>
    public IEnumerable<IScaffoldBuilder> Scaffolders => _scaffoldBuilders;
    /// <inheritdoc/>
    public IServiceProvider? ServiceProvider
    {
        get
        {
            if (!_built)
            {
                return null;
            }
            return _appServices!;
        }
    }

    /// <inheritdoc/>
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

        scaffoldRunner.Options = _options;

        scaffoldRunner.BuildRootCommand();
        return scaffoldRunner;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder AddScaffolder(string name)
    {
        var scaffoldBuilder = new ScaffoldBuilder(name);
        _scaffoldBuilders.Add(scaffoldBuilder);
        return scaffoldBuilder;
    }

    /// <summary>
    /// Add a new <see cref="ScaffolderOption"/> to the builder.
    /// </summary>
    public void AddOption(ScaffolderOption option)
    {
        if (_options is null)
        {
            _options = [option];
        }
        else
        {
            _options.Add(option);
        }
    }

    /// <summary>
    /// Adds a handler to the RootCommand doing the action passed in the handle parameter.
    /// </summary>
    public void AddHandler(Func<InvocationContext, Task> handle)
    {
        if (_appServices is null)
        {
            return;
        }
        IScaffoldRunner scaffoldRunner = _appServices.GetRequiredService<IScaffoldRunner>();
        scaffoldRunner.AddHandler(handle);
    }


    // Adds default services required for scaffolding
    private void AddDefaultServices()
    {
        AddCoreServices();
        Services.AddSingleton<IScaffoldRunner, ScaffoldRunner>();
    }

    // Adds core services such as logging
    private void AddCoreServices()
    {
        Services.AddLogging();
        Logging.AddCleanConsoleFormatter();
        Logging.AddDebug();
    }

    /// <summary>
    /// Internal logging builder implementation for DI.
    /// </summary>
    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
