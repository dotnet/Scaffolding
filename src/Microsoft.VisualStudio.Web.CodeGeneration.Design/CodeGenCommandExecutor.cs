// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class CodeGenCommandExecutor
    {
        private MsBuildProjectContext _projectContext;
        private ProjectDependencyProvider _projectDependencyProvider;
        private string[] _codeGenArguments;
        private string _configuration;
        private ILogger _logger;

        public CodeGenCommandExecutor(ProjectInfoContainer projectInfo, string[] codeGenArguments, string configuration, ILogger logger)
        {
            if (projectInfo == null)
            {
                throw new ArgumentNullException(nameof(projectInfo));
            }
            if (codeGenArguments == null)
            {
                throw new ArgumentNullException(nameof(codeGenArguments));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _projectContext = projectInfo.ProjectContext;
            _projectDependencyProvider = projectInfo.ProjectDependencyProvider;
            _codeGenArguments = codeGenArguments;
            _configuration = configuration;
            _logger = logger;
        }

        public int Execute()
        {
            var serviceProvider = new ServiceProvider();
            AddFrameworkServices(serviceProvider, _projectContext, _projectDependencyProvider);
            AddCodeGenerationServices(serviceProvider);
            var codeGenCommand = serviceProvider.GetService<CodeGenCommand>();
            codeGenCommand.Execute(_codeGenArguments);
            return 0;
        }

        private void AddFrameworkServices(ServiceProvider serviceProvider, MsBuildProjectContext context, ProjectDependencyProvider projectDependencyProvider)
        {
            var applicationInfo = new ApplicationInfo(context.ProjectName, Path.GetDirectoryName(context.ProjectFullPath));
            serviceProvider.Add<MsBuildProjectContext>(context);
            serviceProvider.Add<ProjectDependencyProvider>(projectDependencyProvider);
            serviceProvider.Add<IApplicationInfo>(applicationInfo);
            serviceProvider.Add<ICodeGenAssemblyLoadContext>(new DefaultAssemblyLoadContext());

            serviceProvider.Add<CodeAnalysis.Workspace>(new RoslynWorkspace(context, projectDependencyProvider, context.Configuration));
        }

        private void AddCodeGenerationServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            //Ordering of services is important here
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