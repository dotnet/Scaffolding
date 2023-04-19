// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
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
