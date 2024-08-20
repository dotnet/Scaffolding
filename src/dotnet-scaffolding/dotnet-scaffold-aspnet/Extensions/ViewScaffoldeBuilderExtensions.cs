// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class ViewScaffoldeBuilderExtensions
{
    public static IScaffoldBuilder WithViewsTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(ViewModel), out var viewModelObj);
            ViewModel viewModel = viewModelObj as ViewModel ??
                throw new InvalidOperationException("missing 'ViewModel' in 'ScaffolderContext.Properties'");

            var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["Views"]);
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
