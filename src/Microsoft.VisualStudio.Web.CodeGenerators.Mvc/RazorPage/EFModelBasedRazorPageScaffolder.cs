// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class EFModelBasedRazorPageScaffolder : RazorPageScaffolderBase
    {
        private IEntityFrameworkService _entityFrameworkService;
        private IModelTypesLocator _modelTypesLocator;

        public EFModelBasedRazorPageScaffolder(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            IEntityFrameworkService entityFrameworkService,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (entityFrameworkService == null)
            {
                throw new ArgumentNullException(nameof(entityFrameworkService));
            }

            _modelTypesLocator = modelTypesLocator;
            _entityFrameworkService = entityFrameworkService;
        }

        public override async Task GenerateCode(RazorPageGeneratorModel razorGeneratorModel)
        {
            if (razorGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(razorGeneratorModel));
            }

            if (string.IsNullOrEmpty(razorGeneratorModel.ViewName))
            {
                // TODO: make a separate message resource string for this (currently setup using the one for VIEW)
                throw new ArgumentException(MessageStrings.ViewNameRequired);
            }

            if (string.IsNullOrEmpty(razorGeneratorModel.TemplateName))
            {
                throw new ArgumentException(MessageStrings.TemplateNameRequired);
            }

            ModelTypeAndContextModel modelTypeAndContextModel = null;
            var outputPath = ValidateAndGetOutputPath(razorGeneratorModel, outputFileName: razorGeneratorModel.ViewName + Constants.ViewExtension);

            EFValidationUtil.ValidateEFDependencies(_projectContext.PackageDependencies);

            modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetEFMetadata(
                razorGeneratorModel,
                _entityFrameworkService,
                _modelTypesLocator,
                string.Empty);

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            await GenerateView(razorGeneratorModel, modelTypeAndContextModel, outputPath);
            await layoutDependencyInstaller.InstallDependencies();

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered, MessageStrings.Scaffolding_additionalSteps));
            }
        }

        protected override IEnumerable<RequiredFileEntity> GetRequiredFiles(RazorPageGeneratorModel viewGeneratorModel)
        {
            List<RequiredFileEntity> requiredFiles = new List<RequiredFileEntity>();

            if (viewGeneratorModel.ReferenceScriptLibraries)
            {
                requiredFiles.Add(new RequiredFileEntity(@"Views/Shared/_ValidationScriptsPartial.cshtml", @"_ValidationScriptsPartial.cshtml"));
            }

            return requiredFiles;
        }
    }
}
