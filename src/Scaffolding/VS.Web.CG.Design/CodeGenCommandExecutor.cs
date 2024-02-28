// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.DependencyInjection;
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
            _projectInformation = projectInformation ?? throw new ArgumentNullException(nameof(projectInformation));
            _codeGenArguments = codeGenArguments ?? throw new ArgumentNullException(nameof(codeGenArguments));
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isSimulationMode = isSimulationMode;
        }

        public int Execute(Action<IEnumerable<FileSystemChangeInformation>> simModeAction = null)
        {
            var serviceProvider = new ServiceProvider();
            AddFrameworkServices(serviceProvider, _projectInformation);
            AddCodeGenerationServices(serviceProvider);
            var codeGenCommand = serviceProvider.GetService<CodeGenCommand>();

            try
            {
                codeGenCommand.Execute(_codeGenArguments);
            }
            catch (InvalidOperationException ioe)
            {
                _logger.LogMessage(ioe.Message, LogMessageType.Error);
                _logger.LogMessage(ioe.StackTrace, LogMessageType.Trace);
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

            IFileSystem fileSystem = new FileSystem();
            //Ordering of services is important here
            serviceProvider.Add(typeof(IFileSystem), fileSystem);
            serviceProvider.Add(typeof(ILogger), _logger);
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<IConnectionStringsWriter, ConnectionStringsWriter>();
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
            serviceProvider.AddServiceWithDependencies<ICodeModelService, CodeModelService>();
        }
    }
}
