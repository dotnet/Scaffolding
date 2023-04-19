// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

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

            if (string.IsNullOrEmpty(razorGeneratorModel.RazorPageName))
            {
                throw new ArgumentException(MessageStrings.RazorPageNameRequired);
            }

            if (string.IsNullOrEmpty(razorGeneratorModel.TemplateName))
            {
                throw new ArgumentException(MessageStrings.TemplateNameRequired);
            }

            TemplateModel = GetRazorPageViewGeneratorTemplateModel(razorGeneratorModel);

            var outputPath = ValidateAndGetOutputPath(razorGeneratorModel, outputFileName: razorGeneratorModel.RazorPageName + Constants.ViewExtension);

            await GenerateView(razorGeneratorModel, null, outputPath);
        }

        protected override IEnumerable<RequiredFileEntity> GetRequiredFiles(RazorPageGeneratorModel razorGeneratorModel)
        {
            return RequiredFiles;
        }
    }
}
