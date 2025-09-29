// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Base settings for scaffolding steps, including the project path.
/// </summary>
internal class BaseSettings
{
    /// <summary>
    /// The path to the project file.
    /// </summary>
    public required string Project { get; init; }
}
