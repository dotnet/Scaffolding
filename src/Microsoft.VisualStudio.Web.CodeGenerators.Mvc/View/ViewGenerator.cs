// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;


namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    /// <summary>
    /// Both the command line entry point for View generation (implements ICodeGenerator with Alias attribute)
    /// and View generator itself (extends CommonGeneratorBase).
    /// Consider separating if there are going to multiple view generators like Controllers.
    /// </summary>
    [Alias("view")]
    public class ViewGenerator : CommonGeneratorBase, ICodeGenerator
    {
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IEntityFrameworkService _entityFrameworkService;
        private readonly ILibraryManager _libraryManager;
        private readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        // Todo: Instead of each generator taking services, provide them in some base class?
        // However for it to be effective, it should be property dependecy injection rather
        // than constructor injection.
        public ViewGenerator(
            ILibraryManager libraryManager,
            IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            IEntityFrameworkService entityFrameworkService,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(applicationInfo)
        {
            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (entityFrameworkService == null)
            {
                throw new ArgumentNullException(nameof(entityFrameworkService));
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

            _libraryManager = libraryManager;
            _codeGeneratorActionsService = codeGeneratorActionsService;
            _modelTypesLocator = modelTypesLocator;
            _entityFrameworkService = entityFrameworkService;
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
                    libraryManager: _libraryManager);
            }
        }

        public async Task GenerateCode(ViewGeneratorModel viewGeneratorModel)
        {
            if (viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewGeneratorModel));
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new ArgumentException(MessageStrings.ViewNameRequired);
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.TemplateName))
            {
                throw new ArgumentException(MessageStrings.TemplateNameRequired);
            }

            ModelTypeAndContextModel modelTypeAndContextModel;
            var outputPath = ValidateAndGetOutputPath(viewGeneratorModel, outputFileName: viewGeneratorModel.ViewName + Constants.ViewExtension);
            if (string.IsNullOrEmpty(viewGeneratorModel.DataContextClass))
            {
                modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetCodeModelMetadata(viewGeneratorModel, _entityFrameworkService, _modelTypesLocator);
            }
            else
            {
                modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetMetadata(viewGeneratorModel, _entityFrameworkService, _modelTypesLocator);
            }

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            await GenerateView(viewGeneratorModel, modelTypeAndContextModel, outputPath);
            await layoutDependencyInstaller.InstallDependencies();

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered, MessageStrings.Scaffolding_additionalSteps));
            }
        }

        internal async Task GenerateView(ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string outputPath)
        {
            if (viewGeneratorModel.ViewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = viewGeneratorModel.ViewName.Length - Constants.ViewExtension.Length;
                viewGeneratorModel.ViewName = viewGeneratorModel.ViewName.Substring(0, viewNameLength);
            }

            bool isLayoutSelected = !viewGeneratorModel.PartialView &&
                (viewGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(viewGeneratorModel.LayoutPage));

            var templateModel = new ViewGeneratorTemplateModel()
            {
                ViewDataTypeName = modelTypeAndContextModel.ModelType.FullName,
                ViewDataTypeShortName = modelTypeAndContextModel.ModelType.Name,
                ViewName = viewGeneratorModel.ViewName,
                LayoutPageFile = viewGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = viewGeneratorModel.PartialView,
                ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelTypeAndContextModel.ContextProcessingResult.ModelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            var templateName = viewGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage($"Added View : {outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length)}");

            await GenerateRequiredFiles(viewGeneratorModel);
        }

        /// <summary>
        /// Method exposed for adding multiple views in one operation.
        /// Utilised by the ControllerWithContextGenerator which generates 5 views for a MVC controller with context.
        /// </summary>
        /// <param name="viewsAndTemplates">Names of views and the corresponding template names</param>
        /// <param name="viewGeneratorModel">Model for View Generator</param>
        /// <param name="modelTypeAndContextModel">Model Type and DbContext metadata</param>
        /// <param name="baseOutputPath">Folder where all views will be generated</param>
        internal async Task GenerateViews(Dictionary<string, string> viewsAndTemplates, ViewGeneratorModel viewGeneratorModel, ModelTypeAndContextModel modelTypeAndContextModel, string baseOutputPath)
        {

            if (viewsAndTemplates == null)
            {
                throw new ArgumentNullException(nameof(viewsAndTemplates));
            }

            if(viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewsAndTemplates));
            }

            if(modelTypeAndContextModel == null)
            {
                throw new ArgumentNullException(nameof(modelTypeAndContextModel));
            }

            if(string.IsNullOrEmpty(baseOutputPath))
            {
                baseOutputPath = ApplicationInfo.ApplicationBasePath;
            }

            foreach (var entry in viewsAndTemplates)
            {
                var viewName = entry.Key;
                var templateName = entry.Value;
                if (viewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
                {
                    int viewNameLength = viewName.Length - Constants.ViewExtension.Length;
                    viewName = viewName.Substring(0, viewNameLength);
                }

                var outputPath = Path.Combine(baseOutputPath, viewName + Constants.ViewExtension);
                bool isLayoutSelected = !viewGeneratorModel.PartialView &&
                    (viewGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(viewGeneratorModel.LayoutPage));

                var templateModel = new ViewGeneratorTemplateModel()
                {
                    ViewDataTypeName = modelTypeAndContextModel.ModelType.FullName,
                    ViewDataTypeShortName = modelTypeAndContextModel.ModelType.Name,
                    ViewName = viewName,
                    LayoutPageFile = viewGeneratorModel.LayoutPage,
                    IsLayoutPageSelected = isLayoutSelected,
                    IsPartialView = viewGeneratorModel.PartialView,
                    ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                    ModelMetadata = modelTypeAndContextModel.ContextProcessingResult.ModelMetadata,
                    JQueryVersion = "1.10.2" //Todo
                };

                templateName = templateName + Constants.RazorTemplateExtension;
                await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
                _logger.LogMessage($"Added View : {outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length)}");
            }
            await GenerateRequiredFiles(viewGeneratorModel);
        }

        private async Task GenerateRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            List<RequiredFileEntity> requiredFiles = new List<RequiredFileEntity>();

            if (viewGeneratorModel.ReferenceScriptLibraries)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Views/Shared/_ValidationScriptsPartial.cshtml", @"_ValidationScriptsPartial.cshtml"));
            }

            await AddRequiredFiles(requiredFiles);
        }

        private async Task AddRequiredFiles(IEnumerable<RequiredFileEntity> requiredFiles)
        {
            foreach (var file in requiredFiles)
            {
                if (!File.Exists(Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath)))
                {
                    await _codeGeneratorActionsService.AddFileAsync(
                        Path.Combine(ApplicationInfo.ApplicationBasePath, file.OutputPath),
                        Path.Combine(TemplateFolders.First(), file.TemplateName));
                    _logger.LogMessage($"Added additional file :{file.OutputPath}");
                }
            }
        }
    }
}