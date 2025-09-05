// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.DotNet.Tools.Scaffold.AspNet.AppBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AppBuilder;

/// <summary>
/// Provides a custom type registrar for dependency injection, used by Spectre.Console.Cli to resolve services.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    // The service collection for registering dependencies.
    private readonly IServiceCollection _services;
    // The logging builder for configuring logging services.
    private readonly ILoggingBuilder _logging;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class and sets up logging.
    /// </summary>
    public TypeRegistrar()
    {
        _services = new ServiceCollection();
        _logging = new LoggingBuilder(_services);
        _services.AddLogging();
        _logging.AddCleanConsoleFormatter();
    }

    /// <summary>
    /// Builds the type resolver from the registered services.
    /// </summary>
    /// <returns>An <see cref="ITypeResolver"/> for resolving services.</returns>
    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <summary>
    /// Registers a service and its implementation as a singleton.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="implementation">The implementation type.</param>
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a specific instance as a singleton for the given service type.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="implementation">The implementation instance.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a service with a factory function that creates the instance lazily.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="func">The factory function.</param>
    public void RegisterLazy(Type service, Func<object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _services.AddSingleton(service, (provider) => func());
    }

    /// <summary>
    /// Registers a service with a factory function that receives the service provider and creates the instance lazily.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="func">The factory function with service provider.</param>
    public void RegisterLazy(Type service, Func<IServiceProvider, object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _services.AddSingleton(service, func);
    }

    /// <summary>
    /// Private helper class for logging builder implementation.
    /// </summary>
    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
