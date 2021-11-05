// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal static class EFValidationUtil
    {
        const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
        const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";

        internal static void ValidateEFDependencies(IEnumerable<DependencyDescription> dependencies, bool useSqlite)
        {
            var isEFDesignPackagePresent = dependencies
                .Any(package => package.Name.Equals(EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfDesignPackageName}"));
            }
            if (useSqlite)
            {
                ValidateSqliteDependency(dependencies);
            }
            else
            {
                ValidateSqlServerDependency(dependencies);
            }
            
        }

        internal static void ValidateSqlServerDependency(IEnumerable<DependencyDescription> dependencies)
        { 
            var isSqlServerPackagePresent = dependencies
                .Any(package => package.Name.Equals(SqlServerPackageName, StringComparison.OrdinalIgnoreCase));
            
            if (!isSqlServerPackagePresent) 
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallSqlPackage, $"{SqlServerPackageName}."));
            }
        }

        internal static void ValidateSqliteDependency(IEnumerable<DependencyDescription> dependencies)
        {
            var isSqlServerPackagePresent = dependencies
                .Any(package => package.Name.Equals(SqlitePackageName, StringComparison.OrdinalIgnoreCase));

            if (!isSqlServerPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallSqlPackage, $"{SqlitePackageName}."));
            }
        }
    }
}
