// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

public class ScaffolderContext
{
    internal ScaffolderContext(IScaffolder scaffolder)
    {
        Scaffolder = scaffolder;
    }

    public IScaffolder Scaffolder { get; }
    public Dictionary<string, object?> Properties { get; } = [];
    public Dictionary<ScaffolderOption, object?> OptionResults { get; } = [];

    public T? GetOptionResult<T>(ScaffolderOption<T> option)
    {
        if (OptionResults.TryGetValue(option, out var value))
        {
            return (T?)value;
        }

        return default;
    }

    public T? GetOptionResult<T>(string optionCliName)
    {
        var optionResult = OptionResults.FirstOrDefault(x => !string.IsNullOrEmpty(x.Key.CliOption) &&  x.Key.CliOption.Equals(optionCliName, StringComparison.OrdinalIgnoreCase));
        if (optionResult.Value is not null)
        {
            return (T?)optionResult.Value;
        }

        return default;
    }
}
