// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.General;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps.AddPackageStep;

internal class AddPackagesStep(AddPackageStepInfo packageStepInfo) : ScaffoldStep<AddPackageStepInfo>(packageStepInfo)
{
    public override Task<bool> ExecuteAsync()
    {
        foreach (var packageName in StepInfo.PackageNames)
        {
            if (!string.IsNullOrEmpty(packageName))
            {
                DotnetCommands.AddPackage(
                    packageName: packageName,
                    logger: StepInfo.Logger,
                    projectFile: StepInfo.ProjectPath,
                    includePrerelease: StepInfo.Prerelease);
            }
        }

        return Task.FromResult(true);
    }
}
