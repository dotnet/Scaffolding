// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldBuilder
{
    /// <summary>
    /// Configures the name of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI
    /// </summary>
    IScaffoldBuilder WithDisplayName(string displayName);

    /// <summary>
    /// Configures the category in which the scaffolder will be organized in the dotnet-scaffold interactive UI
    /// </summary>
    IScaffoldBuilder WithCategory(string category);

    /// <summary>
    /// Configures the description of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI
    /// </summary>
    IScaffoldBuilder WithDescription(string description);

    /// <summary>
    /// Adds an option to be used with this scaffolder. This adds the configured option to both the command line and
    /// to the dotnet-scaffold interactive UI.
    /// </summary>
    IScaffoldBuilder WithOption(ScaffolderOption option);

    /// <summary>
    /// Adds mulitple options to be used with this scaffolder. This adds the configured options to both the command line and
    /// to the dotnet-scaffold interactive UI.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    IScaffoldBuilder WithOptions(IEnumerable<ScaffolderOption> options); 

    /// <summary>
    /// Adds a <see cref="ScaffoldStep"/> to the scaffolder to be run in the order it was added. Additionally allows for pre- and post-execution actions to be performed to configure the step and subsequent steps
    /// </summary>
    IScaffoldBuilder WithStep<TStep>(Action<ScaffoldStepConfigurator<TStep>>? preExecute = null, Action<ScaffoldStepConfigurator<TStep>>? postExecute = null) where TStep : ScaffoldStep;

    /// <summary>
    /// Builds the <see cref="IScaffolder"/> from the configured options and steps. This is generally called by the <see cref="IScaffoldRunner"/> when it is building the scaffolders and does not need to be called directly.
    /// </summary>
    IScaffolder Build(IServiceProvider serviceProvider);
}
