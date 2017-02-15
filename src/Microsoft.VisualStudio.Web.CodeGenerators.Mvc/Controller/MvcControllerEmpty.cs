// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public class MvcControllerEmpty : MvcController
    {
        public MvcControllerEmpty(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }
        protected override string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? Constants.ApiEmptyControllerTemplate : Constants.MvcEmptyControllerTemplate;
        }
        protected override string GetRequiredNameError
        {
            get
            {
                return MessageStrings.EmptyControllerNameRequired;
            }
        }
    }
}
