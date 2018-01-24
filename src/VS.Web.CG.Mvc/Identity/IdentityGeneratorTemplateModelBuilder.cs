// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private ILogger _logger;

        public IdentityGeneratorTemplateModelBuilder(
            IdentityGeneratorCommandLineModel commandlineModel,
            IApplicationInfo applicationInfo,
            IProjectContext projectContext,
            Workspace workspace,
            ICodeGenAssemblyLoadContext loader,
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

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _commandlineModel = commandlineModel;
            _applicationInfo = applicationInfo;
            _projectContext = projectContext;
            _workspace = workspace;
            _loader = loader;
            _logger = logger;
        }

        internal bool IsDbContextSpecified => !string.IsNullOrEmpty(_commandlineModel.DbContext);

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

        public async Task<IdentityGeneratorTemplateModel> ValidateAndBuild()
        {
            ValidateCommandLine(_commandlineModel);
            RootNamespace = string.IsNullOrEmpty(_commandlineModel.RootNamespace)
                ? _projectContext.RootNamespace
                : _commandlineModel.RootNamespace;

            var defaultDbContextNamespace = $"{RootNamespace}.Areas.Identity.Data";

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

            var usingExistingDbContext = false;
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
                    usingExistingDbContext = true;
                    UserType = FindUserTypeFromDbContext(existingDbContext);
                    DbContextClass = existingDbContext.Name;
                    DbContextNamespace = existingDbContext.Namespace;
                }
            }
            else
            {
                // --dbContext paramter was not specified. So we need to generate one using convention.
                DbContextClass = GetDfaultDbContextName();
                DbContextNamespace = defaultDbContextNamespace;
            }

            var templateModel = new IdentityGeneratorTemplateModel()
            {
                ApplicationName = _applicationInfo.ApplicationName,
                DbContextClass = DbContextClass,
                DbContextNamespace = DbContextNamespace,
                UserClass = UserClass,
                UserClassNamespace = UserClassNamespace,
                UseSQLite = _commandlineModel.UseSQLite,
                IsUsingExistingDbContext = usingExistingDbContext,
                Namespace = RootNamespace,
                IsGenerateCustomUser = IsGenerateCustomUser
            };

            return templateModel;
        }

        private string GetDfaultDbContextName()
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
            var parentType = existingDbContext.BaseType;
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