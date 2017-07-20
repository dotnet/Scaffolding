// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class EmptyRazorPageScaffolder : RazorPageScaffolderBase
    {
        private static IEnumerable<RequiredFileEntity> RequiredFiles = new List<RequiredFileEntity>();

        public EmptyRazorPageScaffolder(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
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

            var outputPath = ValidateAndGetOutputPath(razorGeneratorModel, outputFileName: razorGeneratorModel.ViewName + Constants.ViewExtension);
            IsRazorPageWireUpNeeded = !RazorPagesFolderExists(razorGeneratorModel.RelativeFolderPath, ApplicationInfo.ApplicationBasePath);
            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            await GenerateView(razorGeneratorModel, null, outputPath);
            await layoutDependencyInstaller.InstallDependencies();
        }
    }
}
