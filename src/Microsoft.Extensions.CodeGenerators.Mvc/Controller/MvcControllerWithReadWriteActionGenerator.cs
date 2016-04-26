// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.DotNet;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class MvcControllerWithReadWriteActionGenerator : MvcController
    {
        public MvcControllerWithReadWriteActionGenerator(ILibraryManager libraryManager,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(libraryManager, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }
        protected override string GetRequiredNameError
        {
            get
            {
                return CodeGenerators.Mvc.MessageStrings.ControllerNameRequired;
            }
        }
        protected override string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? Constants.ApiControllerWithActionsTemplate : Constants.MvcControllerWithActionsTemplate;
        }
    }
}
