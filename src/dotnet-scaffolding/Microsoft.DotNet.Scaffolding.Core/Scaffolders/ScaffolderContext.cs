// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

/// <summary>
/// Provides the context for a scaffolder execution, including properties and option results.
/// </summary>
public class ScaffolderContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScaffolderContext"/> class.
    /// </summary>
    /// <param name="scaffolder">The scaffolder associated with this context.</param>
    internal ScaffolderContext(IScaffolder scaffolder)
    {
        Scaffolder = scaffolder;
    }

    /// <summary>
    /// Gets the scaffolder associated with this context.
    /// </summary>
    public IScaffolder Scaffolder { get; }

    // TODO: add a 'T GetProperty<T>(string)' to easily fetch values from the 'Properties' bucket.

    /// <summary>
    /// Gets the dictionary of properties for this context.
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = [];

    /// <summary>
    /// Gets the dictionary of option results for this context.
    /// </summary>
    public Dictionary<ScaffolderOption, object?> OptionResults { get; } = [];

    /// <summary>
    /// Gets the result value for a specific option.
    /// </summary>
    /// <typeparam name="T">The type of the option result.</typeparam>
    /// <param name="option">The scaffolder option.</param>
    /// <returns>The result value if present; otherwise, default.</returns>
    public T? GetOptionResult<T>(ScaffolderOption<T> option)
    {
        if (OptionResults.TryGetValue(option, out var value))
        {
            return (T?)value;
        }

        return default;
    }

    /// <summary>
    /// Gets the result value for a specific option by CLI name.
    /// </summary>
    /// <typeparam name="T">The type of the option result.</typeparam>
    /// <param name="optionCliName">The CLI name of the option.</param>
    /// <returns>The result value if present; otherwise, default.</returns>
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
