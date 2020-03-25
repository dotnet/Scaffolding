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
        const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
        const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        const string SqliteCorePackageName = "Microsoft.EntityFrameworkCore.Sqlite.Core";

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
                ValidateSQLiteDependency(dependencies);
            } 
            else 
            {
                ValidateSqlServerDependency(dependencies);
            }
        }

        internal static void ValidateSQLiteDependency(IEnumerable<DependencyDescription> dependencies)
        { 
            var isSqliteCorePackagePresent = dependencies
                .Any(package => package.Name.Equals(SqliteCorePackageName, StringComparison.OrdinalIgnoreCase));
            
            if (!isSqliteCorePackagePresent) 
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallSqlPackage, $"{SqlitePackageName}."));
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
    }
}
