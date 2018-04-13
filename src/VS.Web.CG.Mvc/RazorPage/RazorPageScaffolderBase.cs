// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public abstract class RazorPageScaffolderBase : CommonGeneratorBase
    {
        protected readonly IProjectContext _projectContext;
        protected readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;

        public RazorPageScaffolderBase(
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
                    baseFolders: new[] { "RazorPageGenerator" },
                    projectContext: _projectContext);
            }
        }

        protected async Task AddRequiredFiles(RazorPageGeneratorModel razorGeneratorModel)
        {
            IEnumerable<RequiredFileEntity> requiredFiles = GetRequiredFiles(razorGeneratorModel);
            foreach (var file in requiredFiles)
            {
                if (ShouldFileBeAdded(file))
                {
                    await _codeGeneratorActionsService.AddFileAsync(
                        Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                        Path.Combine(TemplateFolders.First(), file.TemplateName));
                    _logger.LogMessage($"Added additional file :{file.OutputPath}");
                }
            }
        }

        private bool ShouldFileBeAdded(RequiredFileEntity fileEntity)
        {
            if (File.Exists(Path.Combine(ApplicationInfo.ApplicationBasePath, fileEntity.OutputPath)))
            {
                return false;
            }

            foreach (string altPath in fileEntity.AltPaths)
            {
                if (File.Exists(Path.Combine(ApplicationInfo.ApplicationBasePath, altPath)))
                {
                    return false;
                }
            }

            return true;
        }

        public abstract Task GenerateCode(RazorPageGeneratorModel razorGeneratorModel);

        internal async Task GenerateView(RazorPageGeneratorModel razorGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            if (razorGeneratorModel.RazorPageName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = razorGeneratorModel.RazorPageName.Length - Constants.ViewExtension.Length;
                razorGeneratorModel.RazorPageName = razorGeneratorModel.RazorPageName.Substring(0, viewNameLength);
            }

            var templateModel = modelTypeAndContextModel == null
                ? GetRazorPageViewGeneratorTemplateModel(razorGeneratorModel)
                : GetRazorPageWithContextTemplateModel(razorGeneratorModel, modelTypeAndContextModel);

            var templateName = razorGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            var pageModelTemplateName = razorGeneratorModel.TemplateName + "PageModel" + Constants.RazorTemplateExtension;
            var pageModelOutputPath = outputPath + ".cs";
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added RazorPage : " + outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length));
            if (!razorGeneratorModel.NoPageModel)
            {
                await _codeGeneratorActionsService.AddFileFromTemplateAsync(pageModelOutputPath, pageModelTemplateName, TemplateFolders, templateModel);
                _logger.LogMessage("Added PageModel : " + pageModelOutputPath.Substring(ApplicationInfo.ApplicationBasePath.Length));
            }

            await AddRequiredFiles(razorGeneratorModel);
        }

        protected abstract IEnumerable<RequiredFileEntity> GetRequiredFiles(RazorPageGeneratorModel razorGeneratorModel);

        protected RazorPageGeneratorTemplateModel GetRazorPageViewGeneratorTemplateModel(RazorPageGeneratorModel razorGeneratorModel)
        {
            bool isLayoutSelected = !razorGeneratorModel.PartialView &&
                (razorGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(razorGeneratorModel.LayoutPage));

            var namespaceName = string.IsNullOrEmpty(razorGeneratorModel.NamespaceName)
                ? GetDefaultPageModelNamespaceName(razorGeneratorModel.RelativeFolderPath)
                : razorGeneratorModel.NamespaceName;

            RazorPageGeneratorTemplateModel templateModel = new RazorPageGeneratorTemplateModel()
            {
                NamespaceName = namespaceName,
                NoPageModel = razorGeneratorModel.NoPageModel,
                PageModelClassName = razorGeneratorModel.RazorPageName+"Model",
                RazorPageName = razorGeneratorModel.RazorPageName,
                LayoutPageFile = razorGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = razorGeneratorModel.PartialView,
                ReferenceScriptLibraries = razorGeneratorModel.ReferenceScriptLibraries,
                JQueryVersion = "1.10.2" //Todo
            };

            return templateModel;
        }

        protected RazorPageWithContextTemplateModel GetRazorPageWithContextTemplateModel(RazorPageGeneratorModel razorGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel)
        {
            bool isLayoutSelected = !razorGeneratorModel.PartialView &&
                (razorGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(razorGeneratorModel.LayoutPage));

            var namespaceName = string.IsNullOrEmpty(razorGeneratorModel.NamespaceName)
                ? GetDefaultPageModelNamespaceName(razorGeneratorModel.RelativeFolderPath)
                : razorGeneratorModel.NamespaceName;

            RazorPageWithContextTemplateModel templateModel = new RazorPageWithContextTemplateModel(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName)
            {
                NamespaceName = namespaceName,
                NoPageModel = razorGeneratorModel.NoPageModel,
                PageModelClassName = razorGeneratorModel.RazorPageName + "Model",
                ViewDataTypeName = modelTypeAndContextModel?.ModelType?.FullName,
                ViewDataTypeShortName = modelTypeAndContextModel?.ModelType?.Name,
                ContextTypeName = modelTypeAndContextModel?.DbContextFullName,
                RazorPageName = razorGeneratorModel.RazorPageName,
                LayoutPageFile = razorGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = razorGeneratorModel.PartialView,
                ReferenceScriptLibraries = razorGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelTypeAndContextModel?.ContextProcessingResult?.ModelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            return templateModel;
        }

        private string GetDefaultPageModelNamespaceName(string relativeFolderPath)
        {
            return NameSpaceUtilities.GetSafeNameSpaceFromPath(relativeFolderPath, ApplicationInfo.ApplicationName);
        }
    }
}
