// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeneratorActionService;
        private IProjectContext _projectContext;
        private IConnectionStringsWriter _connectionStringsWriter;
        private Workspace _workspace;
        private ICodeGenAssemblyLoadContext _loader;
        private IFileSystem _fileSystem;

        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[]
                     { 
                         Path.Combine("Identity", "Controllers"),
                         Path.Combine("Identity", "Data"),
                         Path.Combine("Identity", "Extensions"),
                         Path.Combine("Identity", "Services"),
                         Path.Combine("Identity", "Pages"),
                         "Identity"
                     },
                    _projectContext);
            }
        }

        private string _templateFolderRoot;
        private string TemplateFolderRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_templateFolderRoot))
                {
                    _templateFolderRoot = TemplateFoldersUtilities.GetTemplateFolders(
                        Constants.ThisAssemblyName,
                        _applicationInfo.ApplicationBasePath,
                        new [] { "Identity" },
                        _projectContext
                    ).First();
                }
                
                return _templateFolderRoot;
            }
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
                ShowFileList();
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

        private void ShowFileList()
        {
            _logger.LogMessage("File List:");
            var files = IdentityGeneratorFilesConfig.GetFilesToList();
            _logger.LogMessage(string.Join(Environment.NewLine, files));
        }

        private async Task AddStaticFiles(IdentityGeneratorTemplateModel templateModel)
        {

            var projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            foreach (var staticFile  in templateModel.FilesToGenerate.Where(f => !f.IsTemplate))
            {
                var outputPath = Path.Combine(projectDir, staticFile.OutputPath);
                if (staticFile.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(staticFile, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding static file: {staticFile.Name}", LogMessageLevel.Trace);

                    await _codegeneratorActionService.AddFileAsync(
                        outputPath,
                        Path.Combine(TemplateFolderRoot, staticFile.SourcePath)
                    );
                }
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorTemplateModel templateModel)
        {
            var projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            var templates = templateModel.FilesToGenerate.Where(t => t.IsTemplate);

            foreach (var template in templates)
            {
                var outputPath = Path.Combine(projectDir, template.OutputPath);
                if (template.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(template, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding template: {template.Name}", LogMessageLevel.Trace);
                    await _codegeneratorActionService.AddFileFromTemplateAsync(
                        outputPath,
                        template.SourcePath,
                        TemplateFolders,
                        templateModel);
                }
            }

            if (!templateModel.IsUsingExistingDbContext)
            {
                _connectionStringsWriter.AddConnectionString(
                    connectionStringName: $"{templateModel.DbContextClass}Connection",
                    dataBaseName: templateModel.ApplicationName,
                    useSQLite: templateModel.UseSQLite);
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
