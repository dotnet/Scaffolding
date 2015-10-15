// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
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
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(environment)
        {
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
                    applicationBasePath: ApplicationEnvironment.ApplicationBasePath,
                    baseFolders: new[] { "ControllerGenerator", "ViewGenerator" },
                    libraryManager: LibraryManager);
            }
        }

        protected string GetControllerNamespace()
        {
            // Review: MVC scaffolding used ActiveProject's MSBuild RootNamespace property
            // That's not possible in command line scaffolding - the closest we can get is
            // the name of assembly??
            var appName = LibraryManager.GetLibrary(ApplicationEnvironment.ApplicationName).Name;
            return appName + "." + Constants.ControllersFolderName;
        }

        protected string ValidateAndGetOutputPath(CommandLineGeneratorModel generatorModel)
        {
            return ValidateAndGetOutputPath(generatorModel, generatorModel.ControllerName + Constants.CodeFileExtension);
        }

        public abstract Task Generate(CommandLineGeneratorModel controllerGeneratorModel);
    }
}