// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    /// <summary>
    /// This is create to test the functionality but this should go away perhaps.
    /// </summary>
    [Alias("dependency")]
    public class DependencyGenerator : ICodeGenerator
    {
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;

        public DependencyGenerator([NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]ILogger logger,
            [NotNull]IApplicationEnvironment applicationEnvironment)
        {
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
            _libraryManager = libraryManager;
            _modelTypesLocator = modelTypesLocator;
            _logger = logger;
            _applicationEnvironment = applicationEnvironment;
        }

        public async Task GenerateCode(DependencyGeneratorModel model)
        {
            DependencyInstaller dependencyInstaller = null;

            if (model.AddStaticFiles)
            {
                dependencyInstaller = _typeActivator.CreateInstance<StaticFilesDependencyInstaller>(_serviceProvider);
            }

            if (model.AddMvcLayout)
            {
                dependencyInstaller = _typeActivator.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            }

            if (dependencyInstaller.ShouldInstallDependency())
            {
                dependencyInstaller.Execute();
                await dependencyInstaller.InstallDependencies();
            }
        }
    }
}