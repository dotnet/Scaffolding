// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for EF-based scaffolding steps that require a model and optional database context.
/// </summary>
internal class EfWithModelStepSettings : BaseSettings
{
    /// <summary>
    /// The database provider to use (e.g., SqlServer, Sqlite).
    /// </summary>
    public string? DatabaseProvider { get; set; }
    /// <summary>
    /// The name of the DbContext class to use.
    /// </summary>
    public string? DataContext { get; set; }
    /// <summary>
    /// The name of the model class to scaffold.
    /// </summary>
    public required string Model { get; set; }
    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; init; }
}
