// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using System.Diagnostics;

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

        private ReflectedTypesProvider _reflectedTypesProvider;

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
        internal bool IsExcludeSpecificed => !string.IsNullOrEmpty(_commandlineModel.ExcludeFiles);
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

            ValidateRequiredDependencies(_commandlineModel.UseSqlite);

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

            // if an existing user class was determined from the DbContext, don't try to get it from here.
            // Identity scaffolding must use the user class tied to the existing DbContext (when there is one).
            if (string.IsNullOrEmpty(UserClass))
            {
                if (string.IsNullOrEmpty(_commandlineModel.UserClass))
                {
                    IsGenerateCustomUser = false;
                    UserClass = "IdentityUser";
                    UserClassNamespace = "Microsoft.AspNetCore.Identity";
                }
                else
                {
                    var existingUser = await FindExistingType(_commandlineModel.UserClass);
                    if (existingUser != null)
                    {
                        ValidateExistingUserType(existingUser);
                        IsGenerateCustomUser = false;
                        UserType = existingUser;
                    }
                    else
                    {
                        IsGenerateCustomUser = true;
                        UserClass = GetClassNameFromTypeName(_commandlineModel.UserClass);
                        UserClassNamespace = GetNamespaceFromTypeName(_commandlineModel.UserClass)
                            ?? defaultDbContextNamespace;
                    }
                }
            }

            if (_commandlineModel.UseDefaultUI)
            {
                ValidateDefaultUIOption();
            }

            bool hasExistingLayout = DetermineSupportFileLocation(out string supportFileLocation, out string layout);

            string boostrapVersion = string.IsNullOrEmpty(_commandlineModel.BootstrapVersion) ? IdentityGenerator.DefaultBootstrapVersion : _commandlineModel.BootstrapVersion;

            var templateModel = new IdentityGeneratorTemplateModel2()
            {
                ApplicationName = _applicationInfo.ApplicationName,
                DbContextClass = DbContextClass,
                DbContextNamespace = DbContextNamespace,
                UserClass = UserClass,
                UserClassNamespace = UserClassNamespace,
                UseSQLite = _commandlineModel.UseSqlite,
                IsUsingExistingDbContext = IsUsingExistingDbContext,
                Namespace = RootNamespace,
                IsGenerateCustomUser = IsGenerateCustomUser,
                UseDefaultUI = _commandlineModel.UseDefaultUI,
                GenerateLayout = !hasExistingLayout,
                Layout = layout,
                LayoutPageNoExtension = Path.GetFileNameWithoutExtension(layout),
                SupportFileLocation = supportFileLocation,
                HasExistingNonEmptyWwwRoot = HasExistingNonEmptyWwwRootDirectory,
                BootstrapVersion = boostrapVersion,
                IsRazorPagesProject = IsRazorPagesLayout(),
                IsBlazorProject = IsBlazorProjectLayout()
            };

            templateModel.ContentVersion = DetermineContentVersion(templateModel);

            ValidateIndividualFileOptions();
            if (!string.IsNullOrEmpty(_commandlineModel.Files))
            {
                NamedFiles = _commandlineModel.Files.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else if(!string.IsNullOrEmpty(_commandlineModel.ExcludeFiles))
            {
                string contentVersion;
                if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
                {
                    contentVersion = templateModel2.ContentVersion;
                }
                else
                {
                    contentVersion = IdentityGenerator.ContentVersionDefault;
                }
                IEnumerable<string> excludedFiles = _commandlineModel.ExcludeFiles.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                IEnumerable<string> allFiles = IdentityGeneratorFilesConfig.GetFilesToList(contentVersion);
                //validate excluded files
                var errors = new List<string>();
                var invalidFiles = excludedFiles.Where(f => !allFiles.Contains(f));
                if (invalidFiles.Any())
                {
                    errors.Add(MessageStrings.InvalidFilesListMessage);
                    errors.AddRange(invalidFiles);
                }

                if (errors.Any())
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                }
                
                //get files to overwrite
                NamedFiles = allFiles.Except(excludedFiles);
            }

            templateModel.FilesToGenerate = DetermineFilesToGenerate(templateModel);

            if (IsFilesSpecified)
            {
                ValidateFilesOption(templateModel);
            }

            if (IsExcludeSpecificed)
            {
                ValidateFilesOption(templateModel);
            }
            return templateModel;
        }

        private IdentityGeneratorFile[] DetermineFilesToGenerate(IdentityGeneratorTemplateModel2 templateModel)
        {
            var filesToGenerate = new List<IdentityGeneratorFile>(IdentityGeneratorFilesConfig.GetFilesToGenerate(NamedFiles, templateModel));

            // Check if we need to add ViewImports and which ones.
            if (!_commandlineModel.UseDefaultUI)
            {
                filesToGenerate.AddRange(IdentityGeneratorFilesConfig.GetViewImports(filesToGenerate, _fileSystem, _applicationInfo.ApplicationBasePath));
            }

            if (IdentityGeneratorFilesConfig.TryGetLayoutPeerFiles(_fileSystem, _applicationInfo.ApplicationBasePath, templateModel, out IReadOnlyList<IdentityGeneratorFile> layoutPeerFiles))
            {
                filesToGenerate.AddRange(layoutPeerFiles);
            }

            var filesToGenerateArray = filesToGenerate.ToArray();

            //Blazor projects with Individual Auth enabled ship with a custom Account\Logout.cshtml file. If found, don't add the Account\Logout template shipped with dotnet/Scaffolding (based on aspnetcore\Identity's template).
            if (templateModel.IsBlazorProject)
            {
                string logoutFilePath = $"{_applicationInfo.ApplicationBasePath}\\Areas\\Identity\\Pages\\Account\\LogOut.cshtml";
                if (File.Exists(logoutFilePath))
                {
                    //remove Account\Logout.cshtml and Account\Logout.cshtml.cs files. This is not super performant but doesn't need to be.
                    filesToGenerateArray = filesToGenerateArray.Where(x => !x.Name.Contains("Account.Logout")).ToArray();
                }
            }

            ValidateFilesToGenerate(filesToGenerateArray);

            return filesToGenerateArray;
        }

        // Returns a string indicating which Identity content to use.
        // Currently only pivots on the bootstrap version, but could pivot on anything.
        private string DetermineContentVersion(IdentityGeneratorTemplateModel templateModel)
        {
            if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
            {
                if (string.Equals(templateModel2.BootstrapVersion, IdentityGenerator.DefaultBootstrapVersion, StringComparison.Ordinal))
                {
                    return IdentityGenerator.ContentVersionDefault;
                }
                else if (string.Equals(templateModel2.BootstrapVersion, "3", StringComparison.Ordinal))
                {
                    return IdentityGenerator.ContentVersionBootstrap3;
                }
                else if (string.Equals(templateModel2.BootstrapVersion, "4", StringComparison.Ordinal))
                {
                    return IdentityGenerator.ContentVersionBootstrap4;
                }
            }
            //return default bootstrap version if no specific one can be determined. Better than to throw an exception here.
            return IdentityGenerator.ContentVersionDefault;
        }

        private static readonly IReadOnlyList<string> _ExistingLayoutFileCheckLocations = new List<string>()
        {
            "Pages/Shared/",
            "Views/Shared/"
        };

        // If there is no layout file, check the existence of the key directories, and put the support files in the value directory.
        private static readonly IReadOnlyDictionary<string, string> _CheckDirectoryToTargetMapForSupportFiles = new Dictionary<string, string>()
        {
            { "Pages/", "Pages/Shared/" },
            { "Views/", "Views/Shared/" }
        };

        internal static readonly string _DefaultSupportLocation = "Pages/Shared/";

        internal static readonly string _LayoutFileName = "_Layout.cshtml";

        // Checks if there is an existing layout page, and based on its location or lack of existence, determines where to put support pages.
        // Returns true if there is an existing layout page.
        // Note: layoutFile & supportFileLocation will always have a value when this exits.
        //      supportFileLocation is rooted
        internal bool DetermineSupportFileLocation(out string supportFileLocation, out string layoutFile)
        {
            string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);

            if (!string.IsNullOrEmpty(_commandlineModel.Layout))
            {
                if (_commandlineModel.Layout.StartsWith("~"))
                {
                    layoutFile = _commandlineModel.Layout.Substring(1);
                }
                else
                {
                    layoutFile = _commandlineModel.Layout;
                }

                while (!string.IsNullOrEmpty(layoutFile) &&
                    (layoutFile[0] == Path.DirectorySeparatorChar ||
                    layoutFile[0] == Path.AltDirectorySeparatorChar))
                {
                    layoutFile = layoutFile.Substring(1);
                }

                // if the input layout file path consists of only slashes (and possibly a lead ~), it'll be empty at this point.
                // So we'll treat it as if no layout file was specified (handled below).
                if (!string.IsNullOrEmpty(layoutFile))
                {
                    // normalize the path characters sp GetDirectoryName() works.
                    layoutFile = layoutFile.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    supportFileLocation = Path.GetDirectoryName(layoutFile);

                    // always use forward slashes for the layout file path.
                    layoutFile = layoutFile.Replace("\\", "/");

                    return true;
                }
            }

            bool hasExistingLayoutFile = false;
            supportFileLocation = null;
            layoutFile = null;

            foreach (string checkDirectory in _ExistingLayoutFileCheckLocations)
            {
                string checkFile = Path.Combine(projectDir, checkDirectory, _LayoutFileName);
                if (_fileSystem.FileExists(checkFile))
                {
                    hasExistingLayoutFile = true;
                    supportFileLocation = checkDirectory;
                    layoutFile = Path.Combine(supportFileLocation, _LayoutFileName);
                    break;
                }
            }

            if (string.IsNullOrEmpty(supportFileLocation))
            {
                foreach (KeyValuePair<string, string> checkMapEntry in _CheckDirectoryToTargetMapForSupportFiles)
                {
                    string checkDirectory = Path.Combine(projectDir, checkMapEntry.Key);
                    if (_fileSystem.DirectoryExists(checkDirectory))
                    {
                        supportFileLocation = checkMapEntry.Value;
                        layoutFile = Path.Combine(supportFileLocation, _LayoutFileName);
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(supportFileLocation))
            {
                supportFileLocation = _DefaultSupportLocation;
                layoutFile = Path.Combine(supportFileLocation, _LayoutFileName);
            }

            return hasExistingLayoutFile;
        }

        // A simple check to determine if the project being scaffolded appears to be a razor project.
        internal bool IsRazorPagesLayout()
        {
            string pagesFilesCheckPath = Path.Combine(_applicationInfo.ApplicationBasePath, "Pages");
            return Directory.Exists(pagesFilesCheckPath);
        }


        internal bool IsBlazorProjectLayout()
        {
            bool isBlazorProject = false;
            string programCsFile = Path.Combine(_applicationInfo.ApplicationBasePath, "Program.cs");
            if (!File.Exists(programCsFile))
            {
                programCsFile = Directory.EnumerateFiles(_applicationInfo.ApplicationBasePath, "Program.cs").FirstOrDefault();
            }

            //check for Blazor server
            if (!string.IsNullOrEmpty(programCsFile))
            {
                string programCsText = File.ReadAllText(programCsFile);
                if (!string.IsNullOrEmpty(programCsText) && programCsText.Contains("AddServerSideBlazor()"))
                {
                    isBlazorProject = true;
                }
            }

            //check for blazor wasm
            if (!isBlazorProject &&
                _projectContext.PackageDependencies.Any(p => p.Name.Equals("Microsoft.AspNetCore.Components.WebAssembly", StringComparison.Ordinal)))
            {
                isBlazorProject = true;
            }

            return isBlazorProject;
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

        // returns true if, at the project root, there is a wwwroot directory that contains at least 1 file.
        // return false otherwise.
        private bool HasExistingNonEmptyWwwRootDirectory
        {
            get
            {
                string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
                string wwwrootCheckLocation = Path.Combine(projectDir, "wwwroot");

                return _fileSystem.DirectoryExists(wwwrootCheckLocation)
                    && _fileSystem.EnumerateFiles(wwwrootCheckLocation, "*", SearchOption.AllDirectories).Any();
            }
        }

        private void ValidateIndividualFileOptions() 
        {
            //Both options should not be selected. Users should either scaffold a particular set of files OR exclude a particular set of files.
            if(IsFilesSpecified && IsExcludeSpecificed)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.InvalidOptionCombination,"--files", "--excludeFiles"));
            }
        }
        private void ValidateDefaultUIOption()
        {
            var errorStrings = new List<string>();

            if (IsFilesSpecified)
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidOptionCombination,"--files", "--useDefaultUI"));
            }

            if(IsExcludeSpecificed)
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidOptionCombination,"--excludeFiles", "--useDefaultUI"));
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

        private void ValidateFilesOption(IdentityGeneratorTemplateModel templateModel)
        {
            var errors = new List<string>();

            string contentVersion;
            if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
            {
                contentVersion = templateModel2.ContentVersion;
            }
            else
            {
                contentVersion = IdentityGenerator.ContentVersionDefault;
            }

            var invalidFiles = NamedFiles.Where(f => !IdentityGeneratorFilesConfig.GetFilesToList(contentVersion).Contains(f));

            if (invalidFiles.Any())
            {
                errors.Add(MessageStrings.InvalidFilesListMessage);
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
                defaultDbContextName = "IdentityDataContext";
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
                    string.Format(MessageStrings.DbContextNeedsToInheritFromIdentityContextMessage,
                        existingDbContext.Name,
                        "Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext"));
            }

            // Validate that the `--userClass` parameter is not passed.
            if (!string.IsNullOrEmpty(_commandlineModel.UserClass))
            {
                errorStrings.Add(MessageStrings.UserClassAndDbContextCannotBeSpecifiedTogether);
            }

            if (errorStrings.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        private void ValidateExistingUserType(Type existingUser)
        {
            var errorStrings = new List<string>();

            // Validate that the user type inherits from IdentityUser
            bool foundValidParentDbContextClass = IsTypeDerivedFromIdentityUser(existingUser);

            if (!foundValidParentDbContextClass)
            {
                errorStrings.Add(
                    string.Format(MessageStrings.DbContextNeedsToInheritFromIdentityContextMessage,
                        existingUser.Name,
                        "Microsoft.AspNetCore.Identity.IdentityUser"));
            }

            if (errorStrings.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        private static bool IsTypeDerivedFromIdentityUser(Type type)
        {
            var parentType = type.BaseType;
            while (parentType != null && parentType != typeof(object))
            {
                if (parentType.FullName == "Microsoft.AspNetCore.Identity.IdentityUser"
                    && parentType.Assembly.GetName().Name == "Microsoft.Extensions.Identity.Stores")
                {
                    return true;
                }

                parentType = parentType.BaseType;
            }

            return false;
        }

        private static bool IsTypeDerivedFromIdentityDbContext(Type type)
        {
            var parentType = type.BaseType;
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
                    return true;
                }

                parentType = parentType.BaseType;
            }

            return false;
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
                    string.Format(MessageStrings.UserClassCouldNotBeDetermined,
                        existingDbContext.Name));
            }

            return usersProperty.PropertyType.GetGenericArguments().First();
        }

        private async Task<Type> FindExistingType(string type)
        {
            if (_reflectedTypesProvider == null)
            {
                var compilation = await _workspace.CurrentSolution.Projects
                    .Where(p => p.AssemblyName == _projectContext.AssemblyName)
                    .First()
                    .GetCompilationAsync();

                _reflectedTypesProvider = new ReflectedTypesProvider(
                    compilation,
                    null,
                    _projectContext,
                    _loader,
                    _logger);

                if (_reflectedTypesProvider.GetCompilationErrors() != null
                    && _reflectedTypesProvider.GetCompilationErrors().Any())
                {
                    // Failed to build the project.
                    throw new InvalidOperationException(
                        string.Format(MessageStrings.CompilationFailedMessage,
                            Environment.NewLine,
                            string.Join(Environment.NewLine, _reflectedTypesProvider.GetCompilationErrors())));
                }
            }

            var reflectedType = _reflectedTypesProvider.GetReflectedType(type, true);

            return reflectedType;
        }

        private void ValidateCommandLine(IdentityGeneratorCommandLineModel model)
        {
            var errorStrings = new List<string>();
            if (!string.IsNullOrEmpty(model.UserClass) && !RoslynUtilities.IsValidNamespace(model.UserClass))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidUserClassName, model.UserClass));
            }

            if (!string.IsNullOrEmpty(model.DbContext) && !RoslynUtilities.IsValidNamespace(model.DbContext))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidDbContextClassName, model.DbContext));
            }

            if (!string.IsNullOrEmpty(model.RootNamespace) && !RoslynUtilities.IsValidNamespace(model.RootNamespace))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidNamespaceName, model.RootNamespace));
            }

            if (!string.IsNullOrEmpty(model.Layout) && model.GenerateLayout)
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidOptionCombination,"--layout", "--generateLayout"));
            }

            if (!string.IsNullOrEmpty(model.BootstrapVersion) && !IdentityGenerator.ValidBootstrapVersions.Contains(model.BootstrapVersion.Trim(' ', '\n'))
            {
                errorStrings.Add(string.Format(MessageStrings.InvalidBootstrapVersionForScaffolding, model.BootstrapVersion, string.Join(", ", IdentityGenerator.ValidBootstrapVersions)));
            }

            if (errorStrings.Any())
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        private void ValidateRequiredDependencies(bool useSqlite)
        {
            var dependencies = new HashSet<string>()
            {
                "Microsoft.AspNetCore.Identity.UI",
                "Microsoft.EntityFrameworkCore.Design"
            };

            const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
            var isEFDesignPackagePresent = _projectContext
                .PackageDependencies
                .Any(package => package.Name.Equals(EfDesignPackageName, StringComparison.OrdinalIgnoreCase));

            if (!useSqlite)
            {
                dependencies.Add("Microsoft.EntityFrameworkCore.SqlServer");
            }

            var missingPackages = dependencies.Where(d => !_projectContext.PackageDependencies.Any(p => p.Name.Equals(d, StringComparison.OrdinalIgnoreCase)));
            if (CalledFromCommandline && missingPackages.Any())
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallPackagesForScaffoldingIdentity, string.Join(",", missingPackages)));
            }
        }

        //IFileSystem is DefaultFileSystem in commandline scenarios and SimulationModeFileSystem in VS scenarios.
        private bool CalledFromCommandline => !(_fileSystem is SimulationModeFileSystem);
    }
}
