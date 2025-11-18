// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the base model for CRUD scaffolding, containing context and model information.
/// </summary>
internal class CrudModel
{
    /// <summary>
    /// Gets the type of the page being scaffolded (e.g., "Create", "Edit").
    /// </summary>
    public required string PageType { get; init; }
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
