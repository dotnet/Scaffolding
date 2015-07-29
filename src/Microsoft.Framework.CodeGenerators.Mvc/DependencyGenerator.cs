// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    /// <summary>
    /// This is create to test the functionality but this should go away perhaps.
    /// For testing using this just make this class implement ICodeGenerator interface.
    /// Eventually this class should just be removed.
    /// </summary>
    [Alias("dependency")]
    public class DependencyGenerator
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyGenerator(
            [NotNull]IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task GenerateCode(DependencyGeneratorModel model)
        {
            DependencyInstaller dependencyInstaller = null;

            if (model.AddStaticFiles)
            {
                dependencyInstaller = ActivatorUtilities.CreateInstance<StaticFilesDependencyInstaller>(_serviceProvider);
            }

            if (model.AddMvcLayout)
            {
                dependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            }

            await dependencyInstaller.Execute();
            await dependencyInstaller.InstallDependencies();
        }
    }
}