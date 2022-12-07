// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class EFModelBasedViewScaffolder : ViewScaffolderBase
    {
        private IEntityFrameworkService _entityFrameworkService;
        private IModelTypesLocator _modelTypesLocator;
        private IFileSystem _fileSystem;
        private bool CalledFromCommandline => !(_fileSystem is SimulationModeFileSystem);
        
        public EFModelBasedViewScaffolder(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            IEntityFrameworkService entityFrameworkService,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger,
            IFileSystem fileSystem) 
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
            _modelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            _entityFrameworkService = entityFrameworkService ?? throw new ArgumentNullException(nameof(entityFrameworkService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public override async Task GenerateCode(ViewGeneratorModel viewGeneratorModel)
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

            ModelTypeAndContextModel modelTypeAndContextModel = null;
            var outputPath = ValidateAndGetOutputPath(viewGeneratorModel, outputFileName: viewGeneratorModel.ViewName + Constants.ViewExtension);
            if (!string.IsNullOrEmpty(_projectContext.TargetFrameworkMoniker) && CalledFromCommandline)
            {
                EFValidationUtil.ValidateEFDependencies(_projectContext.PackageDependencies, viewGeneratorModel.DatabaseProvider);
            }
            

            modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetEFMetadata(
                viewGeneratorModel,
                _entityFrameworkService,
                _modelTypesLocator,
                _logger,
                string.Empty);
 
            await GenerateView(viewGeneratorModel, modelTypeAndContextModel, outputPath);

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered, MessageStrings.Scaffolding_additionalSteps));
            }
        }

        protected override IEnumerable<RequiredFileEntity> GetRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            List<RequiredFileEntity> requiredFiles = new List<RequiredFileEntity>();

            if (viewGeneratorModel.ReferenceScriptLibraries)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Views/Shared/_ValidationScriptsPartial.cshtml", @"_ValidationScriptsPartial.cshtml"));
            }

            return requiredFiles;
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

            if (viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewsAndTemplates));
            }

            if (modelTypeAndContextModel == null)
            {
                throw new ArgumentNullException(nameof(modelTypeAndContextModel));
            }

            if (string.IsNullOrEmpty(baseOutputPath))
            {
                baseOutputPath = ApplicationInfo.ApplicationBasePath;
            }

            IEnumerable<string> templateFolders = GetTemplateFoldersForContentVersion(viewGeneratorModel);

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

                var templateModel = GetViewGeneratorTemplateModel(viewGeneratorModel, modelTypeAndContextModel);
                templateModel.ViewName = viewName;

                templateName = templateName + Constants.RazorTemplateExtension;
                await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, templateFolders, templateModel);
                _logger.LogMessage($"Added View : {outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length)}");
            }

            await AddRequiredFiles(viewGeneratorModel);
        }
    }
}
