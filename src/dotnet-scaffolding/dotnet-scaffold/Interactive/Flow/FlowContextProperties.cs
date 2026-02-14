// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;

/// <summary>
/// Provides constant property names used for storing and retrieving values in the flow context during scaffolding operations.
/// These properties act as keys for context data shared between flow steps.
/// </summary>
internal static class FlowContextProperties
{
    /// <summary>Key for command settings object.</summary>
    public const string CommandSettings = nameof(CommandSettings);
    /// <summary>Key for list of command info objects.</summary>
    public const string CommandInfos = nameof(CommandInfos);
    /// <summary>Key for controller name or object.</summary>
    public const string Controller = nameof(Controller);
    /// <summary>Key for excluded projects list.</summary>
    public const string ExcludedProjects = nameof(ExcludedProjects);
    /// <summary>Key for original traits or metadata.</summary>
    public const string OriginalTraits = nameof(OriginalTraits);
    /// <summary>Key for project template name.</summary>
    public const string ProjectTemplateName = nameof(ProjectTemplateName);
    /// <summary>Key for slice or feature name.</summary>
    public const string Slice = nameof(Slice);
    /// <summary>Key for source project file path.</summary>
    public const string SourceProjectPath = nameof(SourceProjectPath);
    /// <summary>Display name for source project.</summary>
    public const string SourceProjectDisplay = "Source project";
    /// <summary>Key for command arguments dictionary.</summary>
    public const string CommandArgs = nameof(CommandArgs);
    /// <summary>Key for command argument values list.</summary>
    public const string CommandArgValues = nameof(CommandArgValues);
    /// <summary>Key for component name string.</summary>
    public const string ComponentName = nameof(ComponentName);
    /// <summary>Key for component object.</summary>
    public const string ComponentObj = nameof(ComponentObj);
    /// <summary>Key for command name string.</summary>
    public const string CommandName = nameof(CommandName);
    /// <summary>Key for command object.</summary>
    public const string CommandObj = nameof(CommandObj);
    /// <summary>Display name for component.</summary>
    public const string ComponentNameDisplay = "Component";
    //of type Microsoft.DotNet.Scaffolding.Helpers.Services.IProjectService
    public const string SourceProject = nameof(SourceProject);
    //of type Microsoft.DotNet.Scaffolding.Helpers.Services.ICodeService
    public const string CodeService = nameof(CodeService);
    /// <summary>Key for target framework name string.</summary>
    public const string TargetFrameworkName = nameof(TargetFrameworkName);
    /// <summary>Key for remaining arguments object.</summary>
    public const string RemainingArgs = nameof(RemainingArgs);
    /// <summary>Key for dotnet tool components list.</summary>
    public const string DotnetToolComponents = nameof(DotnetToolComponents);
    /// <summary>Key for scaffolding categories list.</summary>
    public const string ScaffoldingCategories = nameof(ScaffoldingCategories);
    /// <summary>Key for chosen scaffolding category string.</summary>
    public const string ChosenCategory = nameof(ChosenCategory);
    /// <summary>Key for telemetry environment variables dictionary.</summary>
    public const string TelemetryEnvironmentVariables = nameof(TelemetryEnvironmentVariables);

    public const string ProjectFileParameterResult = nameof(ProjectFileParameterResult);

    /// <summary>Key for boolean indicating if Aspire scaffolders should be available (false for .NET 8 projects).</summary>
    public const string IsAspireAvailable = nameof(IsAspireAvailable);
}
