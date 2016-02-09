// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Dependency
{
    public class ReadMeGenerator
    {
        private readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        private readonly IApplicationEnvironment _environment;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly ILibraryManager _libraryManager;

        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _environment.ApplicationBasePath,
                    new[] { "Startup" },
                    _libraryManager);
            }
        }

        public ReadMeGenerator(
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IModelTypesLocator modelTypesLocator,
            ILibraryManager libraryManager,
            IApplicationEnvironment environment)
        {
            if (codeGeneratorActionsService == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _codeGeneratorActionsService = codeGeneratorActionsService;
            _modelTypesLocator = modelTypesLocator;
            _libraryManager = libraryManager;
            _environment = environment;
        }

        public async Task<bool> GenerateStartupOrReadme(List<StartupContent> startupList)
        {
            var startupTypes = _modelTypesLocator.GetType(Constants.StartupClassName);

            if (startupTypes.Any())
            {
                await GenerateReadMe(startupList);
                return true;
            }
            else
            {
                await GenerateStartup(startupList);
                return false;
            }
        }

        private async Task GenerateStartup(List<StartupContent> startupList)
        {
            var templateName = "Startup" + Constants.RazorTemplateExtension;
            var applicationName = _environment.ApplicationName;
            var outputPath = Path.Combine(_environment.ApplicationBasePath,
                "Startup" + Constants.CodeFileExtension);

            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath,
                templateName,
                TemplateFolders,
                new ReadMeTemplateModel()
                {
                    StartupList = startupList,
                    RootNamespace = applicationName
                });
        }

        private async Task GenerateReadMe(List<StartupContent> startupList)
        {
            var templateName = "ReadMe" + Constants.RazorTemplateExtension;
            var outputPath = Path.Combine(_environment.ApplicationBasePath,
                Constants.ReadMeOutputFileName);

            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath,
                templateName,
                TemplateFolders,
                new ReadMeTemplateModel()
                {
                    StartupList = startupList,
                    RootNamespace = string.Empty //Does not matter.
                });
        }
    }

    public class ReadMeTemplateModel
    {
        public List<StartupContent> StartupList { get; set; }

        public string RootNamespace { get; set; }
    }
}