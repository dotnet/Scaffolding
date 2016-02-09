// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class MvcControllerEmpty : MvcController
    {
        public MvcControllerEmpty(
            ILibraryManager libraryManager,
            IApplicationEnvironment environment,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(libraryManager, environment, codeGeneratorActionsService, serviceProvider, logger)
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
                return CodeGenerators.Mvc.MessageStrings.EmptyControllerNameRequired;
            }
        }
    }
}
