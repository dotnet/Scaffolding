// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Flow;
using System.Collections.Generic;

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
    /// 
    /// </summary>
    /// <param name="steps"></param>
    /// <param name="properties"></param>
    /// <param name="nonInteractive"></param>
    /// <param name="showSelectedOptions"></param>
    /// <returns></returns>
    IFlow GetFlow(IEnumerable<IFlowStep> steps, Dictionary<string, object> properties, bool nonInteractive, bool showSelectedOptions = true);
}
