// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using ConsoleLogger = Microsoft.DotNet.MSIdentity.Shared.ConsoleLogger;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private ICodeGeneratorActionsService CodeGeneratorActionsService { get; set; }
        private Workspace Workspace { get; set; }
        private ConsoleLogger ConsoleLogger { get; set; }

        public BlazorWebCRUDGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            IFilesLocator fileLocator,
            ICodeGeneratorActionsService codeGeneratorActionsService,
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
            CodeGeneratorActionsService = codeGeneratorActionsService ?? throw new ArgumentNullException(nameof(codeGeneratorActionsService));
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
            System.Diagnostics.Debugger.Launch();
            model.ValidateCommandline(Logger, AppInfo.ApplicationName);
            var emptyTemplate = !string.IsNullOrEmpty(model.TemplateName) && model.TemplateName.Equals("empty", StringComparison.OrdinalIgnoreCase);
            if (emptyTemplate)
            {
                //execute empty template
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
                Template = model.TemplateName
            };

            ExecuteTemplates(templateModel);
            await ModifyProgramCsAsync();
        }

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
                    if (!Directory.Exists(folderName))
                    {
                        Directory.CreateDirectory(folderName);
                    }

                    FileSystem.WriteAllText(templatedFilePath, templatedString);
                    Logger.LogMessage($"Added Blazor Page : {templatedFilePath}");
                }
            }
        }

        private ITextTransformation GetBlazorTransformation(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                return null;
            }

            var host = new TextTemplatingEngineHost()
            {
                TemplateFile = templatePath
            };

            if (templatePath.EndsWith("Create.tt"))
            {
                return new Create()
                {
                    Host = host,
                    Session = host.CreateSession()
                };
            }
            else if (templatePath.EndsWith("Index.tt"))
            {
                return new Templates.Blazor.Index()
                {
                    Host = host,
                    Session = host.CreateSession()
                };
            }
            else if (templatePath.EndsWith("Delete.tt"))
            {
                return new Delete()
                {
                    Host = host,
                    Session = host.CreateSession()
                };
            }
            else if (templatePath.EndsWith("Edit.tt"))
            {
                return new Edit()
                {
                    Host = host,
                    Session = host.CreateSession()
                };
            }
            else if (templatePath.EndsWith("Details.tt"))
            {
                return new Details()
                {
                    Host = host,
                    Session = host.CreateSession()
                };
            }

            return null;
        }

        internal IList<string> GetT4Templates(string templateName)
        {
            var templates = new List<string>();
            var crudTemplate = string.IsNullOrEmpty(templateName) || templateName.Equals("crud", StringComparison.OrdinalIgnoreCase);
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
            string outputFileName =  $"{modelName}Pages\\{templateName}{Constants.BlazorExtension}";
            string outputFolder = string.IsNullOrEmpty(relativeFolderPath)
                ? Path.Combine(AppInfo.ApplicationBasePath, "Components", "Pages")
                : Path.Combine(AppInfo.ApplicationBasePath, relativeFolderPath);

            var outputPath = Path.Combine(outputFolder, outputFileName);
            return outputPath;
        }

        internal async Task ModifyProgramCsAsync()
        {
            var jsonText = GetBlazorCodeModifierConfig();
            CodeModifierConfig minimalApiChangesConfig = null;
            try
            {
                minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                ConsoleLogger.LogMessage($"Error deserializing blazorWebCrudChanges.json file. {ex.Message}");
            }

            if (minimalApiChangesConfig != null)
            {
                //Getting Program.cs document
                var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault();
                var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
                var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
                var programDocument = GetUpdatedDocument(project, programType);

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

        //Given CodeAnalysis.Project and ModelType, return CodeAnalysis.Document by reading the latest file from disk.
        //Need CodeAnalysis.Project for AddDocument method.
        internal Document GetUpdatedDocument(Project project, ModelType type)
        {
            if (project != null && type != null)
            {
                string filePath = type.TypeSymbol?.Locations.FirstOrDefault()?.SourceTree?.FilePath;
                string fileText = FileSystem.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(fileText))
                {
                    return project.AddDocument(filePath, fileText);
                }
            }

            return null;
        }

        private string GetBlazorCodeModifierConfig()
        {
            string jsonText = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.Where(x => x.EndsWith("blazorWebCrudChanges.json")).FirstOrDefault();
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

        public const string Main = nameof(Main);

        //Template info
        private const string CreateBlazorTemplate = "Create.tt";
        private const string DeleteBlazorTemplate = "Delete.tt";
        private const string DetailsBlazorTemplate = "Details.tt";
        private const string EditBlazorTemplate = "Edit.tt";
        private const string IndexBlazorTemplate = "Index.tt";
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
