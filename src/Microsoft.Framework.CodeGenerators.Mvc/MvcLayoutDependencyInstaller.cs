// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider)
            : base(libraryManager, applicationEnvironment, logger, typeActivator, serviceProvider)
        {
        }

        public override string TemplateFoldersName
        {
            get
            {
                return "MvcLayout";
            }
        }

        public override void Execute()
        {
            var destinationPath = Path.Combine(ApplicationEnvironment.ApplicationBasePath, Constants.ViewsFolderName,
                Constants.SharedViewsFolderName);

            CopyFolderContentsRecursive(destinationPath, TemplateFolders.First());

            var staticFilesInstaller = TypeActivator.CreateInstance<StaticFilesDependencyInstaller>(ServiceProvider);
            staticFilesInstaller.Execute();
        }

        public override IEnumerable<Dependency> Dependencies
        {
            get
            {
                return new List<Dependency>()
                {
                    StandardDependencies.MvcDependency,
                    StandardDependencies.StaticFilesDependency
                };
            }
        }
    }
}