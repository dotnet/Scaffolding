// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public abstract class ControllerGeneratorBase : CommonGeneratorBase
    {
        protected ICodeGeneratorActionsService CodeGeneratorActionsService
        {
            get;
            private set;
        }
        protected IProjectContext ProjectContext
        {
            get;
            private set;
        }
        protected IServiceProvider ServiceProvider
        {
            get;
            private set;
        }
        protected ILogger Logger
        {
            get;
            private set;
        }

        public ControllerGeneratorBase(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(applicationInfo)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (codeGeneratorActionsService == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            ProjectContext = projectContext;
            CodeGeneratorActionsService = codeGeneratorActionsService;
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        protected virtual IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationInfo.ApplicationBasePath,
                    baseFolders: new[] { "ControllerGenerator", "ViewGenerator" },
                    projectContext: ProjectContext);
            }
        }

        protected string GetDefaultControllerNamespace(string relativeFolderPath)
        {
            return NameSpaceUtilities.GetSafeNamespaceFromPath(relativeFolderPath, ApplicationInfo.ApplicationName);
        }

        protected void ValidateNameSpaceName(CommandLineGeneratorModel generatorModel)
        {
            if (!string.IsNullOrEmpty(generatorModel.ControllerNamespace) &&
                !RoslynUtilities.IsValidNamespace(generatorModel.ControllerNamespace))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    MessageStrings.InvalidNamespaceName,
                    generatorModel.ControllerNamespace));
            }
        }

        protected string ValidateAndGetOutputPath(CommandLineGeneratorModel generatorModel)
        {
            return ValidateAndGetOutputPath(generatorModel, generatorModel.ControllerName + Constants.CodeFileExtension);
        }

        private bool IsInArea(string appBasePath, string outputPath)
        {
            return outputPath.StartsWith(Path.Combine(appBasePath, "Areas") + Path.DirectorySeparatorChar);
        }

        protected string GetAreaName(string appBasePath, string outputPath)
        {
            if (IsInArea(appBasePath, outputPath))
            {
                var relativePath = outputPath.Substring(Path.Combine(appBasePath, "Areas").Length);
                return relativePath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
            return string.Empty;
        }

        public abstract Task Generate(CommandLineGeneratorModel controllerGeneratorModel);
        protected abstract string GetTemplateName(CommandLineGeneratorModel controllerGeneratorModel);
    }
}
