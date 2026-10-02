// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Roslyn;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Healthcare Tracker scaffolding with Syncfusion Blazor Toolkit.
/// </summary>
internal static class HealthcareTrackerHelper
{
    internal const string HealthcareTrackerTemplate = "HealthcareTracker.tt";

    /// <summary>
    /// Gets the template type for a given template path and target framework.
    /// </summary>
    internal static Type? GetTemplateType(string? templatePath, TargetFramework? targetFramework)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(templatePath);
        if (!string.Equals(fileName, HealthcareTrackerTemplate, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return targetFramework switch
        {
            TargetFramework.Net8 => typeof(Templates.net8.HealthcareTracker.HealthcareTracker),
            TargetFramework.Net9 => typeof(Templates.net9.HealthcareTracker.HealthcareTracker),
            TargetFramework.Net10 => typeof(Templates.net10.HealthcareTracker.HealthcareTracker),
            TargetFramework.Net11 => typeof(Templates.net11.HealthcareTracker.HealthcareTracker),
            _ => typeof(Templates.net11.HealthcareTracker.HealthcareTracker),
        };
    }

    /// <summary>
    /// Builds text-templating properties for the Healthcare Tracker page template.
    /// </summary>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(
        IEnumerable<string> allT4TemplatePaths,
        HealthcareTrackerModel healthcareTrackerModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        if (healthcareTrackerModel.ProjectInfo is null ||
            string.IsNullOrEmpty(healthcareTrackerModel.ProjectInfo.ProjectPath))
        {
            return textTemplatingProperties;
        }

        string projectBasePath = Path.GetDirectoryName(healthcareTrackerModel.ProjectInfo.ProjectPath)
            ?? Directory.GetCurrentDirectory();

        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateType = GetTemplateType(
                templatePath,
                healthcareTrackerModel.ProjectInfo.LowestSupportedTargetFramework);

            if (string.IsNullOrEmpty(templatePath) || templateType is null)
            {
                continue;
            }

            var outputFileName = Path.Combine(
                projectBasePath,
                "Components",
                "Pages",
                $"HealthcareTracker{Common.Constants.BlazorExtension}");

            textTemplatingProperties.Add(new()
            {
                TemplateModel = healthcareTrackerModel,
                TemplateModelName = "Model",
                TemplatePath = templatePath,
                TemplateType = templateType,
                OutputPath = outputFileName
            });
        }

        return textTemplatingProperties;
    }
}
