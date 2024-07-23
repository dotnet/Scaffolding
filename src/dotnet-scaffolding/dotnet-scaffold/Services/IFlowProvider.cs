// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Services;
/// <summary>
/// A factory that creates a new flow.
/// </summary>
public interface IFlowProvider
{
    /// <summary>
    /// Since we are in CLI, only one flow is possible at the given moment. This property contains current flow object.
    /// </summary>
    IFlow? CurrentFlow { get; }

    /// <summary>
    /// Initialize the FlowRunner with given steps and properties.
    /// </summary>
    IFlow GetFlow(IEnumerable<IFlowStep> steps, Dictionary<string, object> properties, bool nonInteractive, bool showSelectedOptions = true);
}
