// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class MockControllerGenerator : ControllerGeneratorBase
    {
        public MockControllerGenerator(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }

        public override Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            ValidateNameSpaceName(controllerGeneratorModel);
            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            return Task.CompletedTask;
        }

        protected override string GetTemplateName(CommandLineGeneratorModel controllerGeneratorModel)
        {
            throw new NotImplementedException();
        }
    }
}
