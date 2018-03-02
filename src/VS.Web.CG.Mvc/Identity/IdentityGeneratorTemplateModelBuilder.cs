// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    internal class IdentityGeneratorTemplateModelBuilder
    {
        private IdentityGeneratorCommandLineModel _commandlineModel;
        private IApplicationInfo _applicationInfo;
        private IProjectContext _projectContext;
        private Workspace _workspace;
        private ICodeGenAssemblyLoadContext _loader;
        private IFileSystem _fileSystem;
        private ILogger _logger;

        public IdentityGeneratorTemplateModelBuilder(
            IdentityGeneratorCommandLineModel commandlineModel,
            IApplicationInfo applicationInfo,
            IProjectContext projectContext,
            Workspace workspace,
            ICodeGenAssemblyLoadContext loader,
            IFileSystem fileSystem,
            ILogger logger)
        {
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _commandlineModel = commandlineModel;
            _applicationInfo = applicationInfo;
            _projectContext = projectContext;
            _workspace = workspace;
            _loader = loader;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        internal bool IsFilesSpecified => !string.IsNullOrEmpty(_commandlineModel.Files);
        internal bool IsDbContextSpecified => !string.IsNullOrEmpty(_commandlineModel.DbContext);
        internal bool IsUsingExistingDbContext { get; set; }

        private Type _userType;

        internal string UserClass { get; private set; }
        internal string UserClassNamespace { get; private set; }

        internal Type UserType 
        {
            get
            {
                return _userType;
            }
            set
            {
                _userType = value;
                UserClass = _userType?.Name;
                UserClassNamespace = _userType?.Namespace;
            }
        }

        internal string DbContextClass { get; private set; }
        internal string DbContextNamespace { get; private set; }
        internal string RootNamespace { get; private set; }
        internal bool IsGenerateCustomUser { get; private set; }
        internal IdentityGeneratorFile[] FilesToGenerate { get; private set; }
        internal IEnumerable<string> NamedFiles { get; private set; }

        public async Task<IdentityGeneratorTemplateModel> ValidateAndBuild()
        {
            ValidateCommandLine(_commandlineModel);
            RootNamespace = string.IsNullOrEmpty(_commandlineModel.RootNamespace)
                ? _projectContext.RootNamespace
                : _commandlineModel.RootNamespace;

            var defaultDbContextNamespace = $"{RootNamespace}.Areas.Identity.Data";

            IsUsingExistingDbContext = false;
            if (IsDbContextSpecified)
            {
                var existingDbContext = await FindExistingType(_commandlineModel.DbContext);
                if (existingDbContext == null)
                {
                    // We need to create one with what the user specified.
                    DbContextClass = GetClassNameFromTypeName(_commandlineModel.DbContext);
                    DbContextNamespace = GetNamespaceFromTypeName(_commandlineModel.DbContext)
                        ?? defaultDbContextNamespace;
                }
                else
                {
                    ValidateExistingDbContext(existingDbContext);
                    IsGenerateCustomUser = false;
                    IsUsingExistingDbContext = true;
                    UserType = FindUserTypeFromDbContext(existingDbContext);
                    DbContextClass = existingDbContext.Name;
                    DbContextNamespace = existingDbContext.Namespace;
                }
            }
            else
            {
                // --dbContext paramter was not specified. So we need to generate one using convention.
                DbContextClass = GetDefaultDbContextName();
                DbContextNamespace = defaultDbContextNamespace;
            }

            if (string.IsNullOrEmpty(_commandlineModel.UserClass))
            {
                IsGenerateCustomUser = false;
                UserClass = "IdentityUser";
                UserClassNamespace = "Microsoft.AspNetCore.Identity";
            }
            else
            {
                IsGenerateCustomUser = true;
                UserClass = _commandlineModel.UserClass;
                UserClassNamespace = defaultDbContextNamespace;
            }

            if (_commandlineModel.UseDefaultUI)
            {
                ValidateDefaultUIOption();
            }

            if (IsFilesSpecified)
            {
                await ValidateFilesOption();
            }

            var templateModel = new IdentityGeneratorTemplateModel()
            {
                ApplicationName = _applicationInfo.ApplicationName,
                DbContextClass = DbContextClass,
                DbContextNamespace = DbContextNamespace,
                UserClass = UserClass,
                UserClassNamespace = UserClassNamespace,
                UseSQLite = _commandlineModel.UseSQLite,
                IsUsingExistingDbContext = IsUsingExistingDbContext,
                Namespace = RootNamespace,
                IsGenerateCustomUser = IsGenerateCustomUser,
                IsGeneratingIndividualFiles = IsFilesSpecified,
                UseDefaultUI = _commandlineModel.UseDefaultUI
            };

            var filesToGenerate = new List<IdentityGeneratorFile>(IdentityGeneratorFilesConfig.GetFilesToGenerate(NamedFiles, templateModel));

            // Check if we need to add ViewImports and which ones.
            if (!_commandlineModel.UseDefaultUI)
            {
                filesToGenerate.AddRange(IdentityGeneratorFilesConfig.GetViewImports(filesToGenerate, _fileSystem, _applicationInfo.ApplicationBasePath));
            }

            templateModel.FilesToGenerate = filesToGenerate.ToArray();

            ValidateFilesToGenerate(templateModel.FilesToGenerate);

            return templateModel;
        }

        private void ValidateFilesToGenerate(IdentityGeneratorFile[] filesToGenerate)
        {
            var rootPath = _applicationInfo.ApplicationBasePath;
            var filesToOverWrite = filesToGenerate
                .Where(f => f.ShouldOverWrite == OverWriteCondition.WithForce
                                && _fileSystem.FileExists(Path.Combine(rootPath, f.OutputPath)));

            if (filesToOverWrite.Any() && !_commandlineModel.Force)
            {
                var msg = string.Format(
                        MessageStrings.UseForceOption,
                        string.Join(Environment.NewLine, filesToOverWrite.Select(f => f.OutputPath)));
                throw new InvalidOperationException(msg);
            }
        }

        private void ValidateDefaultUIOption()
        {
            var errorStrings = new List<string>();

            if (IsFilesSpecified)
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidOptionCombination,"--files", "--useDefaultUI"));
            }

            if (IsUsingExistingDbContext)
            {
                errorStrings.Add(MessageStrings.ExistingDbContextCannotBeUsedForDefaultUI);
            }

            if (errorStrings.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        private async Task ValidateFilesOption()
        {
            var errors = new List<string>();
            if (!IsUsingExistingDbContext && IsDbContextSpecified)
            {
                // We need the user to specify that there is an existing DbContext that inherits from IdentityDbContext.
                errors.Add("Using '--files' option requires specifying an existing DbContext which inherits from IdentityDbContext.");
            }
            else if (!IsDbContextSpecified)
            {
                // Try to find an existing DbContext in the project (not in the dependencies) that inherits from IdentityDbContext.
                // If 0 or more than 1 are found, ask the user to specify.
                // If exactly one is found, use that.

                var compilation = await _workspace.CurrentSolution.Projects
                    .Where(p => p.AssemblyName == _projectContext.AssemblyName)
                    .First()
                    .GetCompilationAsync();

                var reflectedTypesProvider = new ReflectedTypesProvider(
                    compilation,
                    null,
                    _projectContext,
                    _loader,
                    _logger);

                if (reflectedTypesProvider.GetCompilationErrors()!= null
                    && reflectedTypesProvider.GetCompilationErrors().Any())
                {
                    // Failed to build the project.
                    throw new InvalidOperationException(
                        string.Format("Failed to compile the project in memory{0}{1}",
                            Environment.NewLine,
                            string.Join(Environment.NewLine, reflectedTypesProvider.GetCompilationErrors())));
                }

                var reflectedTypes = reflectedTypesProvider.GetAllTypesInProject();

                var candidateDbContexts = new List<Type>();
                foreach (var reflectedType in reflectedTypes)
                {
                    if (IsTypeDerivedFromIdentityDbContext(reflectedType))
                    {
                        candidateDbContexts.Add(reflectedType);
                    }
                }

                if (!candidateDbContexts.Any())
                {
                    errors.Add("No valid DbContext found in the project to use.");
                    errors.Add("Please specify an existing DbContext which inherits from IdentityDbContext using the '--dbContext' option.");
                    errors.Add("To add a new DbContext first scaffold using '--useDefaultUI' option.");
                }

                if (candidateDbContexts.Count > 1)
                {
                    errors.Add("Found more than 1 DbContexts. Please specify one from below using the '--dbContext' option");
                    errors.AddRange(candidateDbContexts.Select(c => $"{c.Namespace}.{c.Name}"));
                }
            }


            NamedFiles = _commandlineModel.Files.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var invalidFiles = NamedFiles.Where(f => !IdentityGeneratorFilesConfig.GetFilesToList().Contains(f));

            if (invalidFiles.Any())
            {
                errors.Add("Could not find the files below. (Please use '--listFiles' to check the list of available files)");
                errors.AddRange(invalidFiles);
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        private string GetDefaultDbContextName()
        {
            var defaultDbContextName = $"{_applicationInfo.ApplicationName}IdentityDbContext";

            if (!RoslynUtilities.IsValidIdentifier(defaultDbContextName))
            {
                defaultDbContextName = "IdentityDbContext";
            }

            return defaultDbContextName;
        }

        private string GetNamespaceFromTypeName(string dbContext)
        {
            if (dbContext.LastIndexOf('.') == -1)
            {
                return null;
            }

            return dbContext.Substring(0, dbContext.LastIndexOf('.'));
        }

        private string GetClassNameFromTypeName(string dbContext)
        {
            return dbContext.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        private void ValidateExistingDbContext(Type existingDbContext)
        {
            var errorStrings = new List<string>();

            // Validate that the dbContext inherits from IdentityDbContext.
            bool foundValidParentDbContextClass = IsTypeDerivedFromIdentityDbContext(existingDbContext);

            if (!foundValidParentDbContextClass)
            {
                errorStrings.Add(
                    string.Format("DbContext type '{0}' is found but it does not inherit from '{1}'",
                        existingDbContext.Name,
                        "Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext"));
            }

            // Validate that the `--userClass` parameter is not passed.
            if (!string.IsNullOrEmpty(_commandlineModel.UserClass))
            {
                errorStrings.Add("'--userClass' cannot be used to specify a user class when using an existing DbContext.");
            }

            if (errorStrings.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }

        }

        private static bool IsTypeDerivedFromIdentityDbContext(Type type)
        {
            var parentType = type.BaseType;
            var foundValidParentDbContextClass = false;
            while (parentType != null && parentType != typeof(object))
            {
                // There are multiple variations of IdentityDbContext classes.
                // So have to use StartsWith instead of comparing names.
                // 1. IdentityDbContext
                // 2. IdentityDbContext <TUser, TRole, TKey>
                // 3. IdentityDbContext <TUser, TRole, string> 
                // 4. IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> etc.
                if (parentType.Name.StartsWith("IdentityDbContext")
                    && parentType.Namespace == "Microsoft.AspNetCore.Identity.EntityFrameworkCore"
                    && parentType.Assembly.GetName().Name == "Microsoft.AspNetCore.Identity.EntityFrameworkCore")
                {
                    foundValidParentDbContextClass = true;
                    break;
                }

                parentType = parentType.BaseType;
            }

            return foundValidParentDbContextClass;
        }

        private Type FindUserTypeFromDbContext(Type existingDbContext)
        {
            var usersProperty = existingDbContext.GetProperties()
                .FirstOrDefault(p => p.Name == "Users");

            if (usersProperty == null 
                || !usersProperty.PropertyType.IsGenericType
                || usersProperty.PropertyType.GetGenericArguments().Count() != 1)
            {
                // The IdentityDbContext has DbSet<UserType> Users property.
                // The only case this would happen is if the user hides the inherited property.
                throw new InvalidOperationException(
                    string.Format("Could not determine the user class from the DbContext class '{0}'",
                        existingDbContext.Name));
            }

            return usersProperty.PropertyType.GetGenericArguments().First();
        }

        private async Task<Type> FindExistingType(string dbContext)
        {
            var compilation = await _workspace.CurrentSolution.Projects
                .Where(p => p.AssemblyName == _projectContext.AssemblyName)
                .First()
                .GetCompilationAsync();

            var reflectedTypesProvider = new ReflectedTypesProvider(
                compilation,
                null,
                _projectContext,
                _loader,
                _logger);

            if (reflectedTypesProvider.GetCompilationErrors()!= null
                && reflectedTypesProvider.GetCompilationErrors().Any())
            {
                // Failed to build the project.
                throw new InvalidOperationException(
                    string.Format("Failed to compile the project in memory{0}{1}",
                        Environment.NewLine,
                        string.Join(Environment.NewLine, reflectedTypesProvider.GetCompilationErrors())));
            }

            var dbContextType = reflectedTypesProvider.GetReflectedType(dbContext, true);

            return dbContextType;
        }

        private void ValidateCommandLine(IdentityGeneratorCommandLineModel model)
        {
            var errorStrings = new List<string>();;
            if (!string.IsNullOrEmpty(model.UserClass) && !RoslynUtilities.IsValidIdentifier(model.UserClass))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidUserClassName, model.UserClass));;
            }

            if (!string.IsNullOrEmpty(model.DbContext) && !RoslynUtilities.IsValidNamespace(model.DbContext))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidDbContextClassName, model.DbContext));;
            }

            if (!string.IsNullOrEmpty(model.RootNamespace) && !RoslynUtilities.IsValidNamespace(model.RootNamespace))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidNamespaceName, model.RootNamespace));
            }

            if (errorStrings.Any())
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errorStrings));
            }
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
    }
}