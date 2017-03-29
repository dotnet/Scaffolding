// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class ModelBasedViewScaffolder : ViewScaffolderBase
    {
        private IModelTypesLocator _modelTypesLocator;
        private ICodeModelService _codeModelService;

        public ModelBasedViewScaffolder(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            ICodeModelService codeModelService,
            IServiceProvider serviceProvider,
            ILogger logger) 
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }
            if (codeModelService == null)
            {
                throw new ArgumentNullException(nameof(codeModelService));
            }

            _codeModelService = codeModelService;
            _modelTypesLocator = modelTypesLocator;
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

            modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetCodeModelMetadata(viewGeneratorModel, _codeModelService, _modelTypesLocator);

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            await GenerateView(viewGeneratorModel, modelTypeAndContextModel, outputPath);
            await layoutDependencyInstaller.InstallDependencies();
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
    }
}
