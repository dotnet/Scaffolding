﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        internal static void ValidateEFDependencies(IEnumerable<DependencyDescription> dependencies)
        {
            var isEFDesignPackagePresent = dependencies
                .Any(package => package.Name.Equals(EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            var isSqlServerPackagePresent = dependencies
                .Any(package => package.Name.Equals(SqlServerPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent || !isSqlServerPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfDesignPackageName}, {SqlServerPackageName}"));
            }
        }

        internal static void ValidateSQLiteDependency(IEnumerable<DependencyDescription> dependencies)
        { 
            var isSqlitePackagePresent = dependencies
                .Any(package => package.Name.Equals(SqlitePackageName, StringComparison.OrdinalIgnoreCase));
            
            if (!isSqlitePackagePresent) 
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallSqlitePackage, $"{SqlitePackageName}"));
            }
        }
    }
}