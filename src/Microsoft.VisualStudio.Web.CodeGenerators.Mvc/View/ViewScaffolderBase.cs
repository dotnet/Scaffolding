// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public abstract class ViewScaffolderBase: CommonGeneratorBase
    {
        protected readonly IProjectContext _projectContext;
        protected readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;

        public ViewScaffolderBase(
            IProjectContext projectDependencyProvider,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(applicationInfo)
        {
            if (projectDependencyProvider == null)
            {
                throw new ArgumentNullException(nameof(projectDependencyProvider));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (codeGeneratorActionsService == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _projectContext = projectDependencyProvider;
            _codeGeneratorActionsService = codeGeneratorActionsService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public virtual IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] { "ViewGenerator" },
                    projectContext: _projectContext);
            }
        }

        protected bool IsViewWireUpNeeded { get; set; }

        protected async Task AddRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            IEnumerable<RequiredFileEntity> requiredFiles = GetRequiredFiles(viewGeneratorModel);
            foreach (var file in requiredFiles)
            {
                if (!File.Exists(Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath)))
                {
                    if (file.IsStaticFile)
                    {
                        await _codeGeneratorActionsService.AddFileAsync(
                            Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                            Path.Combine(TemplateFolders.First(), file.TemplateName));
                    }
                    else
                    {
                        await _codeGeneratorActionsService.AddFileFromTemplateAsync(
                            Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                            file.TemplateName,
                            TemplateFolders,
                            file.TemplateModel);
                    }
                    _logger.LogMessage($"Added additional file :{file.OutputPath}");
                }
            }
        }

        public abstract Task GenerateCode(ViewGeneratorModel viewGeneratorModel);

        internal async Task GenerateView(ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            if (viewGeneratorModel.ViewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = viewGeneratorModel.ViewName.Length - Constants.ViewExtension.Length;
                viewGeneratorModel.ViewName = viewGeneratorModel.ViewName.Substring(0, viewNameLength);
            }

            var templateModel = GetViewGeneratorTemplateModel(viewGeneratorModel, modelTypeAndContextModel);
            var templateName = viewGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added View : " + outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length));

            await AddRequiredFiles(viewGeneratorModel);
        }

        protected virtual IEnumerable<RequiredFileEntity> GetRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            List<RequiredFileEntity> requiredFiles = new List<RequiredFileEntity>();

            if (viewGeneratorModel.ReferenceScriptLibraries)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Views/Shared/_ValidationScriptsPartial.cshtml", @"_ValidationScriptsPartial.cshtml"));
            }

            if (IsViewWireUpNeeded)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Views/_ViewImports.cshtml", "_ViewImports.cshtml", false, new RequiredFilesTemplateModel() { RootNamespace = _projectContext.RootNamespace }));
                if (viewGeneratorModel.UseDefaultLayout)
                {
                    requiredFiles.Add(new RequiredFileEntity(@"Views/_ViewStart.cshtml", "_ViewStart.cshtml"));
                    requiredFiles.Add(new RequiredFileEntity(@"Views/Shared/_Layout.cshtml", "_Layout.cshtml", false, new RequiredFilesTemplateModel() { AppName = ApplicationInfo.ApplicationName }));
                }
            }

            return requiredFiles;
        }

        protected bool ViewsFolderExists(string relativeFolderPath, string applicationBasePath)
        {
            const string viewsFolderName = "Views";
            var currentDir = applicationBasePath;
            if (Directory.Exists(Path.Combine(currentDir, viewsFolderName)))
            {
                return true;
            }

            var pathParts = new string[0];
            if (!string.IsNullOrEmpty(relativeFolderPath))
            {
                pathParts = relativeFolderPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var part in pathParts)
            {
                currentDir = Path.Combine(currentDir, part);
                if (Directory.Exists(Path.Combine(currentDir, viewsFolderName)))
                {
                    return true;
                }
            }

            return false;
        }

        protected ViewGeneratorTemplateModel GetViewGeneratorTemplateModel(ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel)
        {
            bool isLayoutSelected = !viewGeneratorModel.PartialView &&
                (viewGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(viewGeneratorModel.LayoutPage));

            ViewGeneratorTemplateModel templateModel = new ViewGeneratorTemplateModel()
            {
                ViewDataTypeName = modelTypeAndContextModel?.ModelType?.FullName,
                ViewDataTypeShortName = modelTypeAndContextModel?.ModelType?.Name,
                ViewName = viewGeneratorModel.ViewName,
                LayoutPageFile = viewGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = viewGeneratorModel.PartialView,
                ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelTypeAndContextModel?.ContextProcessingResult?.ModelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            return templateModel;
        }
    }
}
