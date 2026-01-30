// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Razor view scaffolding steps.
/// </summary>
internal static class ViewScaffoldeBuilderExtensions
{
    /// <summary>
    /// Adds a step to the scaffold builder that configures text templating for Razor view files.
    /// </summary>
    /// <param name="builder">The scaffold builder instance.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithViewsTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(ViewModel), out var viewModelObj);
            ViewModel viewModel = viewModelObj as ViewModel ??
                throw new InvalidOperationException("missing 'ViewModel' in 'ScaffolderContext.Properties'");

            var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["net10.0\\Views"]);
            var viewTemplateProperties = ViewHelper.GetTextTemplatingProperties(allT4TemplatePaths, viewModel);
            if (viewTemplateProperties.Any())
            {
                step.TextTemplatingProperties = viewTemplateProperties;
                step.DisplayName = "Razor view files (.cshtml)";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step to the scaffold builder that configures the addition of the "_ValidationScriptsPartial.cshtml" file.
    /// </summary>
    /// <param name="builder">The scaffold builder instance.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithViewsAddFileStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddFileStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(CrudSettings), out var viewSettingsObj);
            var viewSettings = viewSettingsObj as CrudSettings ??
                throw new InvalidOperationException("missing 'ViewModel' in 'ScaffolderContext.Properties'");

            var projectDirectory = Path.GetDirectoryName(viewSettings.Project);
            if (Directory.Exists(projectDirectory))
            {
                var sharedViewsDirectory = Path.Combine(projectDirectory, "Views", "Shared");
                step.BaseOutputDirectory = sharedViewsDirectory;
                step.FileName = "_ValidationScriptsPartial.cshtml";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }
}
