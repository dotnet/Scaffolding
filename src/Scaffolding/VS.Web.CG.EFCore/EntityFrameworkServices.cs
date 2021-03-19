// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

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

            if(workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if(fileSystem == null)
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

        public async Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeSymbol, string areaName, bool useSqlite)
        {
            if (string.IsNullOrEmpty(dbContextFullTypeName))
            {
                throw new ArgumentException(nameof(dbContextFullTypeName));
            }

            var processor = new EntityFrameworkModelProcessor(dbContextFullTypeName,
                modelTypeSymbol,
                areaName,
                useSqlite,
                _loader,
                _dbContextEditorServices,
                _modelTypesLocator,
                _workspace,
                _projectContext,
                _applicationInfo,
                _fileSystem,
                _logger);

            await processor.Process();

            return new ContextProcessingResult()
            {
                ContextProcessingStatus = processor.ContextProcessingStatus,
                ModelMetadata = processor.ModelMetadata
            };
        }
    }
}
