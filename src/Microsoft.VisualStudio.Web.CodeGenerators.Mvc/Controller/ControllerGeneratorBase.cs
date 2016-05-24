// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public abstract class ControllerGeneratorBase : CommonGeneratorBase
    {
        protected ICodeGeneratorActionsService CodeGeneratorActionsService
        {
            get;
            private set;
        }
        protected ILibraryManager LibraryManager
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
            ILibraryManager libraryManager,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(applicationInfo)
        {
            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
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

            LibraryManager = libraryManager;
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
                    libraryManager: LibraryManager);
            }
        }

        protected string GetDefaultControllerNamespace(string relativeFolderPath)
        {
            return NameSpaceUtilities.GetSafeNameSpaceFromPath(relativeFolderPath, ApplicationInfo.ApplicationName);
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

        public abstract Task Generate(CommandLineGeneratorModel controllerGeneratorModel);
        protected abstract string GetTemplateName(CommandLineGeneratorModel controllerGeneratorModel);
    }
}