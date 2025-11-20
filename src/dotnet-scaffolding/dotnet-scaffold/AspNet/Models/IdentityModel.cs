// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the model for Identity scaffolding, containing user, context, and output information.
/// </summary>
internal class IdentityModel
{
    /// <summary>
    /// Gets the database context information.
    /// </summary>
    public required DbContextInfo DbContextInfo { get; init; }
    /// <summary>
    /// Gets the project information.
    /// </summary>
    public required ProjectInfo ProjectInfo { get; init; }
    /// <summary>
    /// Gets the namespace for Identity related code.
    /// </summary>
    public required string IdentityNamespace { get; init; }
    /// <summary>
    /// Gets or sets the namespace for Identity layout files.
    /// </summary>
    public string? IdentityLayoutNamespace { get; set; }
    /// <summary>
    /// Gets or sets the name of the user class.
    /// </summary>
    public required string UserClassName { get; internal set; }
    /// <summary>
    /// Gets or sets the namespace of the user class.
    /// </summary>
    public required string UserClassNamespace { get; internal set; }
    /// <summary>
    /// Gets or sets the namespace for the DbContext.
    /// </summary>
    public string? DbContextNamespace { get; set; }
    /// <summary>
    /// Gets or sets the base output path for generated files.
    /// </summary>
    public required string BaseOutputPath { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing files.
    /// </summary>
    public bool Overwrite { get; set; }
}
