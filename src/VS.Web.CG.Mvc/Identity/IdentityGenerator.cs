// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    [Alias("identity")]
    public class IdentityGenerator : ICodeGenerator
    {
        private const string IdentityAreaName = "Identity";

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeratorActionService;
        private IProjectContext _projectContext;

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

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _applicationInfo = applicationInfo;
            _serviceProvider = serviceProvider;
            _codegeratorActionService = actionService;
            _projectContext = projectContext;
            _logger = logger;
        }

        public async Task GenerateCode(IdentityGeneratorCommandLineModel commandlineModel)
        {
            // TODO: Generation logic.
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            EnsureFolderLayout(IdentityAreaName);

            await AddTemplateFiles(commandlineModel);
            await AddStaticFiles();
        }

        private async Task AddStaticFiles()
        {
            foreach (var staticFile  in IdentityGeneratorFilesConfig.StaticFiles)
            {
                await _codegeratorActionService.AddFileAsync(
                    staticFile.Value,
                    Path.Combine(TemplateFolderRoot, staticFile.Key)
                );
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorCommandLineModel commandLineModel)
        {
            var rootNamespace = string.IsNullOrEmpty(commandLineModel.RootNamespace) ? _projectContext.RootNamespace:
                    commandLineModel.RootNamespace;

            var model = new IdentityGeneratorTemplateModel()
            {
                Namespace = rootNamespace,
                DbContextNamespace = rootNamespace+".Areas.Identity.Data",
                ApplicationName = _applicationInfo.ApplicationName
            };

            foreach (var template in IdentityGeneratorFilesConfig.Templates)
            {
                await _codegeratorActionService.AddFileFromTemplateAsync(
                    template.Value,
                    template.Key,
                    TemplateFolders,
                    model
                );
            }
        }

        /// <summary>
        /// Creates a folder hierarchy:
        ///     ProjectDir
        ///        \ Areas
        ///            \ IdentityAreaName
        ///                \ Controllers
        ///                \ Data
        ///                \ Extensions
        ///                \ Pages
        ///                \ Services
        /// </summary>
        private void EnsureFolderLayout(string IdentityareaName)
        {
            var areaBasePath = Path.Combine(_applicationInfo.ApplicationBasePath, "Areas");
            if (!Directory.Exists(areaBasePath))
            {
                Directory.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, IdentityareaName);
            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            foreach (var areaFolder in IdentityGeneratorFilesConfig.AreaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
    }
}
