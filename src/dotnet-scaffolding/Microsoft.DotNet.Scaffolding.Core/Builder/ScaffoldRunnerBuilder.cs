// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.DotNet.Scaffolding.Internal.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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
        var loggerConfig = GetLoggingConfiguration();
        Services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(loggerConfig.CreateLogger(), dispose: true);
        });

        Logging.AddDebug();
    }

    private LoggerConfiguration GetLoggingConfiguration()
    {
        //get some logging properties from environment variables
        var isVerboseEnabled = EnvironmentHelpers.GetEnvironmentVariableAsBool(ScaffolderConstants.ENABLE_VERBOSE_LOGGING);
        var isLogToFileEnabled = EnvironmentHelpers.GetEnvironmentVariableAsBool(ScaffolderConstants.LOG_TO_FILE);
        // Configure Serilog Logger
        var loggerConfig = new LoggerConfiguration().WriteTo.Sink(new AnsiConsoleSink());
        loggerConfig.MinimumLevel.Information();
        if (isVerboseEnabled)
        {
            loggerConfig.MinimumLevel.Verbose();
        }

        if (isLogToFileEnabled)
        {
            var loggingDirectory = Path.Combine(EnvironmentHelpers.GetUserProfilePath(), _defaultLogFolder);
            if (!Directory.Exists(loggingDirectory))
            {
                Directory.CreateDirectory(loggingDirectory);
            }

            var filePath = $"dotnet-scaffold-{DateTime.Now:yyyy-MM-dd_HH-mm}.log";
            var logPath = StringUtil.GetUniqueFilePath(Path.Combine(loggingDirectory, filePath));
            loggerConfig.WriteTo.File(logPath, rollingInterval: RollingInterval.Infinite);
        }

        return loggerConfig;
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    private readonly string _defaultLogFolder = Path.Combine(".dotnet-scaffold", ".logs");
}
