// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Represents a tool entry in the scaffold manifest.
namespace Microsoft.DotNet.Tools.Scaffold.Models;

internal class ScaffoldTool
{
    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    public required string Name { get; set; }
}
