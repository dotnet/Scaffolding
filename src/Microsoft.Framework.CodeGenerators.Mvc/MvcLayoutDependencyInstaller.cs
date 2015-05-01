// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class MvcLayoutDependencyInstaller : DependencyInstaller
    {
        public MvcLayoutDependencyInstaller(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment applicationEnvironment,
            [NotNull]ILogger logger,
            [NotNull]IPackageInstaller packageInstaller,
            [NotNull]IServiceProvider serviceProvider)
            : base(libraryManager, applicationEnvironment, logger, packageInstaller, serviceProvider)
        {
        }

        protected override string TemplateFoldersName
        {
            get
            {
                return "MvcLayout";
            }
        }

        protected override async Task GenerateCode()
        {
            var destinationPath = Path.Combine(ApplicationEnvironment.ApplicationBasePath, Constants.ViewsFolderName,
                Constants.SharedViewsFolderName);

            await CopyFolderContentsRecursive(destinationPath, TemplateFolders.First());

            var staticFilesInstaller = ActivatorUtilities.CreateInstance<StaticFilesDependencyInstaller>(ServiceProvider);
            await staticFilesInstaller.Execute();
        }

        protected override IEnumerable<PackageMetadata> Dependencies
        {
            get
            {
                return new List<PackageMetadata>()
                {
                    StandardDependencies.MvcDependency,
                    StandardDependencies.StaticFilesDependency
                };
            }
        }

        protected override IEnumerable<StartupContent> StartupContents
        {
            get
            {
                return new List<StartupContent>()
                {
                    StandardDependencies.MvcStartupContent,
                    StandardDependencies.StaticFilesStartupContent
                };
            }
        }
    }
}