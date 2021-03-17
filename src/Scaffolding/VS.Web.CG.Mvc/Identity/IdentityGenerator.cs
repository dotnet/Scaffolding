// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    [Alias("identity")]
    public class IdentityGenerator : ICodeGenerator
    {
        private const string IdentityAreaName = "Identity";

        internal static readonly string DefaultBootstrapVersion = "4";
        // A hashset would allow faster lookups, but would cause a perf hit when formatting the error string for invalid bootstrap version.
        // Also, with a list this small, the lookup perf hit will be largely irrelevant.
        internal static readonly IReadOnlyList<string> ValidBootstrapVersions = new List<string>()
        {
            "3",
            "4"
        };

        internal static readonly string ContentVersionDefault = "Default";
        internal static readonly string ContentVersionBootstrap3 = "Bootstrap3";

        internal static readonly string DefaultContentRelativeBaseDir = "Identity";
        internal static readonly string VersionedContentRelativeBaseDir = "Identity_Versioned";

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeneratorActionService;
        private IProjectContext _projectContext;
        private IConnectionStringsWriter _connectionStringsWriter;
        private Workspace _workspace;
        private ICodeGenAssemblyLoadContext _loader;
        private IFileSystem _fileSystem;

        // The default-version content files will go in the default location: "Identity\" - (DefaultContentRelativeBaseDir)
        //      other content versions will go in "Identity_Versioned\[VersionIndicator]\" - (VersionedContentRelativeBaseDir)
        // Doing it this way is to maintain back-compat from before multiple content versions were supported.
        // The default content goes in the place where the only content used to be.
        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[]
                     {
                         Path.Combine(DefaultContentRelativeBaseDir, "Controllers"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Data"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Extensions"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Services"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Pages"),
                         DefaultContentRelativeBaseDir
                     },
                    _projectContext);
            }
        }

        // Returns the set of template folders appropriate for templateModel.ContentVersion
        private IEnumerable<string> GetTemplateFoldersForContentVersion(IdentityGeneratorTemplateModel templateModel)
        {
            if (!(templateModel is IdentityGeneratorTemplateModel2 templateModel2))
            {   // for back-compat
                return TemplateFolders;
            }

            // The default content is packaged under the default location "Identity\*" (no subfolder).
            if (string.Equals(templateModel2.ContentVersion, ContentVersionDefault, StringComparison.Ordinal))
            {
                return TemplateFolders;
            }

            // For non-default bootstrap versions, the content is packaged under "Identity_Versioned\[Version_Identifier]\*"
            // Note: In the future, if content gets pivoted on things other than bootstrap, this logic will need enhancement.
            if (string.Equals(templateModel2.ContentVersion, ContentVersionBootstrap3, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[] {
                        Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{templateModel2.BootstrapVersion}")
                    },
                    _projectContext);
            }

            // This should get caught by IdentityGeneratorTemplateModelBuilder.ValidateCommandLine() and emit the same error. 
            // But best to be safe here.
            // Note: If we start pivoting content on things other than bootstrap version, this error message will need to be reworked.
            throw new InvalidOperationException(string.Format(MessageStrings.InvalidBootstrapVersionForScaffolding, templateModel2.BootstrapVersion, string.Join(", ", ValidBootstrapVersions)));
        }

        // Returns the root directory of the template folders appropriate for templateModel.ContentVersion
        private string GetTemplateFolderRootForContentVersion(IdentityGeneratorTemplateModel templateModel)
        {
            string relativePath = null;

            if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
            {
                if (string.Equals(templateModel2.ContentVersion, ContentVersionDefault, StringComparison.Ordinal))
                {
                    relativePath = DefaultContentRelativeBaseDir;
                }
                else if (string.Equals(templateModel2.ContentVersion, ContentVersionBootstrap3, StringComparison.Ordinal))
                {
                    // Note: In the future, if content gets pivoted on things other than bootstrap, this logic will need enhancement.
                    relativePath = Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{templateModel2.BootstrapVersion}");
                }

                if (string.IsNullOrEmpty(relativePath))
                {
                    // This should get caught by IdentityGeneratorTemplateModelBuilder.ValidateCommandLine() and emit the same error. 
                    // But best to be safe here.
                    // Note: If we start pivoting content on things other than bootstrap version, this error message will need to be reworked.
                    throw new InvalidOperationException(string.Format(MessageStrings.InvalidBootstrapVersionForScaffolding, templateModel2.BootstrapVersion, string.Join(", ", ValidBootstrapVersions)));
                }
            }
            else
            {
                relativePath = DefaultContentRelativeBaseDir;
            }

            return TemplateFoldersUtilities.GetTemplateFolders(
                Constants.ThisAssemblyName,
                _applicationInfo.ApplicationBasePath,
                new[] {
                    relativePath
                },
                _projectContext
            ).First();
        }

        public IdentityGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            ICodeGeneratorActionsService actionService,
            IProjectContext projectContext,
            IConnectionStringsWriter connectionStringsWriter,
            Workspace workspace,
            ICodeGenAssemblyLoadContext loader,
            IFileSystem fileSystem,
            ILogger logger)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (actionService == null)
            {
                throw new ArgumentNullException(nameof(actionService));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (connectionStringsWriter == null)
            {
                throw new ArgumentNullException(nameof(connectionStringsWriter));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _applicationInfo = applicationInfo;
            _serviceProvider = serviceProvider;
            _codegeneratorActionService = actionService;
            _projectContext = projectContext;
            _connectionStringsWriter = connectionStringsWriter;
            _workspace = workspace;
            _loader = loader;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task GenerateCode(IdentityGeneratorCommandLineModel commandlineModel)
        {
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            if (commandlineModel.ListFiles)
            {
                ShowFileList(commandlineModel.BootstrapVersion);
                return;
            }

            var templateModelBuilder = new IdentityGeneratorTemplateModelBuilder(
                commandlineModel,
                _applicationInfo,
                _projectContext,
                _workspace,
                _loader,
                _fileSystem,
                _logger);

            var templateModel = await templateModelBuilder.ValidateAndBuild();
            EnsureFolderLayout(IdentityAreaName, templateModel);

            await AddTemplateFiles(templateModel);
            await AddStaticFiles(templateModel);
        }

        private void ShowFileList(string commandBootstrapVersion)
        {
            string contentVersion = string.Equals(commandBootstrapVersion, "3", StringComparison.Ordinal)
                ? ContentVersionBootstrap3
                : ContentVersionDefault;

            _logger.LogMessage("File List:");

            IEnumerable<string> files = IdentityGeneratorFilesConfig.GetFilesToList(contentVersion);

            _logger.LogMessage(string.Join(Environment.NewLine, files));

            if (_fileSystem is SimulationModeFileSystem simModefileSystem)
            {
                foreach (string fileName in files)
                {
                    simModefileSystem.AddMetadataMessage(fileName);
                }
            }
        }

        private async Task AddStaticFiles(IdentityGeneratorTemplateModel templateModel)
        {
            string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            string templateFolderRoot = GetTemplateFolderRootForContentVersion(templateModel);

            foreach (IdentityGeneratorFile staticFile in templateModel.FilesToGenerate.Where(f => !f.IsTemplate))
            {
                string outputPath = Path.Combine(projectDir, staticFile.OutputPath);
                if (staticFile.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(staticFile, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding static file: {staticFile.Name}", LogMessageLevel.Trace);

                    await _codegeneratorActionService.AddFileAsync(
                        outputPath,
                        Path.Combine(templateFolderRoot, staticFile.SourcePath)
                    );
                }
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorTemplateModel templateModel)
        {
            string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            IEnumerable<IdentityGeneratorFile> templates = templateModel.FilesToGenerate.Where(t => t.IsTemplate);
            IEnumerable<string> templateFolders = GetTemplateFoldersForContentVersion(templateModel);

            foreach (IdentityGeneratorFile template in templates)
            {
                string outputPath = Path.Combine(projectDir, template.OutputPath);
                if (template.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(template, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding template: {template.Name}", LogMessageLevel.Trace);

                    await _codegeneratorActionService.AddFileFromTemplateAsync(
                        outputPath,
                        template.SourcePath,
                        templateFolders,
                        templateModel);
                }
            }

            if (!templateModel.IsUsingExistingDbContext)
            {
                _connectionStringsWriter.AddConnectionString(
                    connectionStringName: $"{templateModel.DbContextClass}Connection",
                    dataBaseName: templateModel.ApplicationName,
                    useSqlite: templateModel.UseSQLite);
            }
        }

        // Returns true if the template file exists in it's output path, or in an alt path (if any are specified)
        private bool DoesFileExist(IdentityGeneratorFile template, string projectDir)
        {
            string outputPath = Path.Combine(projectDir, template.OutputPath);
            if (_fileSystem.FileExists(outputPath))
            {
                return true;
            }

            return template.AltPaths.Any(altPath => _fileSystem.FileExists(Path.Combine(projectDir, altPath)));
        }

        /// <summary>
        /// Creates a folder hierarchy:
        ///     ProjectDir
        ///        \ Areas
        ///            \ IdentityAreaName
        ///                \ Data
        ///                \ Pages
        ///                \ Services
        /// </summary>
        private void EnsureFolderLayout(string identityAreaName, IdentityGeneratorTemplateModel templateModel)
        {
            var areaBasePath = Path.Combine(_applicationInfo.ApplicationBasePath, "Areas");
            if (!_fileSystem.DirectoryExists(areaBasePath))
            {
                _fileSystem.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, identityAreaName);
            if (!_fileSystem.DirectoryExists(areaPath))
            {
                _fileSystem.CreateDirectory(areaPath);
            }

            var areaFolders = IdentityGeneratorFilesConfig.GetAreaFolders(
                !templateModel.IsUsingExistingDbContext);

            foreach (var areaFolder in areaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!_fileSystem.DirectoryExists(path))
                {
                    _logger.LogMessage($"Adding folder: {path}", LogMessageLevel.Trace);
                    _fileSystem.CreateDirectory(path);
                }
            }
        }
    }
}
