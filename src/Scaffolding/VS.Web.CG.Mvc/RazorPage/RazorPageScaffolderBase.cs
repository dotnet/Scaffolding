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
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public abstract class RazorPageScaffolderBase : CommonGeneratorBase
    {
        protected readonly IProjectContext _projectContext;
        protected readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;

        internal static readonly string DefaultBootstrapVersion = "5";
        // A hashset would allow faster lookups, but would cause a perf hit when formatting the error string for invalid bootstrap version.
        // Also, with a list this small, the lookup perf hit will be largely irrelevant.
        internal static readonly IReadOnlyList<string> ValidBootstrapVersions = new List<string>()
        {
            "3",
            "4",
            "5"
        };

        internal static readonly string ContentVersionDefault = "Default";
        internal static readonly string ContentVersionBootstrap3 = "Bootstrap3";
        internal static readonly string ContentVersionBootstrap4 = "Bootstrap4";

        internal static readonly string DefaultContentRelativeBaseDir = "RazorPageGenerator";
        internal static readonly string VersionedContentRelativeBaseDir = "RazorPageGenerator_Versioned";

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

        // TODO: At the next major release, this should be refactored in conjunction with changes to the razor scaffolding API's
        // that take in the command line model (RazorPageGeneratorModel) and make a TemplateModel from it.
        private RazorPageGeneratorTemplateModel _templateModel;
        internal RazorPageGeneratorTemplateModel TemplateModel
        {
            get
            {
                if (_templateModel == null)
                {
                    throw new Exception("TemplateModel needs to be set before being used.");
                }

                return _templateModel;
            }
            set
            {
                _templateModel = value;
            }
        }

        public virtual IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] { DefaultContentRelativeBaseDir },
                    projectContext: _projectContext);
            }
        }

        internal IEnumerable<string> GetTemplateFoldersForContentVersion()
        {
            string contentVersion = null;
            string bootstrapVersion = null;

            if (TemplateModel is RazorPageGeneratorTemplateModel2 templateModel2)
            {
                bootstrapVersion = templateModel2.BootstrapVersion;
                contentVersion = templateModel2.ContentVersion;
            }
            else if (TemplateModel is RazorPageWithContextTemplateModel2 templateWithContextModel2)
            {
                bootstrapVersion = templateWithContextModel2.BootstrapVersion;
                contentVersion = templateWithContextModel2.ContentVersion;
            }
            else
            {   // for back-compat
                return TemplateFolders;
            }

            // The default content is packaged under the default location "RazorPageGenerator\*" (no subfolder).
            // Note: In the future, if content gets pivoted on things other than bootstrap, this logic will need enhancement.
            if (string.Equals(contentVersion, ContentVersionDefault, StringComparison.Ordinal))
            {
                return TemplateFolders;
            }

            // For non-default content versions, the content is packaged under "RazorPageGenerator_Versioned\[Version_Identifier]\*"
            if (string.Equals(contentVersion, ContentVersionBootstrap3, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] {
                        Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{bootstrapVersion}")
                    },
                    projectContext: _projectContext);
            }
            else if (string.Equals(contentVersion, ContentVersionBootstrap4, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] {
                        Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{bootstrapVersion}")
                    },
                    projectContext: _projectContext);
            }
            //If no bootstrap version could be decided/invalid, return default bootstrap 5 templates.
            return TemplateFolders;
        }

        protected async Task AddRequiredFiles(RazorPageGeneratorModel razorGeneratorModel)
        {
            IEnumerable<RequiredFileEntity> requiredFiles = GetRequiredFiles(razorGeneratorModel);

            string templateFolderRoot = GetTemplateFoldersForContentVersion().First();

            foreach (var file in requiredFiles)
            {
                if (ShouldFileBeAdded(file))
                {
                    await _codeGeneratorActionsService.AddFileAsync(
                        Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                        Path.Combine(templateFolderRoot, file.TemplateName));
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

        // TODO: rework this to use the TemplateModel, as opposed to the RazorPageGeneratorModel (command line model)
        internal async Task GenerateView(RazorPageGeneratorModel razorGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            IEnumerable<string> templateFolders = GetTemplateFoldersForContentVersion();

            if (razorGeneratorModel.RazorPageName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = razorGeneratorModel.RazorPageName.Length - Constants.ViewExtension.Length;
                razorGeneratorModel.RazorPageName = razorGeneratorModel.RazorPageName.Substring(0, viewNameLength);
            }

            var templateName = razorGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            var pageModelTemplateName = razorGeneratorModel.TemplateName + "PageModel" + Constants.RazorTemplateExtension;
            var pageModelOutputPath = outputPath + ".cs";
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, templateFolders, TemplateModel);
            _logger.LogMessage("Added RazorPage : " + outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length));
            if (!razorGeneratorModel.NoPageModel)
            {
                await _codeGeneratorActionsService.AddFileFromTemplateAsync(pageModelOutputPath, pageModelTemplateName, templateFolders, TemplateModel);
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

            if (string.IsNullOrEmpty(razorGeneratorModel.BootstrapVersion))
            {
                razorGeneratorModel.BootstrapVersion = RazorPageScaffolderBase.DefaultBootstrapVersion;
            }

            RazorPageGeneratorTemplateModel2 templateModel = new RazorPageGeneratorTemplateModel2()
            {
                NamespaceName = namespaceName,
                NoPageModel = razorGeneratorModel.NoPageModel,
                PageModelClassName = razorGeneratorModel.RazorPageName+"Model",
                RazorPageName = razorGeneratorModel.RazorPageName,
                LayoutPageFile = razorGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = razorGeneratorModel.PartialView,
                ReferenceScriptLibraries = razorGeneratorModel.ReferenceScriptLibraries,
                JQueryVersion = "1.10.2", //Todo
                BootstrapVersion = razorGeneratorModel.BootstrapVersion,
                ContentVersion = DetermineContentVersion(razorGeneratorModel)
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

            if (string.IsNullOrEmpty(razorGeneratorModel.BootstrapVersion))
            {
                razorGeneratorModel.BootstrapVersion = RazorPageScaffolderBase.DefaultBootstrapVersion;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            RazorPageWithContextTemplateModel2 templateModel = new RazorPageWithContextTemplateModel2(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName)
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
                JQueryVersion = "1.10.2", //Todo
                BootstrapVersion = razorGeneratorModel.BootstrapVersion,
                ContentVersion = DetermineContentVersion(razorGeneratorModel),
                UseSqlite = razorGeneratorModel.UseSqlite,
                NullableEnabled = "enable".Equals(ApplicationInfo?.WorkspaceHelper?.GetMsBuildProperty("Nullable"), StringComparison.OrdinalIgnoreCase)
            };

            return templateModel;
        }

        private string GetDefaultPageModelNamespaceName(string relativeFolderPath)
        {
            return NameSpaceUtilities.GetSafeNameSpaceFromPath(relativeFolderPath, _projectContext.RootNamespace);
        }

        private static string DetermineContentVersion(RazorPageGeneratorModel razorGeneratorModel)
        {
            if (string.Equals(razorGeneratorModel.BootstrapVersion, DefaultBootstrapVersion, StringComparison.Ordinal))
            {
                return ContentVersionDefault;
            }
            else if (string.Equals(razorGeneratorModel.BootstrapVersion, "3", StringComparison.Ordinal))
            {
                return ContentVersionBootstrap3;
            }
            else if (string.Equals(razorGeneratorModel.BootstrapVersion, "4", StringComparison.Ordinal))
            {
                return ContentVersionBootstrap4;
            }
            return ContentVersionDefault;
        }
    }
}
