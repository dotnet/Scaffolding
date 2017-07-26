// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    [Alias("razorpage")]
    public class RazorPageGenerator : CommonGeneratorBase, ICodeGenerator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public RazorPageGenerator(
            IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(applicationInfo)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task GenerateCode(RazorPageGeneratorModel razorPageGeneratorModel)
        {
            if (razorPageGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(razorPageGeneratorModel));
            }

            if (string.IsNullOrEmpty(razorPageGeneratorModel.ModelClass))
            {
                if (string.IsNullOrEmpty(razorPageGeneratorModel.ViewName))
                {
                    throw new ArgumentException(MessageStrings.ViewNameRequired);
                }

                if (string.IsNullOrEmpty(razorPageGeneratorModel.TemplateName))
                {
                    throw new ArgumentException(MessageStrings.TemplateNameRequired);
                }

                RazorPageScaffolderBase scaffolder = ActivatorUtilities.CreateInstance<EmptyRazorPageScaffolder>(_serviceProvider);
                await scaffolder.GenerateCode(razorPageGeneratorModel);
            }
            else
            {
                EFModelBasedRazorPageScaffolder scaffolder = ActivatorUtilities.CreateInstance<EFModelBasedRazorPageScaffolder>(_serviceProvider);

                if (!string.IsNullOrEmpty(razorPageGeneratorModel.TemplateName) && !string.IsNullOrEmpty(razorPageGeneratorModel.ViewName))
                {   // Razor page using EF
                    await scaffolder.GenerateCode(razorPageGeneratorModel);
                }
                else
                {   // Razor page CRUD using EF
                    await scaffolder.GenerateViews(razorPageGeneratorModel);
                }
            }
        }
    }
}
