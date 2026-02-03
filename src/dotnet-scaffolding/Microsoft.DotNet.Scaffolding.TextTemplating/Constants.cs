// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Contains constant values used throughout the text templating infrastructure.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The default name for a new DbContext.
    /// </summary>
    public const string NewDbContext = nameof(NewDbContext);

    /// <summary>
    /// The C# file extension.
    /// </summary>
    public const string CSharpExtension = ".cs";

    /// <summary>
    /// Represents the command-line option used to specify a project path or name.
    /// </summary>
    /// <remarks>This constant can be used when constructing or parsing command-line arguments that require a
    /// project to be specified, ensuring consistency across tools and scripts.</remarks>
    public const string ProjectCliOption = "--project";
}
