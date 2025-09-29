// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the model for Minimal API scaffolding, including endpoint and context information.
/// </summary>
internal class MinimalApiModel
{
    /// <summary>
    /// Gets or sets a value indicating whether OpenAPI is enabled.
    /// </summary>
    public bool OpenAPI { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether to use typed results for endpoints.
    /// </summary>
    public bool UseTypedResults { get; set; } = true;
    /// <summary>
    /// Gets or sets the name of the endpoints class.
    /// </summary>
    public string? EndpointsClassName { get; set; }
    /// <summary>
    /// Gets or sets the file name for the endpoints class.
    /// </summary>
    public string EndpointsFileName { get; set; } = default!;
    /// <summary>
    /// Gets or sets the path for the endpoints class file.
    /// </summary>
    public string? EndpointsPath { get; set; }
    /// <summary>
    /// Gets or sets the namespace for the endpoints class.
    /// </summary>
    public string? EndpointsNamespace { get; set; }
    /// <summary>
    /// Gets or sets the method name for the endpoints.
    /// </summary>
    public string? EndpointsMethodName { get; set; }
    /// <summary>
    /// Gets the database context information.
    /// </summary>
    public required DbContextInfo DbContextInfo { get; init; }
    /// <summary>
    /// Gets the model information for the entity being scaffolded.
    /// </summary>
    public required ModelInfo ModelInfo { get; init; }
    /// <summary>
    /// Gets the project information.
    /// </summary>
    public required ProjectInfo ProjectInfo { get; init; }
}
