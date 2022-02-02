// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public abstract class ViewScaffolderBase : CommonGeneratorBase
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
                    baseFolders: new[] { ViewGenerator.DefaultContentRelativeBaseDir },
                    projectContext: _projectContext);
            }
        }

        private static string ContentVersionFromModel(ViewGeneratorModel model)
        {
            if (string.IsNullOrEmpty(model.BootstrapVersion)
                || string.Equals(model.BootstrapVersion, ViewGenerator.DefaultBootstrapVersion, StringComparison.Ordinal))
            {
                return ViewGenerator.ContentVersionDefault;
            }
            else if (string.Equals(model.BootstrapVersion, "3", StringComparison.Ordinal))
            {
                return ViewGenerator.ContentVersionBootstrap3;
            }
            else if (string.Equals(model.BootstrapVersion, "4", StringComparison.Ordinal))
            {
                return ViewGenerator.ContentVersionBootstrap4;
            }
            return ViewGenerator.ContentVersionDefault;
        }

        protected IEnumerable<string> GetTemplateFoldersForContentVersion(ViewGeneratorModel model)
        {
            if (string.IsNullOrEmpty(model.BootstrapVersion))
            {   // for back-compat
                return TemplateFolders;
            }

            string contentVersion = ContentVersionFromModel(model);

            // the default content is packaged under the default location (no subfolder)
            if (string.Equals(contentVersion, ViewGenerator.ContentVersionDefault, StringComparison.Ordinal))
            {
                return TemplateFolders;
            }

            if (string.Equals(contentVersion, ViewGenerator.ContentVersionBootstrap3, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] { Path.Combine(ViewGenerator.VersionedContentRelativeBaseDir, ViewGenerator.ContentVersionBootstrap3) },
                    projectContext: _projectContext);
            }
            else if (string.Equals(contentVersion, ViewGenerator.ContentVersionBootstrap4, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] { Path.Combine(ViewGenerator.VersionedContentRelativeBaseDir, ViewGenerator.ContentVersionBootstrap4) },
                    projectContext: _projectContext);
            }
            return TemplateFolders;
        }

        protected async Task AddRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            IEnumerable<RequiredFileEntity> requiredFiles = GetRequiredFiles(viewGeneratorModel);
            string templateFolder = GetTemplateFoldersForContentVersion(viewGeneratorModel).First();

            foreach (var file in requiredFiles)
            {
                if (!File.Exists(Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath)))
                {
                    await _codeGeneratorActionsService.AddFileAsync(
                        Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                        Path.Combine(templateFolder, file.TemplateName));
                    _logger.LogMessage($"Added additional file :{file.OutputPath}");
                }
            }
        }

        public abstract Task GenerateCode(ViewGeneratorModel viewGeneratorModel);

        internal async Task GenerateView(ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            IEnumerable<string> templateFolders = GetTemplateFoldersForContentVersion(viewGeneratorModel);

            if (viewGeneratorModel.ViewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = viewGeneratorModel.ViewName.Length - Constants.ViewExtension.Length;
                viewGeneratorModel.ViewName = viewGeneratorModel.ViewName.Substring(0, viewNameLength);
            }

            var templateModel = GetViewGeneratorTemplateModel(viewGeneratorModel, modelTypeAndContextModel);
            var templateName = viewGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, templateFolders, templateModel);
            _logger.LogMessage("Added View : " + outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length));

            await AddRequiredFiles(viewGeneratorModel);
        }

        protected abstract IEnumerable<RequiredFileEntity> GetRequiredFiles(ViewGeneratorModel viewGeneratorModel);

        protected ViewGeneratorTemplateModel GetViewGeneratorTemplateModel(ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel)
        {
            bool isLayoutSelected = !viewGeneratorModel.PartialView &&
                (viewGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(viewGeneratorModel.LayoutPage));

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
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
                JQueryVersion = "1.10.2", //Todo,
                NullableEnabled = "enable".Equals(ApplicationInfo?.WorkspaceHelper?.GetMsBuildProperty("Nullable"), StringComparison.OrdinalIgnoreCase)
            };

            return templateModel;
        }
    }
}
