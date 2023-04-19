// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public class MvcControllerWithReadWriteActionGenerator : MvcController
    {
        public MvcControllerWithReadWriteActionGenerator(IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }
        protected override string GetRequiredNameError
        {
            get
            {
                return MessageStrings.ControllerNameRequired;
            }
        }
        protected override string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? Constants.ApiControllerWithActionsTemplate : Constants.MvcControllerWithActionsTemplate;
        }
    }
}
