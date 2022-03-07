using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi
{
    [Alias("minimalapi")]
    public class MinimalApiGenerator : ICodeGenerator
    {
        private IServiceProvider ServiceProvider { get; set; }
        private IApplicationInfo AppInfo { get; set; }
        private ILogger Logger { get; set; }
        private IModelTypesLocator ModelTypesLocator { get; set; }
        private IFileSystem FileSystem { get; set; }
        private IProjectContext ProjectContext { get; set; }
        private IEntityFrameworkService EntityFrameworkService { get; set;}
        private ICodeGeneratorActionsService CodeGeneratorActionsService { get; set; }
        private ICodeModelService CodeModelService { get; set; }
        private Workspace Workspace { get; set; }

        private const string ProgramCsFileName = "Program.cs";

        public MinimalApiGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IProjectContext projectContext,
            IEntityFrameworkService entityframeworkService,
            ICodeModelService codeModelService,
            Workspace workspace)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            AppInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo)); ;
            ModelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            CodeGeneratorActionsService = codeGeneratorActionsService ?? throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            ProjectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            EntityFrameworkService = entityframeworkService ?? throw new ArgumentNullException(nameof(entityframeworkService));
            CodeModelService = codeModelService ?? throw new ArgumentNullException(nameof(codeModelService));
        }

        /// <summary>
        /// Scaffold API Controller code into the provided (or created) Endpoints file. If no DbContext is provided, we will use the non-EF templates.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task GenerateCode(MinimalApiGeneratorCommandLineModel model)
        {
            System.Diagnostics.Debugger.Launch();

            ValidateModel(model);
            var namespaceName = NameSpaceUtilities.GetSafeNameSpaceFromPath(model.RelativeFolderPath, AppInfo.ApplicationName);
            //get model and dbcontext
            var modelTypeAndContextModel = await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                model,
                EntityFrameworkService,
                ModelTypesLocator,
                areaName : string.Empty);

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            var templateModel = new MinimalApiModel(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName, model.EndpintsClassName)
            {
                EndpointsName = model.EndpintsClassName,
                EndpointsNamespace = namespaceName,
                ModelMetadata = modelTypeAndContextModel.ContextProcessingResult?.ModelMetadata,
                NullableEnabled = "enable".Equals(AppInfo?.WorkspaceHelper?.GetMsBuildProperty("Nullable"), StringComparison.OrdinalIgnoreCase),
                OpenAPI = model.OpenApi,
                MethodName = $"Map{modelTypeAndContextModel.ModelType.Name}Endpoints"
            };

            var endpointsModel = ModelTypesLocator.GetAllTypes().FirstOrDefault(t => t.Name.Equals(model.EndpintsClassName));
            var endpointsFilePath = endpointsModel?.TypeSymbol?.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? ValidateAndGetOutputPath(model, model.EndpintsClassName + Constants.CodeFileExtension);

            //endpoints file exists, use CodeAnalysis to add required clauses.
            if (FileSystem.FileExists(endpointsFilePath))
            {
                if (CalledFromCommandline)
                {
                    EFValidationUtil.ValidateEFDependencies(ProjectContext.PackageDependencies, useSqlite: false);
                }
                //get method block with the api endpoints.
                string membersBlockText = await CodeGeneratorActionsService.ExecuteTemplate(GetTemplateName(model, existingEndpointsFile: true), TemplateFolders, templateModel);
                var className = model.EndpintsClassName;
                await AddEndpointsMethod(membersBlockText, endpointsFilePath, className, templateModel);

                if (modelTypeAndContextModel?.ContextProcessingResult?.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
                {
                    throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered,
                        MessageStrings.Scaffolding_additionalSteps));
                }
            }
            //execute CodeGeneratorActionsService.AddFileFromTemplateAsync to add endpoints file.
            else 
            {
                //Add endpoints file with endpoints class since it does not exist.
                await CodeGeneratorActionsService.AddFileFromTemplateAsync(endpointsFilePath, GetTemplateName(model, existingEndpointsFile: false), TemplateFolders, templateModel);
                Logger.LogMessage(string.Format(MessageStrings.AddedController, endpointsFilePath.Substring(AppInfo.ApplicationBasePath.Length)));
                //add app.Map statement to Program.cs
                await ModifyProgramCs(templateModel.MethodName, templateModel.EndpointsNamespace);
            }
        }

        internal string ValidateAndGetOutputPath(MinimalApiGeneratorCommandLineModel commandLineModel, string outputFileName)
        {
            string outputFolder = string.IsNullOrEmpty(commandLineModel.RelativeFolderPath)
                ? AppInfo.ApplicationBasePath
                : Path.Combine(AppInfo.ApplicationBasePath, commandLineModel.RelativeFolderPath);

            var outputPath = Path.Combine(outputFolder, outputFileName);
            return outputPath;
        }

        internal async Task AddEndpointsMethod(string membersBlockText, string endpointsFilePath, string className, MinimalApiModel templateModel)
        {
            if (!string.IsNullOrEmpty(endpointsFilePath) &&
                !string.IsNullOrEmpty(membersBlockText) &&
                !string.IsNullOrEmpty(className) &&
                templateModel != null)
            {
                var endPointsDocument = ModelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.EndsWith(endpointsFilePath));
                if (endPointsDocument != null)
                {
                    var docEditor = await DocumentEditor.CreateAsync(endPointsDocument);
                    if (docEditor is null)
                    {
                        //TODO throw exception
                        return;
                    }
                    //Get class syntax node to add members to the class
                    var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
                    var classNode = docRoot.DescendantNodes().FirstOrDefault(node => node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.Identifier.ValueText.Contains(className));
                    //get namespace node just for the namespace name.
                    var namespaceSyntax = classNode.Parent.DescendantNodes().FirstOrDefault(node => node is NamespaceDeclarationSyntax nsDeclarationSyntax || node is FileScopedNamespaceDeclarationSyntax fsDeclarationSyntax);
                    templateModel.EndpointsNamespace = string.IsNullOrEmpty(namespaceSyntax?.ToString()) ? templateModel.EndpointsNamespace : namespaceSyntax?.ToString();

                    if (classNode != null && classNode is ClassDeclarationSyntax classDeclaration)
                    {
                        var modifiedClass = classDeclaration.AddMembers(
                            SyntaxFactory.GlobalStatement(
                                SyntaxFactory.ParseStatement(membersBlockText))
                            .WithLeadingTrivia(SyntaxFactory.Tab));
                        docEditor.ReplaceNode(classNode, modifiedClass);
                        var classFileSourceTxt = await docEditor.GetChangedDocument()?.GetTextAsync();
                        var classFileTxt = classFileSourceTxt?.ToString();
                        if (!string.IsNullOrEmpty(classFileTxt))
                        {
                            //write to endpoints class path.
                            FileSystem.WriteAllText(endPointsDocument.FilePath, classFileTxt);
                            //add app.Map statement to Program.cs
                            await ModifyProgramCs(templateModel.MethodName, templateModel.EndpointsNamespace);
                        }
                    }
                }
            }
        }


        internal static string GetTemplateName(MinimalApiGeneratorCommandLineModel model, bool existingEndpointsFile)
        {
            if (existingEndpointsFile)
            {
                return string.IsNullOrEmpty(model.DataContextClass) ? Constants.MinimalApiNoClassTemplate : Constants.MinimalApiEfNoClassTemplate;
            }
            else
            {
                return string.IsNullOrEmpty(model.DataContextClass) ? Constants.MinimalApiTemplate : Constants.MinimalApiEfTemplate;
            }
        }


        internal static void ValidateModel(MinimalApiGeneratorCommandLineModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            //check namespace for the Endpoints class for invalid keywords 
            if (!string.IsNullOrEmpty(model.EndpointsNamespace) &&
                !RoslynUtilities.IsValidNamespace(model.EndpointsNamespace))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    MessageStrings.InvalidNamespaceName,
                    model.EndpointsNamespace));
            }
        }
        private async Task ModifyProgramCs(string mapMethodName, string entityClassNamepsace)
        {
            var jsonText = GetMinimalApiCodeModifierConfig();
            CodeModifierConfig minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            if (minimalApiChangesConfig != null)
            {
                var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault();
                programCsFile.Usings = new string[] { entityClassNamepsace };
                var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
                var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName == ProjectContext.AssemblyName);
                var programDocument = GetUpdatedDocument(project, programType);
                var docEditor = await DocumentEditor.CreateAsync(programDocument);

                var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
                var docBuilder = new DocumentBuilder(docEditor, programCsFile, new DotNet.MSIdentity.Shared.ConsoleLogger(jsonOutput: false));
                //adding usings
                var newRoot = docBuilder.AddUsings(new CodeChangeOptions());
                //add code snippets/changes.
                if (programCsFile.Methods != null && programCsFile.Methods.Any())
                {
                    var globalChanges = programCsFile.Methods.Where(x => x.Key == "Global").First().Value;
                    foreach (var change in globalChanges.CodeChanges)
                    {
                        change.Block = string.Format(change.Block, mapMethodName);
                        newRoot = DocumentBuilder.AddGlobalStatements(change, newRoot);
                    }
                }
                //replace root node with all the updates.
                docEditor.ReplaceNode(docRoot, newRoot);
                //write to Program.cs file
                await docBuilder.WriteToClassFileAsync(programDocument.Name);
            }
        }

        private string GetMinimalApiCodeModifierConfig()
        {
            string jsonText = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.Where(x => x.EndsWith("minimalApiChanges.json")).FirstOrDefault();
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

        //Folders where the .cshtml templates are. Should be in VS.Web.CG.Mvc\Templates\MinimalApi
        private IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: AppInfo.ApplicationBasePath,
                    baseFolders: new[] { "MinimalApi" },
                    projectContext: ProjectContext);
            }
        }

        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        //Given CodeAnalysis.Project and ModelType, return CodeAnalysis.Document by reading the latest file from disk.
        //Need CodeAnalysis.Project for AddDocument method.
        private Document GetUpdatedDocument(Project project, ModelType type)
        {
            if (type != null)
            {
                string filePath = type?.TypeSymbol?.Locations.FirstOrDefault()?.SourceTree?.FilePath;
                string fileText = FileSystem.ReadAllText(filePath);
                return project.AddDocument(filePath, fileText);
            }
            return null;
        }
    }
}
