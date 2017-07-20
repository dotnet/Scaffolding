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

        protected bool IsRazorPageWireUpNeeded { get; set; }

        protected async Task AddRequiredFiles(RazorPageGeneratorModel razorGeneratorModel)
        {
            IEnumerable<RequiredFileEntity> requiredFiles = GetRequiredFiles(razorGeneratorModel);
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

        public abstract Task GenerateCode(RazorPageGeneratorModel razorGeneratorModel);

        internal async Task GenerateView(RazorPageGeneratorModel razorGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            if (razorGeneratorModel.ViewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = razorGeneratorModel.ViewName.Length - Constants.ViewExtension.Length;
                razorGeneratorModel.ViewName = razorGeneratorModel.ViewName.Substring(0, viewNameLength);
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

        protected virtual IEnumerable<RequiredFileEntity> GetRequiredFiles(RazorPageGeneratorModel razorGeneratorModel)
        {
            List<RequiredFileEntity> requiredFiles = new List<RequiredFileEntity>();
            var namespaceName = string.IsNullOrEmpty(razorGeneratorModel.NamespaceName)
                ? GetDefaultPageModelNamespaceName(razorGeneratorModel.RelativeFolderPath)
                : razorGeneratorModel.NamespaceName;

            if (razorGeneratorModel.ReferenceScriptLibraries)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Pages/_ValidationScriptsPartial.cshtml", @"_ValidationScriptsPartial.cshtml"));
            }

            if (IsRazorPageWireUpNeeded)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Pages/_ViewImports.cshtml", "_ViewImports.cshtml", false, new RequiredFilesTemplateModel() { RootNamespace = namespaceName }));
                if (razorGeneratorModel.UseDefaultLayout)
                {
                    requiredFiles.Add(new RequiredFileEntity(@"Pages/_ViewStart.cshtml", "_ViewStart.cshtml"));
                    requiredFiles.Add(new RequiredFileEntity(@"Pages/_Layout.cshtml", "_Layout.cshtml", false, new RequiredFilesTemplateModel() { AppName = ApplicationInfo.ApplicationName }));
                }
            }

            return requiredFiles;
        }

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
                PageModelClassName = razorGeneratorModel.ViewName+"Model",
                ViewName = razorGeneratorModel.ViewName,
                LayoutPageFile = razorGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = razorGeneratorModel.PartialView,
                ReferenceScriptLibraries = razorGeneratorModel.ReferenceScriptLibraries,
                JQueryVersion = "1.10.2" //Todo
            };

            return templateModel;
        }

        protected bool RazorPagesFolderExists(string relativeFolderPath, string applicationBasePath)
        {
            const string viewsFolderName = "Pages";
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
                PageModelClassName = razorGeneratorModel.ViewName + "Model",
                ViewDataTypeName = modelTypeAndContextModel?.ModelType?.FullName,
                ViewDataTypeShortName = modelTypeAndContextModel?.ModelType?.Name,
                ContextTypeName = modelTypeAndContextModel?.DbContextFullName,
                ViewName = razorGeneratorModel.ViewName,
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
