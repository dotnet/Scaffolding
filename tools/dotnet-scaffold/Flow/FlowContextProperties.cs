// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Flow;

internal static class FlowContextProperties
{
    public const string CommandSettings = nameof(CommandSettings);
    public const string CommandInfos = nameof(CommandInfos);
    public const string Controller = nameof(Controller);
    public const string ExcludedProjects = nameof(ExcludedProjects);
    public const string OriginalTraits = nameof(OriginalTraits);
    public const string ProjectTemplateName = nameof(ProjectTemplateName);
    public const string Slice = nameof(Slice);
    public const string SourceProjectPath = nameof(SourceProjectPath);
    public const string SourceProjectDisplay = "Source project";
    public const string CommandArgs = nameof(CommandArgs);
    public const string ComponentName = nameof(ComponentName);
    public const string CommandName = nameof(CommandName);
    public const string ComponentNameDisplay = "Component";
    //of type Microsoft.Build.Evaluation.Project
    public const string SourceProject = nameof(SourceProject);
    public const string TargetFrameworkName = nameof(TargetFrameworkName);
    public const string RemainingArgs = nameof(RemainingArgs);
}
