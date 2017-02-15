// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class EmptyViewScaffolder : ViewScaffolderBase
    {
        private static IEnumerable<RequiredFileEntity> RequiredFiles = new List<RequiredFileEntity>();
        public EmptyViewScaffolder(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {

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

            var outputPath = ValidateAndGetOutputPath(viewGeneratorModel, outputFileName: viewGeneratorModel.ViewName + Constants.ViewExtension);
            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            await GenerateView(viewGeneratorModel, null, outputPath);
            await layoutDependencyInstaller.InstallDependencies();
        }

        protected override IEnumerable<RequiredFileEntity> GetRequiredFiles(ViewGeneratorModel viewGeneratorModel)
        {
            return RequiredFiles;
        }
    }
}
