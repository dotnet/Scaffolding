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
    [Alias("razor")]
    public class RazorPageGenerator : CommonGeneratorBase, ICodeGenerator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        // When the template name is blank, generate each of these.
        private static readonly IReadOnlyList<string> TemplateNamesForAllGeneration = new List<string>()
        {
            "Create",
            "Edit",
            "Details",
            "Delete",
            "List"
        };

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

        public async Task GenerateCode(RazorPageGeneratorModel razorGeneratorModel)
        {
            if (razorGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(razorGeneratorModel));
            }

            if (string.IsNullOrEmpty(razorGeneratorModel.ViewName))
            {
                // TODO: make a separate message resource string for this (currently setup using the one for VIEW)
                throw new ArgumentException(MessageStrings.ViewNameRequired);
            }

            // TODO: Determine if these checks are appropriate for when to use which scaffolder.
            //
            RazorPageScaffolderBase scaffolder = null;
            if (string.IsNullOrEmpty(razorGeneratorModel.ModelClass) && string.IsNullOrEmpty(razorGeneratorModel.DataContextClass))
            {
                scaffolder = ActivatorUtilities.CreateInstance<EmptyRazorPageScaffolder>(_serviceProvider);
            }
            else
            {
                scaffolder = ActivatorUtilities.CreateInstance<EFModelBasedRazorPageScaffolder>(_serviceProvider);
            }

            if (scaffolder != null)
            {
                if (string.IsNullOrEmpty(razorGeneratorModel.TemplateName))
                {
                    foreach (string templateName in TemplateNamesForAllGeneration)
                    {
                        RazorPageGeneratorModel modelForTemplate = (RazorPageGeneratorModel)razorGeneratorModel.Clone();
                        modelForTemplate.TemplateName = templateName;
                        modelForTemplate.ViewName = razorGeneratorModel.ViewName + templateName;
                        await scaffolder.GenerateCode(modelForTemplate);
                    }
                }
                else
                {
                    await scaffolder.GenerateCode(razorGeneratorModel);
                }
            }
        }
    }
}
