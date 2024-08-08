// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using ConsoleLogger = Microsoft.DotNet.MSIdentity.Shared.ConsoleLogger;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    [Alias("blazor-identity")]
    public class BlazorIdentityGenerator : ICodeGenerator
    {
        private IApplicationInfo AppInfo { get; set; }
        private ILogger Logger { get; set; }
        private IModelTypesLocator ModelTypesLocator { get; set; }
        private IFileSystem FileSystem { get; set; }
        private IFilesLocator FileLocator { get; set; }
        private IProjectContext ProjectContext { get; set; }
        private Workspace Workspace { get; set; }
        private ConsoleLogger ConsoleLogger { get; set; }
        private ICodeGenAssemblyLoadContext AssemblyLoadContextLoader { get; set; }
        private const string Main = nameof(Main);
        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        private IEnumerable<string> _templateFolders;
        //Folders where the .tt templates for Blazor Identity scenario live. Should be in VS.Web.CG.Mvc\Templates\BlazorIdentity
        private IEnumerable<string> TemplateFolders
        {
            get
            {
                if (_templateFolders is null)
                {
                    _templateFolders = TemplateFoldersUtilities.GetTemplateFolders(
                        containingProject: Constants.ThisAssemblyName,
                        applicationBasePath: AppInfo.ApplicationBasePath,
                        baseFolders: new[] { "BlazorIdentity", "General" },
                        projectContext: ProjectContext);
                }
                return _templateFolders;
            }
        }
        private IDictionary<string, string> _allBlazorIdentityFiles;
        private IDictionary<string, string> AllBlazorIdentityFiles
        {
            get
            {
                if (_allBlazorIdentityFiles is null)
                {
                    _allBlazorIdentityFiles = BlazorIdentityHelper.GetBlazorIdentityFiles(FileSystem, TemplateFolders);
                }

                return _allBlazorIdentityFiles;
            }
        }

        private IEnumerable<string> _generalT4Files;
        private IEnumerable<string> GeneralT4Files
        {
            get
            {
                if (_generalT4Files is null)
                {
                    _generalT4Files = BlazorIdentityHelper.GetGeneralT4Files(FileSystem, TemplateFolders);
                }

                return _generalT4Files;
            }
        }

        private IList<Type> _blazorIdentityTemplateTypes;
        private IList<Type> BlazorIdentityTemplateTypes
        {
            get
            {
                if (_blazorIdentityTemplateTypes is null)
                {
                    var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                    _blazorIdentityTemplateTypes = allTypes.Where(t => t.FullName.Contains("Mvc.Templates.BlazorIdentity")).ToList();
                }

                return _blazorIdentityTemplateTypes;
            }
        }

        public BlazorIdentityGenerator(IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            IFilesLocator fileLocator,
            IProjectContext projectContext,
            IEntityFrameworkService entityframeworkService,
            ICodeGenAssemblyLoadContext loader,
            Workspace workspace)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            AppInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo)); ;
            ModelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            FileLocator = fileLocator ?? throw new ArgumentNullException(nameof(fileLocator));
            ProjectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            ConsoleLogger = new ConsoleLogger(jsonOutput: false);
            AssemblyLoadContextLoader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// Scaffold API Controller code into the provided (or created) Endpoints file. If no DbContext is provided, we will use the non-EF templates.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task GenerateCode(BlazorIdentityCommandLineModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (model.ListFiles)
            {
                ShowFileList();
                return;
            }

            var blazorTemplateModel = await ValidateAndBuild(model);
            ExecuteTemplates(blazorTemplateModel);
            await ModifyFilesAsync(blazorTemplateModel);
        }

        internal async Task<BlazorIdentityModel> ValidateAndBuild(BlazorIdentityCommandLineModel commandlineModel)
        {
            commandlineModel.DatabaseProvider = ModelMetadataUtilities.ValidateDatabaseProvider(commandlineModel.DatabaseProviderString, Logger, isIdentity: true);
            ValidateRequiredDependencies(commandlineModel.DatabaseProvider);
            if (string.IsNullOrEmpty(commandlineModel.RootNamespace) || RoslynUtilities.IsValidNamespace(commandlineModel.RootNamespace))
            {
                Logger.LogMessage("Empty or invalid namespace provided, using default namespace.");
                commandlineModel.RootNamespace = ProjectContext.RootNamespace;
            }

            var rootIdentityNamespace = $"{commandlineModel.RootNamespace}.Components.Account";
            var layoutNamespace = $"{commandlineModel.RootNamespace}.Components.Layout.MainLayout";
            var defaultDbContextNamespace = $"{commandlineModel.RootNamespace}.Data";
            var defaultUserNamespace = $"{commandlineModel.RootNamespace}.Data";
            var blazorIdentityModel = new BlazorIdentityModel
            {
                BlazorIdentityNamespace = rootIdentityNamespace,
                BlazorLayoutNamespace = layoutNamespace
            };

            var createDbContext = false;
            //if DataContext is specified, try to find or create it.
            if (!string.IsNullOrEmpty(commandlineModel.DataContextClass))
            {
                var existingDbContext = await RoslynWorkspaceHelper.FindExistingType(
                    Workspace,
                    AssemblyLoadContextLoader,
                    ProjectContext.AssemblyName,
                    commandlineModel.DataContextClass);
                // We need to create one with what the user specified.
                if (existingDbContext is null)
                {
                    blazorIdentityModel.DbContextName = IdentityHelper.GetClassNameFromTypeName(commandlineModel.DataContextClass);
                    blazorIdentityModel.DbContextNamespace = IdentityHelper.GetNamespaceFromTypeName(commandlineModel.DataContextClass)
                        ?? defaultDbContextNamespace;
                    blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                    createDbContext = true;
                }
                else
                {
                    IdentityHelper.ValidateExistingDbContext(existingDbContext, commandlineModel.UserClass);
                    var userClassType = IdentityHelper.FindUserTypeFromDbContext(existingDbContext);
                    blazorIdentityModel.UserClassName = userClassType.Name;
                    blazorIdentityModel.UserClassNamespace = userClassType.Namespace;
                    blazorIdentityModel.DbContextName = existingDbContext.Name;
                    blazorIdentityModel.DbContextNamespace = existingDbContext.Namespace;
                    blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                }
            }
            else
            {
                // --dbContext paramter was not specified. So we need to generate one using convention.
                blazorIdentityModel.DbContextName = IdentityHelper.GetDefaultDbContextName(ProjectContext.ProjectName);
                blazorIdentityModel.DbContextNamespace = defaultDbContextNamespace;
                blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                createDbContext = true;
            }

            var createUserClass = false;
            // if an existing user class was determined from the DbContext, don't try to get it from here.
            // Identity scaffolding must use the user class tied to the existing DbContext (when there is one).
            if (string.IsNullOrEmpty(blazorIdentityModel.UserClassName))
            {
                if (string.IsNullOrEmpty(commandlineModel.UserClass))
                {
                    blazorIdentityModel.UserClassName = "IdentityUser";
                    blazorIdentityModel.UserClassNamespace = "Microsoft.AspNetCore.Identity";
                }
                else
                {
                    var existingUser = await RoslynWorkspaceHelper.FindExistingType(
                        Workspace,
                        AssemblyLoadContextLoader,
                        ProjectContext.AssemblyName,
                        commandlineModel.UserClass);

                    if (existingUser != null)
                    {
                        IdentityHelper.ValidateExistingUserType(existingUser);
                        blazorIdentityModel.UserClassName = existingUser.Name;
                        blazorIdentityModel.UserClassNamespace = existingUser.Namespace;
                    }
                    else
                    {
                        createUserClass = true;
                        blazorIdentityModel.UserClassName = IdentityHelper.GetClassNameFromTypeName(commandlineModel.UserClass);
                        blazorIdentityModel.UserClassNamespace = IdentityHelper.GetNamespaceFromTypeName(commandlineModel.UserClass)
                            ?? defaultDbContextNamespace;
                    }
                }
            }

            var applicationUserModel = new IdentityApplicationUserModel
            {
                UserClassName = blazorIdentityModel.UserClassName,
                UserClassNamespace = blazorIdentityModel.UserClassNamespace
            };

            if (createUserClass)
            {
                ExecuteApplicationUserTemplate(applicationUserModel);
            }

            if (createDbContext)
            {
                var dbContextModel = new IdentityDbContextModel
                {
                    DbContextName = blazorIdentityModel.DbContextName,
                    DbContextNamespace = blazorIdentityModel.DbContextNamespace,
                    UserClassModel = applicationUserModel
                };

                ExecuteDbContextTemplate(dbContextModel);
                var connectionStringsWriter = new ConnectionStringsWriter(AppInfo, FileSystem);
                connectionStringsWriter.AddConnectionString("DefaultConnection", dbContextModel.DbContextName, commandlineModel.DatabaseProvider);
            }

            //gather blazor identity files to generate
            if (!string.IsNullOrEmpty(commandlineModel.Files))
            {
                blazorIdentityModel.FilesToGenerate = commandlineModel.Files.Split(IdentityHelper.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else if (!string.IsNullOrEmpty(commandlineModel.ExcludeFiles))
            {
                IEnumerable<string> excludedFiles = commandlineModel.ExcludeFiles.Split(IdentityHelper.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                //validate excluded files
                var errors = new List<string>();
                var invalidFiles = excludedFiles.Where(f => !AllBlazorIdentityFiles.ContainsKey(f));
                if (invalidFiles.Any())
                {
                    errors.Add(MessageStrings.InvalidFilesListMessage);
                    errors.AddRange(invalidFiles);
                }

                if (errors.Count != 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                }

                //get files to overwrite
                blazorIdentityModel.FilesToGenerate = AllBlazorIdentityFiles.Keys.Except(excludedFiles).ToList();
            }
            else
            {
                blazorIdentityModel.FilesToGenerate = AllBlazorIdentityFiles.Keys.ToList();
            }

            blazorIdentityModel.BaseOutputPath = Path.Combine(AppInfo.ApplicationBasePath, commandlineModel.RelativeFolderPath);
            return blazorIdentityModel;
        }

        private void ShowFileList()
        {
            Logger.LogMessage("File List:\n");
            IEnumerable<string> files = AllBlazorIdentityFiles.Keys;
            Logger.LogMessage(string.Join(Environment.NewLine, files));
            if (FileSystem is SimulationModeFileSystem simModefileSystem)
            {
                foreach (string fileName in files)
                {
                    simModefileSystem.AddMetadataMessage(fileName);
                }
            }
        }

        private void ValidateRequiredDependencies(DbProvider dbProvider)
        {
            var dbProviderPackage = EfConstants.EfPackagesDict[dbProvider] ?? string.Empty;
            var dependencies = new HashSet<string>()
            {
                EfConstants.AspnetNetCoreIdentityEfPackageName,
                EfConstants.EfToolsPackageName,
                dbProviderPackage
            };

            var missingPackages = dependencies.Where(d => !ProjectContext.PackageDependencies.Any(p => p.Name.Equals(d, StringComparison.OrdinalIgnoreCase)));
            if (CalledFromCommandline && missingPackages.Any())
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallPackagesForScaffoldingIdentity, string.Join(",", missingPackages)));
            }
        }

        internal async Task ModifyFilesAsync(BlazorIdentityModel blazorIdentityModel)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "blazorIdentityChanges.json";
            var jsonText = ProjectModelHelper.GetManifestResource(assembly, resourceName);
            CodeModifierConfig minimalApiChangesConfig = null;
            try
            {
                minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                Logger.LogMessage($"Error deserializing {resourceName}. {ex.Message}");
            }

            //Getting Program.cs document
            var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault(x => x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
            await ModifyProgramCsAsync(programCsFile, blazorIdentityModel);

            //Modify all other non .cs files
            var otherFiles = minimalApiChangesConfig.Files.Where(x => !x.FileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            var projectPath = Path.GetDirectoryName(ProjectContext.ProjectFullPath);
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            foreach (var file in otherFiles)
            {
                var fileDoc = project.GetDocumentFromName(file.FileName, FileSystem);
                await DocumentBuilder.ApplyTextReplacements(file, fileDoc, new CodeChangeOptions(), FileSystem);
                if (file.FileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) &&
                    FileSystem.FileExists(fileDoc.Name))
                {
                    var fileText = FileSystem.ReadAllText(fileDoc.Name);
                    fileText = $"@using {blazorIdentityModel.BlazorIdentityNamespace}.Shared" + Environment.NewLine + fileText;
                    FileSystem.WriteAllText(fileDoc.Name, fileText.ToString());
                    Logger.LogMessage($"Modified {fileDoc.Name}.\n");
                }
            }
        }

        internal async Task ModifyProgramCsAsync(CodeFile programCsFile, BlazorIdentityModel blazorIdentityModel)
        {
            if (programCsFile is null)
            {
                return;
            }

            var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            var programDocument = project.GetUpdatedDocument(FileSystem, programType);
            //Modifying Program.cs document
            var docEditor = await DocumentEditor.CreateAsync(programDocument);
            var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
            programCsFile.Usings = programCsFile.Usings.Union(new string[] { blazorIdentityModel.DbContextNamespace, blazorIdentityModel.BlazorIdentityNamespace }).ToArray();
            var docBuilder = new DocumentBuilder(docEditor, programCsFile, ConsoleLogger);
            var newRoot = docBuilder.AddUsings(new CodeChangeOptions());
            var useTopLevelStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(project.Documents.ToList());
            var builderIdentifier = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(newRoot.Members);
            //add code snippets/changes.
            if (programCsFile.Methods != null && programCsFile.Methods.Count > 0)
            {
                var globalMethod = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
                var globalChanges = globalMethod.CodeChanges;
                if (builderIdentifier.HasValue)
                {
                    (string oldValue, string newValue) = builderIdentifier.Value;
                    globalChanges = ProjectModifierHelper.UpdateVariables(globalChanges, oldValue, newValue);
                }

                globalChanges = BlazorIdentityHelper.ApplyIdentityChanges(globalChanges, blazorIdentityModel.DbContextName, blazorIdentityModel.UserClassName, blazorIdentityModel.DatabaseProvider);
                if (useTopLevelStatements)
                {
                    newRoot = DocumentBuilder.ApplyChangesToMethod(newRoot, globalChanges) as CompilationUnitSyntax;
                }
                else
                {
                    var mainMethod = DocumentBuilder.GetMethodFromSyntaxRoot(newRoot, Main);
                    if (mainMethod != null)
                    {
                        var updatedMethod = DocumentBuilder.ApplyChangesToMethod(mainMethod.Body, globalChanges);
                        newRoot = newRoot?.ReplaceNode(mainMethod.Body, updatedMethod);
                    }
                }
            }

            //replace root node with all the updates.
            docEditor.ReplaceNode(docRoot, newRoot);
            //write to Program.cs file
            var changedDocument = docEditor.GetChangedDocument();
            var classFileTxt = await changedDocument.GetTextAsync();
            var classFileString = classFileTxt.ToString();
            classFileString = StringUtil.NormalizeLineEndings(classFileString);
            FileSystem.WriteAllText(programDocument.Name, classFileString);
            Logger.LogMessage($"Modified {programDocument.Name}.\n");
        }

        private void ExecuteTemplates(BlazorIdentityModel templateModel)
        {
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , templateModel }
            };

            foreach (var templateName in templateModel.FilesToGenerate)
            {
                ITextTransformation contextTemplate = GetBlazorIdentityTransformation(templateName);
                var templatedString = templateInvoker.InvokeTemplate(contextTemplate, dictParams);
                if (!string.IsNullOrEmpty(templatedString))
                {
                    //currently only Identity...cs files are of CSharp type, rest are razor
                    string extension = templateName.StartsWith("identity", StringComparison.OrdinalIgnoreCase) ? ".cs" : ".razor";
                    string templateNameWithNamespace = $"{templateModel.BlazorIdentityNamespace}.{templateName}";
                    string templatePath = StringUtil.ToPath(templateNameWithNamespace, templateModel.BaseOutputPath, ProjectContext.RootNamespace);
                    string templatedFilePath = $"{templatePath}{extension}";
                    var folderName = Path.GetDirectoryName(templatedFilePath);
                    if (!FileSystem.DirectoryExists(folderName))
                    {
                        FileSystem.CreateDirectory(folderName);
                    }

                    templatedString = StringUtil.NormalizeLineEndings(templatedString);
                    if (!FileSystem.FileExists(templatedFilePath))
                    {
                        FileSystem.WriteAllText(templatedFilePath, templatedString);
                        Logger.LogMessage($"Added Blazor identity file : {templatedFilePath}");
                    }
                }
            }
        }

        private void ExecuteApplicationUserTemplate(IdentityApplicationUserModel model)
        {
            var t4TemplatePath = GeneralT4Files.FirstOrDefault(x => x.Contains(nameof(IdentityApplicationUser)));
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , model }
            };

            ITextTransformation t4TextTransformation = new IdentityApplicationUser
            {
                Session = host.CreateSession()
            };

            var templatedString = templateInvoker.InvokeTemplate(t4TextTransformation, dictParams);
            if (!string.IsNullOrEmpty(templatedString))
            {
                var identityDataPath = Path.Combine(AppInfo.ApplicationBasePath, "Data");
                if (!FileSystem.DirectoryExists(identityDataPath))
                {
                    FileSystem.CreateDirectory(identityDataPath);
                }

                string templatedFilePath = Path.Combine(identityDataPath, $"{model.UserClassName}.cs");
                templatedString = StringUtil.NormalizeLineEndings(templatedString);
                FileSystem.WriteAllText(templatedFilePath, templatedString);
                Logger.LogMessage($"Added IdentityUser class : {templatedFilePath}");
            }
        }

        private void ExecuteDbContextTemplate(IdentityDbContextModel model)
        {
            var t4TemplatePath = GeneralT4Files.FirstOrDefault(x => x.Contains(nameof(IdentityDbContext)));
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , model }
            };

            ITextTransformation t4TextTransformation = new IdentityDbContext
            {
                Session = host.CreateSession()
            };

            var templatedString = templateInvoker.InvokeTemplate(t4TextTransformation, dictParams);
            if (!string.IsNullOrEmpty(templatedString))
            {
                var identityDataPath = Path.Combine(AppInfo.ApplicationBasePath, "Data");
                if (!FileSystem.DirectoryExists(identityDataPath))
                {
                    FileSystem.CreateDirectory(identityDataPath);
                }

                string templatedFilePath = Path.Combine(identityDataPath, $"{model.DbContextName}.cs");
                templatedString = StringUtil.NormalizeLineEndings(templatedString);
                FileSystem.WriteAllText(templatedFilePath, templatedString);
                Logger.LogMessage($"Added IdentityDbContext class : {templatedFilePath}");
            }
        }

        private ITextTransformation GetBlazorIdentityTransformation(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return null;
            }

            var typeName = StringUtil.GetTypeNameFromNamespace(templateName);
            var t4TemplateType = BlazorIdentityTemplateTypes.FirstOrDefault(x => x.FullName.Contains(templateName) && x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            var t4TemplatePath = AllBlazorIdentityFiles[templateName];
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            try
            {
                ITextTransformation transformation = Activator.CreateInstance(t4TemplateType) as ITextTransformation;
                if (transformation != null)
                {
                    transformation.Session = host.CreateSession();
                }

                return transformation;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
