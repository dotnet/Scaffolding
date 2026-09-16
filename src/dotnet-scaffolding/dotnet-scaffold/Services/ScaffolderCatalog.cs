// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Combines the scaffolding components supplied by registered providers.
/// </summary>
internal sealed class ScaffolderCatalog
{
    private readonly Lazy<IReadOnlyList<ScaffolderComponent>> _components;

    public ScaffolderCatalog(IEnumerable<IScaffolderProvider> providers)
    {
        _components = new(() => providers.SelectMany(provider => provider.GetComponents()).ToList());
    }

    public IReadOnlyList<ScaffolderComponent> GetComponents()
        => _components.Value;

    public IList<KeyValuePair<string, CommandInfo>> GetCommands()
        => GetComponents()
            .SelectMany(component => component.Commands.Select(
                command => KeyValuePair.Create(component.Component.Command, command)))
            .ToList();

    public ScaffolderComponent? FindComponent(string? command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return null;
        }

        return GetComponents().FirstOrDefault(component =>
            string.Equals(component.Component.Command, command, StringComparison.OrdinalIgnoreCase));
    }
}
