// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Services;

/// <summary>
/// A factory interface for creating and managing interactive flows in the CLI.
/// </summary>
public interface IFlowProvider
{
    /// <summary>
    /// Gets the current flow object. Only one flow is possible at a time in the CLI context.
    /// </summary>
    IFlow? CurrentFlow { get; }

    /// <summary>
    /// Initializes a new flow runner with the specified steps and properties.
    /// </summary>
    /// <param name="steps">The steps to include in the flow.</param>
    /// <param name="properties">A dictionary of properties for the flow context.</param>
    /// <param name="nonInteractive">Whether the flow should run in non-interactive mode.</param>
    /// <param name="showSelectedOptions">Whether to display selected options in the flow UI.</param>
    /// <returns>The initialized <see cref="IFlow"/> instance.</returns>
    IFlow GetFlow(IEnumerable<IFlowStep> steps, Dictionary<string, object> properties, bool nonInteractive, bool showSelectedOptions = true);
}
