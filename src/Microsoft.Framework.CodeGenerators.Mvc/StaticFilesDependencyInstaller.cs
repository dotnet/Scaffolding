// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class StaticFilesDependencyInstaller : DependencyInstaller
    {
        public StaticFilesDependencyInstaller(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment applicationEnvironment,
            [NotNull]ILogger logger,
            [NotNull]IPackageInstaller packageInstaller,
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider)
            : base(libraryManager, applicationEnvironment, logger, packageInstaller, typeActivator, serviceProvider)
        {
        }

        protected override async Task GenerateCode()
        {
            await CopyFolderContentsRecursive(ApplicationEnvironment.ApplicationBasePath, TemplateFolders.First());
        }

        protected override IEnumerable<PackageMetadata> Dependencies
        {
            get
            {
                return new List<PackageMetadata>()
                {
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
                    StandardDependencies.StaticFilesStartupContent
                };
            }
        }

        protected override string TemplateFoldersName
        {
            get
            {
                return "StaticFiles";
            }
        }
    }
}