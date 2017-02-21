// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class CodeGenCommandExecutor
    {
        private readonly IProjectContext _projectInformation;
        private string[] _codeGenArguments;
        private string _configuration;
        private ILogger _logger;
        private bool _isSimulationMode;

        public CodeGenCommandExecutor(IProjectContext projectInformation, string[] codeGenArguments, string configuration, ILogger logger, bool isSimulationMode)
        {
            if (projectInformation == null)
            {
                throw new ArgumentNullException(nameof(projectInformation));
            }
            if (codeGenArguments == null)
            {
                throw new ArgumentNullException(nameof(codeGenArguments));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _projectInformation = projectInformation;
            _codeGenArguments = codeGenArguments;
            _configuration = configuration;
            _logger = logger;
            _isSimulationMode = isSimulationMode;
        }

        public int Execute(Action<IEnumerable<FileSystemChangeInformation>> simModeAction = null)
        {
            var serviceProvider = new ServiceProvider();
            AddFrameworkServices(serviceProvider, _projectInformation);
            AddCodeGenerationServices(serviceProvider);
            var codeGenCommand = serviceProvider.GetService<CodeGenCommand>();
            codeGenCommand.Execute(_codeGenArguments);

            if (_isSimulationMode && simModeAction != null)
            {
                simModeAction.Invoke(SimulationModeFileSystem.Instance.FileSystemChangeTracker.Changes);
            }

            return 0;
        }

        private void AddFrameworkServices(ServiceProvider serviceProvider, IProjectContext projectInformation)
        {
            var applicationInfo = new ApplicationInfo(
                projectInformation.ProjectName,
                Path.GetDirectoryName(projectInformation.ProjectFullPath));
            serviceProvider.Add<IProjectContext>(projectInformation);
            serviceProvider.Add<IApplicationInfo>(applicationInfo);
            serviceProvider.Add<ICodeGenAssemblyLoadContext>(new DefaultAssemblyLoadContext());

            serviceProvider.Add<CodeAnalysis.Workspace>(new RoslynWorkspace(projectInformation, projectInformation.Configuration));
        }

        private void AddCodeGenerationServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            IFileSystem fileSystem = _isSimulationMode
                ? (IFileSystem)SimulationModeFileSystem.Instance
                : (IFileSystem)DefaultFileSystem.Instance;
            //Ordering of services is important here
            serviceProvider.Add(typeof(IFileSystem), fileSystem);
            serviceProvider.Add(typeof(ILogger), _logger);
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();
            serviceProvider.AddServiceWithDependencies<CodeGenCommand, CodeGenCommand>();

            serviceProvider.AddServiceWithDependencies<ICompilationService, RoslynCompilationService>();
            serviceProvider.AddServiceWithDependencies<ITemplating, RazorTemplating>();

            serviceProvider.AddServiceWithDependencies<IPackageInstaller, PackageInstaller>();

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();

            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}