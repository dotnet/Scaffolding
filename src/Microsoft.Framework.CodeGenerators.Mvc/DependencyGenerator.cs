// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.PackageManager;
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
            IEnumerable<Dependency> dependencies = Enumerable.Empty<Dependency>();

            if (model.AddStaticFiles)
            {
                dependencies = RunDepdencyInstaller<StaticFilesDependencyInstaller>();
            }

            if (model.AddMvcLayout)
            {
                dependencies = RunDepdencyInstaller<MvcLayoutDependencyInstaller>();
            }

            var missingDependencies = dependencies.Where(dep => _libraryManager.GetLibraryInformation(dep.Name) == null);
            if (missingDependencies.Any())
            {
                var readMeGenerator = _typeActivator.CreateInstance<ReadMeGenerator>(_serviceProvider);

                var isReadMe = await readMeGenerator.GenerateStartupOrReadme(missingDependencies
                    .Select(md => md.StartupConfiguration)
                    .ToList());

                if (isReadMe)
                {
                    _logger.LogMessage("There are probably still some manual steps required");
                    _logger.LogMessage("Checkout the " + Constants.ReadMeOutputFileName + " file that got generated");
                }

                foreach (var missingDependency in missingDependencies)
                {
                    AddCommand addComand = new AddCommand()
                    {
                        Name = missingDependency.Name,
                        Version = missingDependency.Version,
                        ProjectDir = _applicationEnvironment.ApplicationBasePath,
                    };

                    addComand.ExecuteCommand();
                }

                //try
                //{
                //    RestoreCommand restore = new RestoreCommand(_applicationEnvironment);
                //    restore.RestoreDirectory = _applicationEnvironment.ApplicationBasePath;
                //    restore.ExecuteCommand();
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogMessage("Error from Restore");
                //    _logger.LogMessage(ex.ToString());
                //}
            }
        }

        private IEnumerable<Dependency> RunDepdencyInstaller<T>() where T : DependencyInstaller
        {
            var dependencyInstaller = _typeActivator.CreateInstance<T>(_serviceProvider);
            dependencyInstaller.Execute();
            return dependencyInstaller.Dependencies;
        }
    }
}