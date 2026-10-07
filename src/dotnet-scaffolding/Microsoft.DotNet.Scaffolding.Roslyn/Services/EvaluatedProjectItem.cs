// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

/// <summary>
/// An MSBuild item as evaluated for a project.
/// </summary>
/// <param name="ItemType">The item type, e.g. 'PackageReference'.</param>
/// <param name="EvaluatedInclude">The evaluated Include value, e.g. a package ID or a project-relative file path.</param>
/// <param name="DefiningProjectFullPath">
/// The full path of the file that declares the item: the project itself, or an imported file such as Directory.Build.props.
/// </param>
public sealed record EvaluatedProjectItem(string ItemType, string EvaluatedInclude, string DefiningProjectFullPath);
