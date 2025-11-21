// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

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
    private readonly Dictionary<ScaffolderCatagory, List<ScaffoldBuilder>> _scaffoldBuilders = [];

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
    public IReadOnlyDictionary<ScaffolderCatagory, List<ScaffoldBuilder>> Scaffolders => _scaffoldBuilders;
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

        // Build all scaffolders and add them to the commands for each subcommand
        Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>> builtScaffolders = new();
        foreach (KeyValuePair<ScaffolderCatagory, List<ScaffoldBuilder>> kvp in Scaffolders)
        {
            ScaffolderCatagory category = kvp.Key;
            List<ScaffoldBuilder> builders = kvp.Value;
            var builtList = new List<IScaffolder>();
            foreach (ScaffoldBuilder builder in builders)
            {
                builtList.Add(builder.Build(_appServices));
            }
            builtScaffolders[category] = builtList;
        }
        scaffoldRunner.Scaffolders = builtScaffolders;

        scaffoldRunner.Options = _options;

        scaffoldRunner.BuildRootCommand();
        return scaffoldRunner;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder AddScaffolder(ScaffolderCatagory category, string name)
    {
        var scaffoldBuilder = new ScaffoldBuilder(name);
        if (!_scaffoldBuilders.TryGetValue(category, out var builders))
        {
            builders = new List<ScaffoldBuilder>();
            _scaffoldBuilders[category] = builders;
        }
        builders.Add(scaffoldBuilder);
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
    /// Adds an action to the RootCommand doing the action passed in the handle parameter.
    /// </summary>
    public void AddHandler(Func<ParseResult, CancellationToken, Task> handle)
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
