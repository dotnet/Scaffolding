// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the model for scaffolding an EF controller, including controller and context information.
/// </summary>
internal class EfControllerModel
{
    /// <summary>
    /// Gets or sets the type of the controller (e.g., "ApiController").
    /// </summary>
    public required string ControllerType { get; set; }
    /// <summary>
    /// Gets or sets the name of the controller.
    /// </summary>
    public required string ControllerName { get; set; }
    /// <summary>
    /// Gets or sets the output path for the generated controller file.
    /// </summary>
    public required string ControllerOutputPath { get; set; }
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
