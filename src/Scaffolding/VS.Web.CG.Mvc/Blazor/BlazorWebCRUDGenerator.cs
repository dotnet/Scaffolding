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

            //check if getting model and dbcontext was successfull
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
            await ModifyProgramCsAsync();
        }

        internal IList<string> GetT4Templates(string templateName)
        {
            var templates = new List<string>();
            var crudTemplate = string.Equals(templateName, "crud", StringComparison.OrdinalIgnoreCase);
            if (crudTemplate)
            {
                return CRUDTemplates.Values.ToList();
            }
            else if (CRUDTemplates.TryGetValue(templateName, out var t4Template))
            {
                templates.Add(t4Template);
            }
            else
            {
                Logger.LogMessage($"Invalid template for the Blazor CRUD scaffolder '{templateName}' entered!", LogMessageLevel.Error);
                throw new ArgumentException(templateName);
            }

            return templates;
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

        internal async Task ModifyProgramCsAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.Where(x => x.EndsWith("blazorWebCrudChanges.json")).FirstOrDefault();
            var jsonText = GetBlazorCodeModifierConfig(assembly, resourceName);
            CodeModifierConfig minimalApiChangesConfig = null;
            try
            {
                minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                ConsoleLogger.LogMessage($"Error deserializing {resourceName}. {ex.Message}");
            }

            if (minimalApiChangesConfig != null)
            {
                //Getting Program.cs document
                var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault();
                var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
                var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
                var programDocument = project.GetUpdatedDocument(FileSystem, programType);

                //Modifying Program.cs document
                var docEditor = await DocumentEditor.CreateAsync(programDocument);
                var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
                var docBuilder = new DocumentBuilder(docEditor, programCsFile, ConsoleLogger);
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
                FileSystem.WriteAllText(programDocument.Name, classFileTxt.ToString());
                ConsoleLogger.LogMessage($"Modified {programDocument.Name}.\n");
            }
        }

        private async Task<IDictionary<string, string>> GetBlazorPropertiesAsync()
        {
            var blazorAppProperties = new Dictionary<string, string>();
            //get Program.cs document
            var allTypes = ModelTypesLocator.GetAllTypes();
            var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            var programDocument = project.GetUpdatedDocument(FileSystem, programType);
            var appRazorDocument = project.Documents.FirstOrDefault(x => x.FilePath.ContainsIgnoreCase("App.razor"));
            bool hasAddRazorComponents = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddRazorComponentsMethod, IRazorComponentsBuilderType);
            blazorAppProperties.Add("hasAddRazorComponents", hasAddRazorComponents.ToString());
            if (!hasAddRazorComponents)
            {
                return blazorAppProperties;
            }
            //AddRazorComponents() is present, check if it is server or webassembly.
            else
            {
                bool hasInteractiveServerComponents = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveServerComponentsMethod, IRazorComponentsBuilderType);
                bool hasInteractiveWebAssemblyComponents = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveWebAssemblyComponentsMethod, IRazorComponentsBuilderType);
                bool addInteractiveComponents = !hasInteractiveServerComponents && !hasInteractiveWebAssemblyComponents;
                blazorAppProperties.Add("hasInteractiveServerComponents", hasInteractiveServerComponents.ToString());
                blazorAppProperties.Add("hasInteractiveWebAssemblyComponents", hasInteractiveWebAssemblyComponents.ToString());
                if (addInteractiveComponents)
                {
                    return blazorAppProperties;
                }
            }

            //check for MapRazorComponents
            bool hasMapRazorComponents = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, MapRazorComponentsMethod, IEndpointRouteBuilderContainingType);
            blazorAppProperties.Add("hasMapRazorComponents", hasMapRazorComponents.ToString());
            if (!hasMapRazorComponents)
            {
                return blazorAppProperties;
            }
            //MapRazorComponents() is present, check if it is server or webassembly.
            else
            {
                bool hasInteractiveServerRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveServerRenderModeMethod, RazorComponentsEndpointsConventionBuilderType);
                bool hasInteractiveWebAssemblyRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveWebAssemblyRenderModeMethod, RazorComponentsEndpointsConventionBuilderType);
                bool addInteractiveRenderMode = !hasInteractiveServerRenderMode && !hasInteractiveWebAssemblyRenderMode;
                blazorAppProperties.Add("hasInteractiveServerRenderMode", hasInteractiveServerRenderMode.ToString());
                blazorAppProperties.Add("hasInteractiveWebAssemblyRenderMode", hasInteractiveWebAssemblyRenderMode.ToString());

                if (addInteractiveRenderMode)
                {
                    return blazorAppProperties;
                }
            }

            if (appRazorDocument != null)
            {
                //bool isGlobal = await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, );
            }
            
            return blazorAppProperties;
        }

        private string GetBlazorCodeModifierConfig(Assembly assembly, string resourceName)
        {
            string jsonText = string.Empty;
            if (assembly != null && !string.IsNullOrEmpty(resourceName))
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonText = reader.ReadToEnd();
                }
            }
            return jsonText;
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
            var templateNames = GetT4Templates(templateModel.Template);
            var fullTemplatePaths = templateNames.Select(x => FileLocator.GetFilePath(x, templateFolders));
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , templateModel }
            };

            foreach (var templatePath in fullTemplatePaths)
            {
                ITextTransformation contextTemplate = GetBlazorTransformation(templatePath);
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

        private ITextTransformation GetBlazorTransformation(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath)) return null;

            var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
            ITextTransformation transformation = null;

            switch (Path.GetFileName(templatePath))
            {
                case "Create.tt":
                    transformation = new Create() { Host = host };
                    break;
                case "Index.tt":
                    transformation = new Templates.Blazor.Index() { Host = host };
                    break;
                case "Delete.tt":
                    transformation = new Delete() { Host = host };
                    break;
                case "Edit.tt":
                    transformation = new Edit() { Host = host };
                    break;
                case "Details.tt":
                    transformation = new Details() { Host = host };
                    break;
            }

            if (transformation != null)
            {
                transformation.Session = host.CreateSession();
            }

            return transformation;
        }

        public const string Main = nameof(Main);

        //Template info
        private const string CreateBlazorTemplate = "Create.tt";
        private const string DeleteBlazorTemplate = "Delete.tt";
        private const string DetailsBlazorTemplate = "Details.tt";
        private const string EditBlazorTemplate = "Edit.tt";
        private const string IndexBlazorTemplate = "Index.tt";
        private const string IEndpointRouteBuilderContainingType = "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder";
        private const string IRazorComponentsBuilderType = "Microsoft.Extensions.DependencyInjection.IRazorComponentsBuilder";
        private const string RazorComponentsEndpointsConventionBuilderType = "Microsoft.AspNetCore.Builder.RazorComponentsEndpointsConventionBuilder";
        private const string IServerSideBlazorBuilderType = "Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder";
        private const string AddInteractiveWebAssemblyComponentsMethod = "AddInteractiveWebAssemblyComponents";
        private const string AddInteractiveServerComponentsMethod = "AddInteractiveServerComponents";
        private const string AddInteractiveWebAssemblyRenderModeMethod = "AddInteractiveWebAssemblyRenderMode";
        private const string AddInteractiveServerRenderModeMethod = "AddInteractiveServerRenderMode";
        private const string AddRazorComponentsMethod = "AddRazorComponents";
        private const string MapRazorComponentsMethod = "MapRazorComponents";
        private const string GlobalServerRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveServer"" />";
        private const string GlobalWebAssemblyRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveWebAssembly"" />";
        private const string GlobalWebAssemblyRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveWebAssembly"" />";
        private const string GlobalServerRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveServer"" />";

        private Dictionary<string, string> _crudTemplates;
        private Dictionary<string, string> CRUDTemplates
        {
            get
            {
                if (_crudTemplates == null)
                {
                    _crudTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Create", CreateBlazorTemplate },
                        { "Delete", DeleteBlazorTemplate },
                        { "Details", DetailsBlazorTemplate },
                        { "Edit",  EditBlazorTemplate },
                        { "Index", IndexBlazorTemplate }
                    };
                }

                return _crudTemplates;
            }
        }
    }
}
