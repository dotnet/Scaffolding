// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    [Alias("identity")]
    public class IdentityGenerator : ICodeGenerator
    {
        private const string IdentityAreaName = "Identity";

        internal static readonly string DefaultBootstrapVersion = "5";
        // A hashset would allow faster lookups, but would cause a perf hit when formatting the error string for invalid bootstrap version.
        // Also, with a list this small, the lookup perf hit will be largely irrelevant.
        internal static readonly IReadOnlyList<string> ValidBootstrapVersions = new List<string>()
        {
            "3",
            "4",
            "5"
        };

        internal static readonly string ContentVersionDefault = "Default";
        internal static readonly string ContentVersionBootstrap3 = "Bootstrap3";
        internal static readonly string ContentVersionBootstrap4 = "Bootstrap4";

        internal static readonly string DefaultContentRelativeBaseDir = "Identity";
        internal static readonly string VersionedContentRelativeBaseDir = "Identity_Versioned";

        //const strings for Program.cs edits.
        internal const string AddDbContext = nameof(AddDbContext);
        internal const string AddDefaultIdentity = nameof(AddDefaultIdentity);
        internal const string AddEntityFrameworkStores = nameof(AddEntityFrameworkStores);
        internal const string OptionsUseConnectionString = "options.{0}(connectionString)";
        internal const string GetConnectionString = nameof(GetConnectionString);
        internal const string UseSqlite = nameof(UseSqlite);
        internal const string UseSqlServer = nameof(UseSqlServer);
        internal const string ProgramCsFileName = "Program.cs";

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeneratorActionService;
        private IProjectContext _projectContext;
        private IConnectionStringsWriter _connectionStringsWriter;
        private Workspace _workspace;
        private ICodeGenAssemblyLoadContext _loader;
        private IFileSystem _fileSystem;

        // The default-version content files will go in the default location: "Identity\" - (DefaultContentRelativeBaseDir)
        //      other content versions will go in "Identity_Versioned\[VersionIndicator]\" - (VersionedContentRelativeBaseDir)
        // Doing it this way is to maintain back-compat from before multiple content versions were supported.
        // The default content goes in the place where the only content used to be.
        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[]
                     {
                         Path.Combine(DefaultContentRelativeBaseDir, "Controllers"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Data"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Extensions"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Services"),
                         Path.Combine(DefaultContentRelativeBaseDir, "Pages"),
                         DefaultContentRelativeBaseDir
                     },
                    _projectContext);
            }
        }

        // Returns the set of template folders appropriate for templateModel.ContentVersion
        private IEnumerable<string> GetTemplateFoldersForContentVersion(IdentityGeneratorTemplateModel templateModel)
        {
            if (!(templateModel is IdentityGeneratorTemplateModel2 templateModel2))
            {   // for back-compat
                return TemplateFolders;
            }

            // The default content is packaged under the default location "Identity\*" (no subfolder).
            if (string.Equals(templateModel2.ContentVersion, ContentVersionDefault, StringComparison.Ordinal))
            {
                return TemplateFolders;
            }

            // For non-default bootstrap versions, the content is packaged under "Identity_Versioned\[Version_Identifier]\*"
            // Note: In the future, if content gets pivoted on things other than bootstrap, this logic will need enhancement.
            if (string.Equals(templateModel2.ContentVersion, ContentVersionBootstrap3, StringComparison.Ordinal) || 
                string.Equals(templateModel2.ContentVersion, ContentVersionBootstrap4, StringComparison.Ordinal))
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[] {
                        Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{templateModel2.BootstrapVersion}")
                    },
                    _projectContext);
            }

            //return default template path if ContentVersion == DefaultVersion and if we can't figure out/invalid versions of bootstrap.
            //better than throwing an invalid bootstrap version exception.
            return TemplateFolders;
        }

        // Returns the root directory of the template folders appropriate for templateModel.ContentVersion
        private string GetTemplateFolderRootForContentVersion(IdentityGeneratorTemplateModel templateModel)
        {
            string relativePath = null;

            if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
            {
                if (string.Equals(templateModel2.ContentVersion, ContentVersionDefault, StringComparison.Ordinal))
                {
                    relativePath = DefaultContentRelativeBaseDir;
                }
                else if (string.Equals(templateModel2.ContentVersion, ContentVersionBootstrap3, StringComparison.Ordinal))
                {
                    // Note: In the future, if content gets pivoted on things other than bootstrap, this logic will need enhancement.
                    relativePath = Path.Combine(VersionedContentRelativeBaseDir, $"Bootstrap{templateModel2.BootstrapVersion}");
                }

                if (string.IsNullOrEmpty(relativePath))
                {
                    //set to defaultPath if invalid ContentVersion for Identity scaffolding. Not throwing an InvalidOpException anymore.
                    relativePath = DefaultContentRelativeBaseDir;
                }
            }
            else
            {
                relativePath = DefaultContentRelativeBaseDir;
            }

            return TemplateFoldersUtilities.GetTemplateFolders(
                Constants.ThisAssemblyName,
                _applicationInfo.ApplicationBasePath,
                new[] {
                    relativePath
                },
                _projectContext
            ).First();
        }

        public IdentityGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            ICodeGeneratorActionsService actionService,
            IProjectContext projectContext,
            IConnectionStringsWriter connectionStringsWriter,
            Workspace workspace,
            ICodeGenAssemblyLoadContext loader,
            IFileSystem fileSystem,
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

            _applicationInfo = applicationInfo;
            _serviceProvider = serviceProvider;
            _codegeneratorActionService = actionService;
            _projectContext = projectContext;
            _connectionStringsWriter = connectionStringsWriter;
            _workspace = workspace;
            _loader = loader;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task GenerateCode(IdentityGeneratorCommandLineModel commandlineModel)
        {
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            if (commandlineModel.ListFiles)
            {
                ShowFileList(commandlineModel.BootstrapVersion);
                return;
            }

            var templateModelBuilder = new IdentityGeneratorTemplateModelBuilder(
                commandlineModel,
                _applicationInfo,
                _projectContext,
                _workspace,
                _loader,
                _fileSystem,
                _logger);

            var templateModel = await templateModelBuilder.ValidateAndBuild();
            EnsureFolderLayout(IdentityAreaName, templateModel);
            //Identity is not supported in minimal apps.
            var minimalApp = await ProjectModifierHelper.IsMinimalApp(new ModelTypesLocator(_workspace));
            if (minimalApp)
            {
                //remove IdentityGeneratorFilesConfig.IdentityHostingStartup. This is not super performant but doesn't need to be.
                int hostingStartupIndex = Array.IndexOf(templateModel.FilesToGenerate, IdentityGeneratorFilesConfig.IdentityHostingStartup);
                if (hostingStartupIndex != -1)
                {
                    templateModel.FilesToGenerate = templateModel.FilesToGenerate.Where((source, index) => index != hostingStartupIndex).ToArray();
                }
                
                //edit Program.cs in minimal hosting scenario
                await EditProgramCsForIdentity(
                    new ModelTypesLocator(_workspace),
                    templateModel.DbContextClass,
                    templateModel.UserClass,
                    templateModel.DbContextNamespace,
                    templateModel.UseSQLite);
                //return;
            }

            await AddTemplateFiles(templateModel);
            await AddStaticFiles(templateModel);
        }

        private string GetIdentityCodeModifierConfig()
        {
            string jsonText = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.Where(x => x.EndsWith("identityMinimalHostingChanges.json")).FirstOrDefault();
            if (!string.IsNullOrEmpty(resourceName))
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonText = reader.ReadToEnd();
                }
            }
            return jsonText;
        }

        /// <summary>
        /// Edit the Program.cs file for Individual Auth. Adds GlobalStatements found in 'identityMinimalHostingChanges.json' file.
        /// </summary>
        /// <param name="modelTypesLocator">To find Program.cs model type and document for editing</param>
        /// <param name="dbContextClassName">For injecting the DbContext class in statements.</param>
        /// <param name="identityUserClassName">For injecting the IdentityUser class in statements.</param>
        /// <param name="dbContextNamespace">For injecting the namespace for DbContext class in statements.</param>
        /// <param name="useSqlite">To opt between injecting UseSqlite or UseSqlServer</param>
        /// <returns></returns>
        internal async Task EditProgramCsForIdentity(
            IModelTypesLocator modelTypesLocator,
            string dbContextClassName,
            string identityUserClassName,
            string dbContextNamespace,
            bool useSqlite = false)
        {
            var jsonText = GetIdentityCodeModifierConfig();
            CodeModifierConfig identityProgramFileConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            if (identityProgramFileConfig != null)
            {
                var programCsFile = identityProgramFileConfig.Files.FirstOrDefault();
                //Add the newly generated DbContext's namespace.
                programCsFile.Usings = programCsFile.Usings.Append(dbContextNamespace).ToArray();
                var programType = modelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? modelTypesLocator.GetType("Program").FirstOrDefault();
                var programDocument = modelTypesLocator.GetAllDocuments().Where(d => d.Name.EndsWith(ProgramCsFileName)).FirstOrDefault();
                var docEditor = await DocumentEditor.CreateAsync(programDocument);

                var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
                var docBuilder = new DocumentBuilder(docEditor, programCsFile, new Microsoft.DotNet.MSIdentity.Shared.ConsoleLogger(jsonOutput: false));
                //adding usings
                var newRoot = docBuilder.AddUsings(new CodeChangeOptions());
                //add code snippets/changes.
                if (programCsFile.Methods != null && programCsFile.Methods.Any())
                {
                    var globalChanges = programCsFile.Methods.Where(x => x.Key == "Global").First().Value;
                    foreach (var change in globalChanges.CodeChanges)
                    {
                        change.Block = EditIdentityStrings(change.Block, dbContextClassName, identityUserClassName, useSqlite);
                        newRoot = DocumentBuilder.AddGlobalStatements(change, newRoot);
                    }
                }
                //replace root node with all the updates.
                docEditor.ReplaceNode(docRoot, newRoot);
                //write to Program.cs file
                await docBuilder.WriteToClassFileAsync(programDocument.FilePath);
            }
        }

        internal static string EditIdentityStrings(string stringToModify, string dbContextClassName, string identityUserClassName, bool isSqlite)
        {
            if (string.IsNullOrEmpty(stringToModify))
            {
                return string.Empty;
            }

            string modifiedString = stringToModify;
            if (stringToModify.Contains(AddDbContext))
            {
                modifiedString = modifiedString.Replace("AddDbContext<{0}>", $"{AddDbContext}<{dbContextClassName}>");
            }
            if (stringToModify.Contains(AddDefaultIdentity))
            {
                modifiedString = modifiedString.Replace("AddDefaultIdentity<{0}>", $"{AddDefaultIdentity}<{identityUserClassName}>");
            }
            if (stringToModify.Contains(AddEntityFrameworkStores))
            {
                modifiedString = modifiedString.Replace("AddEntityFrameworkStores<{0}>", $"{AddEntityFrameworkStores}<{dbContextClassName}>");
            }
            if (stringToModify.Contains(OptionsUseConnectionString))
            {
                modifiedString = modifiedString.Replace("options.{0}",
                    isSqlite ?  $"options.{UseSqlite}" :
                                $"options.{UseSqlServer}");
            }
            if (stringToModify.Contains(GetConnectionString))
            {
                modifiedString = modifiedString.Replace("GetConnectionString(\"{0}\")", $"GetConnectionString(\"{dbContextClassName}Connection\")");
            }
            return modifiedString;
        }

        private void ShowFileList(string commandBootstrapVersion)
        {
            string contentVersion = string.Equals(commandBootstrapVersion, "3", StringComparison.Ordinal)
                ? ContentVersionBootstrap3
                : ContentVersionDefault;

            _logger.LogMessage("File List:");

            IEnumerable<string> files = IdentityGeneratorFilesConfig.GetFilesToList(contentVersion);

            _logger.LogMessage(string.Join(Environment.NewLine, files));

            if (_fileSystem is SimulationModeFileSystem simModefileSystem)
            {
                foreach (string fileName in files)
                {
                    simModefileSystem.AddMetadataMessage(fileName);
                }
            }
        }

        private async Task AddStaticFiles(IdentityGeneratorTemplateModel templateModel)
        {
            string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            string templateFolderRoot = GetTemplateFolderRootForContentVersion(templateModel);

            foreach (IdentityGeneratorFile staticFile in templateModel.FilesToGenerate.Where(f => !f.IsTemplate))
            {
                string outputPath = Path.Combine(projectDir, staticFile.OutputPath);
                if (staticFile.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(staticFile, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding static file: {staticFile.Name}", LogMessageLevel.Trace);

                    await _codegeneratorActionService.AddFileAsync(
                        outputPath,
                        Path.Combine(templateFolderRoot, staticFile.SourcePath)
                    );
                }
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorTemplateModel templateModel)
        {
            string projectDir = Path.GetDirectoryName(_projectContext.ProjectFullPath);
            IEnumerable<IdentityGeneratorFile> templates = templateModel.FilesToGenerate.Where(t => t.IsTemplate);
            IEnumerable<string> templateFolders = GetTemplateFoldersForContentVersion(templateModel);
            foreach (IdentityGeneratorFile template in templates)
            {
                string outputPath = Path.Combine(projectDir, template.OutputPath);
                if (template.ShouldOverWrite != OverWriteCondition.Never || !DoesFileExist(template, projectDir))
                {
                    // We never overwrite some files like _ViewImports.cshtml.
                    _logger.LogMessage($"Adding template: {template.Name}", LogMessageLevel.Trace);

                    await _codegeneratorActionService.AddFileFromTemplateAsync(
                        outputPath,
                        template.SourcePath,
                        templateFolders,
                        templateModel);
                }
            }

            if (!templateModel.IsUsingExistingDbContext)
            {
                _connectionStringsWriter.AddConnectionString(
                    connectionStringName: $"{templateModel.DbContextClass}Connection",
                    dataBaseName: templateModel.ApplicationName,
                    useSqlite: templateModel.UseSQLite);
            }
        }

        // Returns true if the template file exists in it's output path, or in an alt path (if any are specified)
        private bool DoesFileExist(IdentityGeneratorFile template, string projectDir)
        {
            string outputPath = Path.Combine(projectDir, template.OutputPath);
            if (_fileSystem.FileExists(outputPath))
            {
                return true;
            }

            return template.AltPaths.Any(altPath => _fileSystem.FileExists(Path.Combine(projectDir, altPath)));
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
        private void EnsureFolderLayout(string identityAreaName, IdentityGeneratorTemplateModel templateModel)
        {
            var areaBasePath = Path.Combine(_applicationInfo.ApplicationBasePath, "Areas");
            if (!_fileSystem.DirectoryExists(areaBasePath))
            {
                _fileSystem.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, identityAreaName);
            if (!_fileSystem.DirectoryExists(areaPath))
            {
                _fileSystem.CreateDirectory(areaPath);
            }

            var areaFolders = IdentityGeneratorFilesConfig.GetAreaFolders(
                !templateModel.IsUsingExistingDbContext);

            foreach (var areaFolder in areaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!_fileSystem.DirectoryExists(path))
                {
                    _logger.LogMessage($"Adding folder: {path}", LogMessageLevel.Trace);
                    _fileSystem.CreateDirectory(path);
                }
            }
        }
    }
}
