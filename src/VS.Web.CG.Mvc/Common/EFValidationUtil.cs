// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal static class EFValidationUtil
    {
        internal static void ValidateEFDependencies(IEnumerable<DependencyDescription> dependencies)
        {
            const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
            var isEFDesignPackagePresent = dependencies
                .Any(package => package.Name.Equals(EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
            var isSqlServerPackagePresent = dependencies
                .Any(package => package.Name.Equals(SqlServerPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent || !isSqlServerPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfDesignPackageName}, {SqlServerPackageName}"));
            }
        }
    }
}