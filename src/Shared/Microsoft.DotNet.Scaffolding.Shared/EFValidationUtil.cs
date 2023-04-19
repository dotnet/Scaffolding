// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    internal static class EFValidationUtil
    {
        internal static void ValidateEFDependencies(IEnumerable<DependencyDescription> dependencies, DbProvider dataContextType)
        {
            var isEFDesignPackagePresent = dependencies
                .Any(package => package.Name.Equals(EfConstants.EfToolsPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfConstants.EfToolsPackageName}"));
            }

            if (EfConstants.EfPackagesDict.TryGetValue(dataContextType, out var dbProviderPackageName))
            {
                ValidateDependency(dbProviderPackageName, dependencies);
            }
        }

        internal static void ValidateDependency(string packageName, IEnumerable<DependencyDescription> dependencies)
        {
            var isPackagePresent = dependencies
                .Any(package => package.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (!isPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallSqlPackage, $"{packageName}."));
            }
        }
    }
}
