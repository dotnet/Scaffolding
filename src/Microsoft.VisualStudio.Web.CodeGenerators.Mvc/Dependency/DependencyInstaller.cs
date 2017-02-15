// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency
{
    public abstract class DependencyInstaller
    {
        protected DependencyInstaller(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ILogger logger,
            IPackageInstaller packageInstaller,
            IServiceProvider serviceProvider)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (packageInstaller == null)
            {
                throw new ArgumentNullException(nameof(packageInstaller));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            ProjectContext = projectContext;
            ApplicationEnvironment = applicationInfo;
            Logger = logger;
            PackageInstaller = packageInstaller;
            ServiceProvider = serviceProvider;
        }

        public async Task Execute()
        {
            if (MissingDepdencies.Any())
            {
                await GenerateCode();
            }
        }

        public async Task InstallDependencies()
        {
            if (MissingDepdencies.Any())
            {
                var isReadMe = true;
                var readMeGenerator = ActivatorUtilities.CreateInstance<ReadMeGenerator>(ServiceProvider);
                if (IsMsBuildProject)
                {
                    readMeGenerator.GenerateReadMeWithContent(GetMsBuildMissingDependencyReadMeText(MissingDepdencies));
                }
                else
                {
                    await PackageInstaller.InstallPackages(MissingDepdencies);
                    isReadMe = await readMeGenerator.GenerateStartupOrReadme(StartupContents.ToList());
                }

                if (isReadMe)
                {
                    Logger.LogMessage("There are probably still some manual steps required");
                    Logger.LogMessage("Checkout the " + Constants.ReadMeOutputFileName + " file that got generated");
                }
            }
        }

        private string GetMsBuildMissingDependencyReadMeText(IEnumerable<PackageMetadata> missingDepdencies)
        {
            var contentBuilder = new StringBuilder("Please install the below packages to your project:");
            foreach (var dependency in missingDepdencies)
            {
                contentBuilder.Append($"{Environment.NewLine}    {dependency.Name} :: {dependency.Version}");
            }

            return contentBuilder.ToString();
        }

        protected abstract Task GenerateCode();

        protected IApplicationInfo ApplicationEnvironment { get; private set; }
        protected ILogger Logger { get; private set; }
        public IPackageInstaller PackageInstaller { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }
        protected IProjectContext ProjectContext { get; private set; }
        protected bool IsMsBuildProject => Path.GetExtension(ProjectContext.ProjectFullPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase);

        protected IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    baseFolders: new[] { TemplateFoldersName },
                    applicationBasePath: ApplicationEnvironment.ApplicationBasePath,
                    projectContext: ProjectContext);
            }
        }

        protected virtual IEnumerable<PackageMetadata> Dependencies
        {
            get
            {
                return Enumerable.Empty<PackageMetadata>();
            }
        }

        protected virtual IEnumerable<StartupContent> StartupContents
        {
            get
            {
                return Enumerable.Empty<StartupContent>();
            }
        }

        protected abstract string TemplateFoldersName { get; }

        protected IEnumerable<PackageMetadata> MissingDepdencies
        {
            get
            {
                return Dependencies
                    .Where(dep => ProjectContext.GetPackage(dep.Name) == null);
            }
        }

        // Copies files from given source directory to destination directory recursively
        // Ignores any existing files
        protected async Task CopyFolderContentsRecursive(string destinationPath, string sourcePath)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
            Contract.Assert(sourceDir.Exists);

            // Create the destination directory if it does not exist.
            Directory.CreateDirectory(destinationPath);

            // Copy the files only if they don't exist in the destination.
            foreach (var fileInfo in sourceDir.GetFiles())
            {
                var destinationFilePath = Path.Combine(destinationPath, fileInfo.Name);
                if (!File.Exists(destinationFilePath))
                {
                    fileInfo.CopyTo(destinationFilePath);
                }
            }

            // Copy sub folder contents
            foreach (var subDirInfo in sourceDir.GetDirectories())
            {
                await CopyFolderContentsRecursive(Path.Combine(destinationPath, subDirInfo.Name), subDirInfo.FullName);
            }
        }
    }
}