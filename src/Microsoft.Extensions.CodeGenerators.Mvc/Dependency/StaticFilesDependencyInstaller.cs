// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.CodeGeneration;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Dependency
{
    public class StaticFilesDependencyInstaller : DependencyInstaller
    {
        public StaticFilesDependencyInstaller(
            ILibraryManager libraryManager,
            IApplicationEnvironment applicationEnvironment,
            ILogger logger,
            IPackageInstaller packageInstaller,
            IServiceProvider serviceProvider)
            : base(libraryManager, applicationEnvironment, logger, packageInstaller, serviceProvider)
        {
        }

        protected override async Task GenerateCode()
        {
            await CopyFolderContentsRecursive(GetWebRoot(), TemplateFolders.First());
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

        private string GetWebRoot()
        {
            var projectFile = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "project.json");
            Contract.Assert(File.Exists(projectFile));

            var jsonContent = JObject.Parse(File.ReadAllText(projectFile));
            var webRootToken = jsonContent["webroot"];

            var webRootRelativePath = webRootToken != null ? webRootToken.Value<string>() : string.Empty;
            return Path.Combine(ApplicationEnvironment.ApplicationBasePath, webRootRelativePath);
        }
    }
}