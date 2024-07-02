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
using Microsoft.DotNet.Scaffolding.Shared.Cli.Utils;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using ConsoleLogger = Microsoft.DotNet.MSIdentity.Shared.ConsoleLogger;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    [Alias("blazor")]
    public class BlazorWebCRUDGenerator : ICodeGenerator
    {
        private IServiceProvider ServiceProvider { get; set; }
        private IApplicationInfo AppInfo { get; set; }
        private ILogger Logger { get; set; }
        private IModelTypesLocator ModelTypesLocator { get; set; }
        private IFileSystem FileSystem { get; set; }
        private IFilesLocator FileLocator { get; set; }
        private IProjectContext ProjectContext { get; set; }
        private IEntityFrameworkService EntityFrameworkService { get; set; }
        private Workspace Workspace { get; set; }
        private ConsoleLogger ConsoleLogger { get; set; }

        public BlazorWebCRUDGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            IFilesLocator fileLocator,
            IProjectContext projectContext,
            IEntityFrameworkService entityframeworkService,
            Workspace workspace)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            AppInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo)); ;
            ModelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            FileLocator = fileLocator ?? throw new ArgumentNullException(nameof(fileLocator));
            ProjectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            EntityFrameworkService = entityframeworkService ?? throw new ArgumentNullException(nameof(entityframeworkService));
            ConsoleLogger = new ConsoleLogger(jsonOutput: false);
        }

        /// <summary>
        /// Scaffold API Controller code into the provided (or created) Endpoints file. If no DbContext is provided, we will use the non-EF templates.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task GenerateCode(BlazorWebCRUDGeneratorCommandLineModel model)
        {
            model.ValidateCommandline();
            var emptyTemplate = !string.IsNullOrEmpty(model.TemplateName) && model.TemplateName.Equals("empty", StringComparison.OrdinalIgnoreCase);
            if (emptyTemplate)
            {
                ArgumentNullException.ThrowIfNull(model);
                string emptyRazorName = "Empty";
                var outputPath = ValidateAndGetOutputPath(string.Empty, emptyRazorName);

                //arguments for 'dotnet new razorcomponent'
                var additionalArgs = new List<string>()
                {
                    "razorcomponent",
                    "--name",
                    emptyRazorName,
                    "--output",
                    Path.GetDirectoryName(outputPath),
                };

                DotnetCommands.ExecuteDotnetNew(ProjectContext.ProjectFullPath, additionalArgs, Logger);
                return;
            }

            //get model and dbcontext
            var modelTypeAndContextModel = await ModelMetadataUtilities.GetModelEFMetadataBlazorAsync(
                model,
                EntityFrameworkService,
                ModelTypesLocator,
                Logger,
                areaName: string.Empty);

            //check if getting model and dbcontext was successful
            if (modelTypeAndContextModel is null ||
                modelTypeAndContextModel.ContextProcessingResult is null ||
                modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus is ContextProcessingStatus.MissingContext ||
                modelTypeAndContextModel.ModelType is null)
            {
                throw new InvalidOperationException("Unable to get model and/or dbcontext metadata.");
            }

            if (!string.IsNullOrEmpty(modelTypeAndContextModel.DbContextFullName) && CalledFromCommandline)
            {
                EFValidationUtil.ValidateEFDependencies(ProjectContext.PackageDependencies, model.DatabaseProvider);
            }

            var templateModel = new BlazorModel(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName)
            {
                Namespace = modelTypeAndContextModel.ModelType.Namespace,
                ModelMetadata = modelTypeAndContextModel.ContextProcessingResult?.ModelMetadata,
                DatabaseProvider = model.DatabaseProvider,
                Template = model.TemplateName,
                BlazorWebAppProperties = await GetBlazorPropertiesAsync()
            };

            ExecuteTemplates(templateModel);
            await ModifyProgramCsAsync(templateModel.BlazorWebAppProperties);
        }

        internal string ValidateAndGetOutputPath(string modelName, string templateName, string relativeFolderPath = null)
        {
            string outputFileName = string.IsNullOrEmpty(modelName) ?
                $"{templateName}{Constants.BlazorExtension}" :
                Path.Combine($"{modelName}Pages", $"{templateName}{Constants.BlazorExtension}");
            string outputFolder = string.IsNullOrEmpty(relativeFolderPath) ?
                Path.Combine(AppInfo.ApplicationBasePath, "Components", "Pages") :
                Path.Combine(AppInfo.ApplicationBasePath, relativeFolderPath);

            var outputPath = Path.Combine(outputFolder, outputFileName);
            return outputPath;
        }

        internal async Task ModifyProgramCsAsync(BlazorWebAppProperties appProperties)
        {
            CodeModifierConfig minimalApiChangesConfig = GetBlazorCodeModifierConfig();
            if (minimalApiChangesConfig is null)
            {
                return;
            }

            var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault(x => x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
            programCsFile = AddBlazorChangesToCodeFile(programCsFile, appProperties);
            var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            var programDocument = project.GetUpdatedDocument(FileSystem, programType);

            //Modifying Program.cs document
            var docEditor = await DocumentEditor.CreateAsync(programDocument);
            var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
            var docBuilder = new DocumentBuilder(docEditor, programCsFile, ConsoleLogger);
            if (docRoot is null)
            {
                return;
            }
            //adding usings
            var newRoot = docBuilder.AddUsings(new CodeChangeOptions());
            var useTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(project.Documents.ToList());
            //add code snippets/changes.
            if (programCsFile.Methods != null && programCsFile.Methods.Count != 0)
            {
                var globalMethod = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
                var globalChanges = globalMethod.CodeChanges;
                var updatedIdentifer = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(newRoot.Members);
                if (updatedIdentifer.HasValue)
                {
                    (string oldValue, string newValue) = updatedIdentifer.Value;
                    globalChanges = ProjectModifierHelper.UpdateVariables(globalChanges, oldValue, newValue);
                }

                if (useTopLevelsStatements)
                {
                    newRoot = DocumentBuilder.ApplyChangesToMethod(newRoot, globalChanges) as CompilationUnitSyntax;
                }
                else
                {
                    var mainMethod = DocumentBuilder.GetMethodFromSyntaxRoot(newRoot, BlazorWebCRUDHelper.Main);
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
            //ApplyTextReplacements will write the updated document to disk (takes changes from above as well).
            await DocumentBuilder.ApplyTextReplacements(programCsFile, changedDocument, new CodeChangeOptions(), FileSystem);
            ConsoleLogger.LogMessage($"Modified {programDocument.Name}.\n");
        }

        internal CodeFile AddBlazorChangesToCodeFile(CodeFile programCsFile, BlazorWebAppProperties appProperties)
        {
            programCsFile.Methods.TryGetValue("Global", out var globalMethod);
            if (globalMethod is null)
            {
                return programCsFile;
            }

            var codeChanges = globalMethod.CodeChanges.ToHashSet();
            if (appProperties.AddRazorComponentsExists)
            {
                if (!appProperties.InteractiveWebAssemblyComponentsExists && !appProperties.InteractiveServerComponentsExists)
                {
                    codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerComponentsSnippet);
                    codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerRenderModeSnippet);
                }
            }
            else
            {
                codeChanges.Add(BlazorWebCRUDHelper.AddRazorComponentsSnippet);
                codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerComponentsSnippet);
                codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerRenderModeSnippet);
            }

            if (appProperties.MapRazorComponentsExists)
            {
                if (appProperties.InteractiveServerRenderModeNeeded)
                {
                    codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerRenderModeSnippet);
                }

                if (appProperties.InteractiveWebAssemblyRenderModeNeeded)
                {
                    codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveWebAssemblyRenderModeSnippet);
                }
            }
            else
            {
                codeChanges.Add(BlazorWebCRUDHelper.AddMapRazorComponentsSnippet);
                codeChanges.Add(BlazorWebCRUDHelper.AddInteractiveServerRenderModeSnippet);
            }

            globalMethod.CodeChanges = codeChanges.ToArray();
            programCsFile.Methods["Global"] = globalMethod;
            return programCsFile;
        }

        internal async Task<BlazorWebAppProperties> GetBlazorPropertiesAsync()
        {
            var blazorAppProperties = new BlazorWebAppProperties();
            //get Program.cs, App.razor and Routes.razor document
            var allTypes = await ModelTypesLocator.GetAllTypesAsync();
            var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            var programDocument = project.GetUpdatedDocument(FileSystem, programType);
            var programDocumentDirectory = Path.GetDirectoryName(programDocument.FilePath);

            blazorAppProperties.AddRazorComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.AddRazorComponentsMethod, BlazorWebCRUDHelper.IServiceCollectionType);
            if (blazorAppProperties.AddRazorComponentsExists)
            {
                blazorAppProperties.InteractiveServerComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.AddInteractiveServerComponentsMethod, BlazorWebCRUDHelper.IRazorComponentsBuilderType);
                blazorAppProperties.InteractiveWebAssemblyComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.AddInteractiveWebAssemblyComponentsMethod, BlazorWebCRUDHelper.IRazorComponentsBuilderType);
            }

            blazorAppProperties.MapRazorComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.MapRazorComponentsMethod, BlazorWebCRUDHelper.IEndpointRouteBuilderContainingType);
            if (blazorAppProperties.MapRazorComponentsExists)
            {
                bool hasInteractiveServerRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.AddInteractiveServerRenderModeMethod, BlazorWebCRUDHelper.RazorComponentsEndpointsConventionBuilderType);
                bool hasInteractiveWebAssemblyRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorWebCRUDHelper.AddInteractiveWebAssemblyRenderModeMethod, BlazorWebCRUDHelper.RazorComponentsEndpointsConventionBuilderType);

                blazorAppProperties.InteractiveServerRenderModeNeeded = !hasInteractiveServerRenderMode && !blazorAppProperties.InteractiveWebAssemblyComponentsExists;
                blazorAppProperties.InteractiveWebAssemblyRenderModeNeeded = !hasInteractiveWebAssemblyRenderMode && blazorAppProperties.InteractiveWebAssemblyComponentsExists;
            }

            var appRazorDocument = project.GetDocumentFromName("App.razor", FileSystem);
            if (appRazorDocument != null)
            {
                blazorAppProperties.IsHeadOutletGlobal = await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, BlazorWebCRUDHelper.GlobalServerRenderModeText) ||
                    await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, BlazorWebCRUDHelper.GlobalWebAssemblyRenderModeText);

                blazorAppProperties.AreRoutesGlobal = await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, BlazorWebCRUDHelper.GlobalServerRenderModeRoutesText) ||
                    await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, BlazorWebCRUDHelper.GlobalWebAssemblyRenderModeRoutesText);
            }
            
            return blazorAppProperties;
        }

        private CodeModifierConfig GetBlazorCodeModifierConfig()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "blazorWebCrudChanges.json";
            string jsonText = ProjectModelHelper.GetManifestResource(assembly, resourceName);
            CodeModifierConfig minimalApiChangesConfig = null;
            try
            {
                minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                ConsoleLogger.LogMessage($"Error deserializing {resourceName}. {ex.Message}");
            }

            return minimalApiChangesConfig;
        }

        //Folders where the .tt templates for Blazor CRUD scenario live. Should be in VS.Web.CG.Mvc\Templates\Blazor
        private IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: AppInfo.ApplicationBasePath,
                    baseFolders: new[] { "Blazor" },
                    projectContext: ProjectContext);
            }
        }

        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        private void ExecuteTemplates(BlazorModel templateModel)
        {
            var templateFolders = TemplateFolders;
            var templateNames = BlazorWebCRUDHelper.GetT4Templates(templateModel.Template, Logger);
            var fullTemplatePaths = templateNames.Select(x => FileLocator.GetFilePath(x, templateFolders));
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , templateModel }
            };

            foreach (var templatePath in fullTemplatePaths)
            {
                ITextTransformation contextTemplate = BlazorWebCRUDHelper.GetBlazorTransformation(templatePath);
                var t4TemplateName = Path.GetFileNameWithoutExtension(templatePath);
                var templatedString = templateInvoker.InvokeTemplate(contextTemplate, dictParams);
                if (!string.IsNullOrEmpty(templatedString))
                {
                    string templatedFilePath = ValidateAndGetOutputPath(templateModel.ModelTypeName, t4TemplateName);
                    var folderName = Path.GetDirectoryName(templatedFilePath);
                    if (!FileSystem.DirectoryExists(folderName))
                    {
                        FileSystem.CreateDirectory(folderName);
                    }

                    FileSystem.WriteAllText(templatedFilePath, templatedString);
                    Logger.LogMessage($"Added Blazor Page : {templatedFilePath}");
                }
            }
        }
    }
}
