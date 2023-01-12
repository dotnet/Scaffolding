// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                .Any(package => package.Name.Equals(EfConstants.EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfConstants.EfDesignPackageName}"));
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
