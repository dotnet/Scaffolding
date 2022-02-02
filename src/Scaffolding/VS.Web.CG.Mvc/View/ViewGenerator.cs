// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
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

        internal static readonly string DefaultBootstrapVersion = "5";
        // A hashset would allow faster lookups, but would cause a perf hit when formatting the error string for invalid bootstrap version.
        // Also, with a list this small, the lookup perf hit will be largely irrelevant.
        internal static readonly IReadOnlyList<string> ValidBootstrapVersions = new List<string>()
        {
            "3",
            "4",
            "5"
        };

        internal static readonly string ContentVersionDefault = "Default";
        internal static readonly string ContentVersionBootstrap3 = "Bootstrap3";
        internal static readonly string ContentVersionBootstrap4 = "Bootstrap4";

        internal static readonly string DefaultContentRelativeBaseDir = "ViewGenerator";
        internal static readonly string VersionedContentRelativeBaseDir = "ViewGenerator_Versioned";

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
            var viewTemplate = ValidateViewGeneratorModel(viewGeneratorModel);

            ViewScaffolderBase scaffolder = null;
            if (viewTemplate.Name == ViewTemplate.EmptyViewTemplate.Name)
            {
                scaffolder = ActivatorUtilities.CreateInstance<EmptyViewScaffolder>(_serviceProvider);
            }
            else if (string.IsNullOrEmpty(viewGeneratorModel.DataContextClass))
            {
                scaffolder = ActivatorUtilities.CreateInstance<ModelBasedViewScaffolder>(_serviceProvider);
            }
            else
            {
                scaffolder = ActivatorUtilities.CreateInstance<EFModelBasedViewScaffolder>(_serviceProvider);
            }

            if (scaffolder != null)
            {
                await scaffolder.GenerateCode(viewGeneratorModel);
            }
        }

        private static ViewTemplate ValidateViewGeneratorModel(ViewGeneratorModel viewGeneratorModel)
        {
            if (viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewGeneratorModel));
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new ArgumentException(MessageStrings.ViewNameRequired);
            }

            var templateName = viewGeneratorModel.TemplateName;
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentException(MessageStrings.TemplateNameRequired);
            }

            ViewTemplate viewTemplate;
            if (!ViewTemplate.ViewTemplateNames.TryGetValue(templateName, out viewTemplate))
            {
                throw new InvalidOperationException(string.Format(MessageStrings.InvalidViewTemplateName, templateName));
            }

            if (viewTemplate.IsModelRequired && string.IsNullOrEmpty(viewGeneratorModel.ModelClass))
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelClassRequiredForTemplate, templateName));
            }

            if (!string.IsNullOrEmpty(viewGeneratorModel.BootstrapVersion) && !ValidBootstrapVersions.Contains(viewGeneratorModel.BootstrapVersion.Trim(' ', '\n')))
            {
                throw new InvalidOperationException(string.Format(MessageStrings.InvalidBootstrapVersionForScaffolding, viewGeneratorModel.BootstrapVersion, string.Join(", ", ValidBootstrapVersions)));
            }

            return viewTemplate;
        }
    }
}
