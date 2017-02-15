// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency
{
    public class ReadMeGenerator
    {
        private readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        private readonly IApplicationInfo _applicationInfo;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IProjectContext _projectContext;

        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[] { "Startup" },
                    _projectContext);
            }
        }

        public ReadMeGenerator(
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IModelTypesLocator modelTypesLocator,
            IProjectContext projectContext,
            IApplicationInfo applicationInfo)
        {
            if (codeGeneratorActionsService == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            _codeGeneratorActionsService = codeGeneratorActionsService;
            _modelTypesLocator = modelTypesLocator;
            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
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

        public async Task GenerateReadmeForArea()
        {
            var templateName = "ReadMe" + Constants.RazorTemplateExtension;
            var outputPath = Path.Combine(_applicationInfo.ApplicationBasePath,
                Constants.ReadMeOutputFileName);

            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath,
                templateName,
                TemplateFolders,
                new ReadMeTemplateModel()
                {
                    StartupList = null,
                    RootNamespace = string.Empty, //Does not matter.
                    IsAreaReadMe = true
                });
        }

        public void GenerateReadMeWithContent(string content)
        {
            var outputPath = Path.Combine(_applicationInfo.ApplicationBasePath,
                Constants.ReadMeOutputFileName);
            try
            {
                File.WriteAllText(outputPath, content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate readme at {outputPath}", ex);
            }
        }

        private async Task GenerateStartup(List<StartupContent> startupList)
        {
            var templateName = "Startup" + Constants.RazorTemplateExtension;
            var applicationName = _applicationInfo.ApplicationName;
            var outputPath = Path.Combine(_applicationInfo.ApplicationBasePath,
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
            var outputPath = Path.Combine(_applicationInfo.ApplicationBasePath,
                Constants.ReadMeOutputFileName);

            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath,
                templateName,
                TemplateFolders,
                new ReadMeTemplateModel()
                {
                    StartupList = startupList,
                    RootNamespace = string.Empty, //Does not matter.
                    IsAreaReadMe = false
                });
        }
    }

    public class ReadMeTemplateModel
    {
        public List<StartupContent> StartupList { get; set; }

        public string RootNamespace { get; set; }

        public bool IsAreaReadMe { get; set; }
    }
}