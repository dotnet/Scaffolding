// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    [Alias("view")]
    public class ViewGenerator : CommonGeneratorBase, ICodeGenerator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public ViewGenerator(
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

        public async Task GenerateCode(ViewGeneratorModel viewGeneratorModel)
        {
            if (viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewGeneratorModel));
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new ArgumentException(MessageStrings.ViewNameRequired);
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.TemplateName))
            {
                throw new ArgumentException(MessageStrings.TemplateNameRequired);
            }

            ViewScaffolderBase scaffolder = null;
            if (string.IsNullOrEmpty(viewGeneratorModel.ModelClass))
            {
                scaffolder = ActivatorUtilities.CreateInstance<EmptyViewScaffolder>(_serviceProvider);
            }
            else
            {
                scaffolder = ActivatorUtilities.CreateInstance<ModelBasedViewScaffolder>(_serviceProvider);
            }

            if (scaffolder != null)
            {
                await scaffolder.GenerateCode(viewGeneratorModel);
            }
        }
    }
}