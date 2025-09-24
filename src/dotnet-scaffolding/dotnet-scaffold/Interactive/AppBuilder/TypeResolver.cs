// Copyright (c) Microsoft Corporation. All rights reserved.

using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;

/// <summary>
/// Resolves service types using the provided <see cref="IServiceProvider"/>.
/// Used by Spectre.Console.Cli for dependency injection.
/// </summary>
internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    // The service provider used to resolve dependencies.
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving services.</param>
    public TypeResolver(IServiceProvider serviceProvider)
    {
        _provider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Resolves an instance of the specified type from the service provider.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>An instance of the specified type, or null if not found.</returns>
    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    /// <summary>
    /// Disposes the underlying service provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
