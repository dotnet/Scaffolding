// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.DotNet.Tools.Scaffold.AspNet.AppBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AppBuilder;

internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;
    private readonly ILoggingBuilder _logging;

    public TypeRegistrar()
    {
        _services = new ServiceCollection();
        _logging = new LoggingBuilder(_services);
        _services.AddLogging();
        //_logging.AddCleanConsoleFormatter();
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _services.AddSingleton(service, (provider) => func());
    }

    public void RegisterLazy(Type service, Func<IServiceProvider, object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _services.AddSingleton(service, func);
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
