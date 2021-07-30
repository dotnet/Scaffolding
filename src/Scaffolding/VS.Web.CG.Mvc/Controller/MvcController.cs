// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    /// <summary>
    /// MvcController class provides basic functionality for scaffolding an MVC controller. 
    /// The specific type of controller (Empty, Controller with read write actions etc, need to provide the template names to be used for scaffolding.
    /// </summary>
    public abstract class MvcController : ControllerGeneratorBase
    {
        public MvcController(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }

        public override async Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            if (!string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                if (!controllerGeneratorModel.ControllerName.EndsWith(Constants.ControllerSuffix, StringComparison.Ordinal))
                {
                    controllerGeneratorModel.ControllerName = controllerGeneratorModel.ControllerName + Constants.ControllerSuffix;
                }
            }
            else
            {
                throw new ArgumentException(GetRequiredNameError);
            }
            ValidateNameSpaceName(controllerGeneratorModel);
            var namespaceName = string.IsNullOrEmpty(controllerGeneratorModel.ControllerNamespace)
                ? GetDefaultControllerNamespace(controllerGeneratorModel.RelativeFolderPath)
                : controllerGeneratorModel.ControllerNamespace;
            var templateModel = new ClassNameModel(className: controllerGeneratorModel.ControllerName, namespaceName: namespaceName);

            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, GetTemplateName(controllerGeneratorModel), TemplateFolders, templateModel);
            Logger.LogMessage(string.Format(MessageStrings.AddedController, outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length)));
        }

        protected virtual string GetRequiredNameError
        {
            get
            {
                return MessageStrings.ControllerNameRequired;
            }
        }
        
    }
}
