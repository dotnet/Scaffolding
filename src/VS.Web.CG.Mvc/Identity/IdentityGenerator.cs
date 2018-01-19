// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    [Alias("identity")]
    public class IdentityGenerator : ICodeGenerator
    {
        private const string IdentityAreaName = "Identity";

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeratorActionService;
        private IProjectContext _projectContext;
        private IConnectionStringsWriter _connectionStringsWriter;

        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[]
                     { 
                         Path.Combine("Identity", "Controllers"),
                         Path.Combine("Identity", "Data"),
                         Path.Combine("Identity", "Extensions"),
                         Path.Combine("Identity", "Services"),
                         Path.Combine("Identity", "Pages"),
                         "Identity"
                     },
                    _projectContext);
            }
        }

        private string _templateFolderRoot;
        private string TemplateFolderRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_templateFolderRoot))
                {
                    _templateFolderRoot = TemplateFoldersUtilities.GetTemplateFolders(
                        Constants.ThisAssemblyName,
                        _applicationInfo.ApplicationBasePath,
                        new [] { "Identity" },
                        _projectContext
                    ).First();
                }
                
                return _templateFolderRoot;
            }
        }

        public IdentityGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            ICodeGeneratorActionsService actionService,
            IProjectContext projectContext,
            IConnectionStringsWriter connectionStringsWriter,
            ILogger logger)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (actionService == null)
            {
                throw new ArgumentNullException(nameof(actionService));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (connectionStringsWriter == null)
            {
                throw new ArgumentNullException(nameof(connectionStringsWriter));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _applicationInfo = applicationInfo;
            _serviceProvider = serviceProvider;
            _codegeratorActionService = actionService;
            _projectContext = projectContext;
            _connectionStringsWriter = connectionStringsWriter;
            _logger = logger;
        }

        public async Task GenerateCode(IdentityGeneratorCommandLineModel commandlineModel)
        {
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            Validate(commandlineModel);
            ValidateEFDependencies(commandlineModel.UseSQLite);

            EnsureFolderLayout(IdentityAreaName, commandlineModel.IsGenerateCustomUser);

            await AddTemplateFiles(commandlineModel);
            await AddStaticFiles();
        }

        private void ValidateEFDependencies(bool useSqlite)
        {
            const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
            var isEFDesignPackagePresent = _projectContext
                .PackageDependencies
                .Any(package => package.Name.Equals(EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            string SqlPackageName = useSqlite 
                ? "Microsoft.EntityFrameworkCore.Sqlite"
                : "Microsoft.EntityFrameworkCore.SqlServer";

            var isSqlServerPackagePresent = _projectContext
                .PackageDependencies
                .Any(package => package.Name.Equals(SqlPackageName, StringComparison.OrdinalIgnoreCase));

            if (!isEFDesignPackagePresent || !isSqlServerPackagePresent)
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallEfPackages, $"{EfDesignPackageName}, {SqlPackageName}"));
            }
        }

        private async Task AddStaticFiles()
        {
            foreach (var staticFile  in IdentityGeneratorFilesConfig.StaticFiles)
            {
                await _codegeratorActionService.AddFileAsync(
                    staticFile.Value,
                    Path.Combine(TemplateFolderRoot, staticFile.Key)
                );
            }
        }

        private void Validate(IdentityGeneratorCommandLineModel model)
        {
            var errorStrings = new List<string>();;
            if (!string.IsNullOrEmpty(model.UserClass) && !RoslynUtilities.IsValidIdentifier(model.UserClass))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidUserClassName, model.UserClass));;
            }

            if (!string.IsNullOrEmpty(model.DbContext) && !RoslynUtilities.IsValidIdentifier(model.DbContext))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidDbContextClassName, model.DbContext));;
            }

            if (errorStrings.Any())
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorCommandLineModel commandLineModel)
        {
            var rootNamespace = string.IsNullOrEmpty(commandLineModel.RootNamespace) ? _projectContext.RootNamespace:
                    commandLineModel.RootNamespace;

            var model = new IdentityGeneratorTemplateModel()
            {
                Namespace = rootNamespace,
                DbContextNamespace = rootNamespace+".Areas.Identity.Data",
                ApplicationName = _applicationInfo.ApplicationName,
                UserClass = commandLineModel.UserClass,
                DbContextClass = GetDbContextClassName(commandLineModel.DbContext),
                UseSQLite = commandLineModel.UseSQLite
            };

            var templates = IdentityGeneratorFilesConfig.GetTemplateFiles(model.UserClass, model.DbContextClass);

            foreach (var template in templates)
            {
                await _codegeratorActionService.AddFileFromTemplateAsync(
                    template.Value,
                    template.Key,
                    TemplateFolders,
                    model);
            }

            var dbContextClass = model.IsGenerateCustomUser ? model.DbContextClass : "IdentityDbContext";
            _connectionStringsWriter.AddConnectionString(
                connectionStringName: $"{dbContextClass}Connection",
                dataBaseName: $"{model.ApplicationName}",
                useSQLite: commandLineModel.UseSQLite);
        }

        // We always want to generate a DbContext. In case the user does not provide a name, 
        // we try to generate one by concatenating Application name and "IdentityDbContext"
        // If the result is not a valid class name, then we just use IdentityDbContext.
        private string GetDbContextClassName(string dbContext)
        {
            if (!string.IsNullOrEmpty(dbContext))
            {
                return dbContext;
            }

            var defaultDbContextName = $"{_applicationInfo.ApplicationName}IdentityDbContext";

            if (!RoslynUtilities.IsValidIdentifier(defaultDbContextName))
            {
                defaultDbContextName = "IdentityDbContext";
            }

            return defaultDbContextName;
        }

        /// <summary>
        /// Creates a folder hierarchy:
        ///     ProjectDir
        ///        \ Areas
        ///            \ IdentityAreaName
        ///                \ Data
        ///                \ Pages
        ///                \ Services
        /// </summary>
        private void EnsureFolderLayout(string IdentityareaName, bool isGenerateCustomUser)
        {
            var areaBasePath = Path.Combine(_applicationInfo.ApplicationBasePath, "Areas");
            if (!Directory.Exists(areaBasePath))
            {
                Directory.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, IdentityareaName);
            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            foreach (var areaFolder in IdentityGeneratorFilesConfig.AreaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
    }
}
