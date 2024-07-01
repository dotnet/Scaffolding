// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class EntityFrameworkServices : IEntityFrameworkService
    {
        private readonly IDbContextEditorServices _dbContextEditorServices;
        private readonly IApplicationInfo _applicationInfo;
        private readonly ICodeGenAssemblyLoadContext _loader;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IPackageInstaller _packageInstaller;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private const string EFSqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string NewDbContextFolderName = "Data";
        private readonly Workspace _workspace;
        private readonly IProjectContext _projectContext;
        private readonly IFileSystem _fileSystem;


        public EntityFrameworkServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGenAssemblyLoadContext loader,
            IModelTypesLocator modelTypesLocator,
            IDbContextEditorServices dbContextEditorServices,
            IPackageInstaller packageInstaller,
            IServiceProvider serviceProvider,
            Workspace workspace,
            IFileSystem fileSystem,
            ILogger logger)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (dbContextEditorServices == null)
            {
                throw new ArgumentNullException(nameof(dbContextEditorServices));
            }

            if (packageInstaller == null)
            {
                throw new ArgumentNullException(nameof(packageInstaller));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
            _loader = loader;
            _modelTypesLocator = modelTypesLocator;
            _dbContextEditorServices = dbContextEditorServices;
            _packageInstaller = packageInstaller;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workspace = workspace;
            _fileSystem = fileSystem;
        }

        public async Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeSymbol, string areaName, DbProvider databaseProvider, bool useDbFactory = false)
        {
            if (string.IsNullOrEmpty(dbContextFullTypeName))
            {
                throw new ArgumentException(nameof(dbContextFullTypeName));
            }

            var processor = new EntityFrameworkModelProcessor(dbContextFullTypeName,
                modelTypeSymbol,
                areaName,
                databaseProvider,
                _loader,
                _dbContextEditorServices,
                _modelTypesLocator,
                _workspace,
                _projectContext,
                _applicationInfo,
                _fileSystem,
                _logger,
                useDbFactory);

            await processor.Process();

            return new ContextProcessingResult()
            {
                ContextProcessingStatus = processor.ContextProcessingStatus,
                ModelMetadata = processor.ModelMetadata
            };
        }
    }
}
