// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

/// <summary>
/// Extension methods for <see cref="ScaffolderContext"/> to simplify retrieval of common context values.
/// </summary>
public static class ScaffolderContextExtensions
{
    /// <summary>
    /// Gets the specified target framework from the scaffolder context.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <returns>The target framework name if present; otherwise, null.</returns>
    public static string? GetSpecifiedTargetFramework(this ScaffolderContext context)
    {
        string? targetFramework = null;
        if (context.Properties.TryGetValue(TargetFrameworkConstants.TargetFrameworkPropertyName, out object? tfm))
        {
            targetFramework = tfm as string;
        }
        return targetFramework;
    }

    /// <summary>
    /// Sets the specified target framework in the scaffolder context.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="targetFramework">The target framework to set.</param>
    public static string? SetSpecifiedTargetFramework(this ScaffolderContext context, string? targetFramework)
    {
        context.Properties[TargetFrameworkConstants.TargetFrameworkPropertyName] = targetFramework;
        return targetFramework;
    }
}
