// Copyright (c) Microsoft Corporation. All rights reserved.

using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.AppBuilder;

internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider serviceProvider)
    {
        _provider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
