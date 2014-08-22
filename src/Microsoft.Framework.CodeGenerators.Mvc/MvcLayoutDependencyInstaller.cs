// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class MvcLayoutDependencyInstaller : DependencyInstaller
    {
        public MvcLayoutDependencyInstaller([NotNull]ILibraryManager libraryManager)
            :base(libraryManager)
        {
        }

        public override string TemplateFoldersName
        {
            get
            {
                return "MvcLayout";
            }
        }

        public override void Install(IApplicationEnvironment application)
        {
            var destinationPath = Path.Combine(application.ApplicationBasePath, Constants.ViewsFolderName,
                Constants.SharedViewsFolderName);

            CopyFolderContentsRecursive(destinationPath, TemplateFolders.First());

            StaticFilesDependencyInstaller staticFilesInstaller = new StaticFilesDependencyInstaller(LibraryManager);
            staticFilesInstaller.Install(application);
        }

        public override IEnumerable<Dependency> Dependencies
        {
            get
            {
                return new List<Dependency>()
                {
                    StandardDependencies.MvcDependency, //Todo: This is not required here??
                    StandardDependencies.StaticFilesDependency
                };
            }
        }
    }
}