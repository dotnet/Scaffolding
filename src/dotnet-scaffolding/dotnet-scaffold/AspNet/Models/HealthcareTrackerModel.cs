// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Model for Healthcare Tracker scaffolding with Syncfusion Blazor Toolkit.
/// </summary>
internal class HealthcareTrackerModel
{
    /// <summary>
    /// Project metadata used for template output and code modification.
    /// </summary>
    public required ProjectInfo ProjectInfo { get; init; }

    /// <summary>
    /// Indicates whether the host project contains a MainLayout component.
    /// Used to emit the @layout directive for the generated page.
    /// </summary>
    public bool HasMainLayout { get; set; }
}
