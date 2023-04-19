// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

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
                if (string.IsNullOrEmpty(razorPageGeneratorModel.RazorPageName))
                {
                    throw new ArgumentException(MessageStrings.RazorPageNameRequired);
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

                if (!string.IsNullOrEmpty(razorPageGeneratorModel.TemplateName) && !string.IsNullOrEmpty(razorPageGeneratorModel.RazorPageName))
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
