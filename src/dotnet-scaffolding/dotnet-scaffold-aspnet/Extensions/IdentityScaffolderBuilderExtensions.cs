// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class IdentityScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithIdentityAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<string> packageList = [
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackageName,
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityUiPackageName,
                PackageConstants.EfConstants.EfCoreToolsPackageName
            ];

            if (context.Properties.TryGetValue(nameof(IdentitySettings), out var commandSettingsObj) &&
                commandSettingsObj is IdentitySettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.IdentityEfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
                {
                    packageList.Add(projectPackageName);
                }

                step.PackageNames = packageList;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }
}
