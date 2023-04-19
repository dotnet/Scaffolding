// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Cli.Utils;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public class ControllerEmpty : MvcController
    {
        public ControllerEmpty(
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
            throw new NotImplementedException("Switched to using 'dotnet new', not shipping these templates anymore.");
        }

        public override Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            //item name in `dotnet new` for api controller or mvc controller (empty with or without actions)
            var controllerTypeName = controllerGeneratorModel.IsRestController ? "apicontroller" : "mvccontroller";
            var actionsParameter = controllerGeneratorModel.GenerateReadWriteActions ? "--actions" : string.Empty;
            //arguments for `dotnet new page`
            var additionalArgs = new List<string>()
            {
                controllerTypeName,
                "--name",
                controllerGeneratorModel.ControllerName,
                "--output",
                Path.GetDirectoryName(outputPath),
                "--force",
                controllerGeneratorModel.Force.ToString(),
                actionsParameter,
            };

            if (!string.IsNullOrEmpty(controllerGeneratorModel.ControllerNamespace))
            {
                additionalArgs.Add("--namespace");
                additionalArgs.Add(controllerGeneratorModel.ControllerNamespace);
            }

            DotnetCommands.ExecuteDotnetNew(ProjectContext.ProjectFullPath, additionalArgs, Logger);
            return Task.CompletedTask;
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
