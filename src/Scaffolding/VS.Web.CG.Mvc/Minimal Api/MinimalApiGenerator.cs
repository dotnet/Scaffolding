using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
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

        public MinimalApiGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IProjectContext projectContext,
            IEntityFrameworkService entityframeworkService,
            ICodeModelService codeModelService)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            AppInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo)); ;
            ModelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            CodeGeneratorActionsService = codeGeneratorActionsService ?? throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            ProjectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            EntityFrameworkService = entityframeworkService;
            CodeModelService = codeModelService;
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
                OpenAPI = model.OpenApi
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
                string membersSourceText = await CodeGeneratorActionsService.ExecuteTemplate(GetTemplateName(model, existingEndpointsFile: true), TemplateFolders, templateModel);
                var className = model.EndpintsClassName;
                var endPointsDocument = ModelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.EndsWith(endpointsFilePath));
                if (endPointsDocument != null)
                {
                    var docEditor = await DocumentEditor.CreateAsync(endPointsDocument);
                    if (docEditor is null)
                    {
                        //TODO throw exception
                        return;
                    }
                    var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
                    var classNode = docRoot.DescendantNodes().FirstOrDefault(node => node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.Identifier.ValueText.Contains(className));
                    if (classNode != null && classNode is ClassDeclarationSyntax classDeclaration)
                    {
                        var modifiedClass = classDeclaration.AddMembers(SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(membersSourceText)).WithLeadingTrivia(SyntaxFactory.Tab));
                        docEditor.ReplaceNode(classNode, modifiedClass);
                        var changedDocument = docEditor.GetChangedDocument();
                        var classFileTxt = await changedDocument.GetTextAsync();
                        FileSystem.WriteAllText(endPointsDocument.FilePath, classFileTxt.ToString());
                    }
                }
            }
            //execute CodeGeneratorActionsService.AddFileFromTemplateAsync to add endpoints file.
            else 
            {
                await CodeGeneratorActionsService.AddFileFromTemplateAsync(endpointsFilePath, GetTemplateName(model, existingEndpointsFile: false), TemplateFolders, templateModel);
                Logger.LogMessage(string.Format(MessageStrings.AddedController, endpointsFilePath.Substring(AppInfo.ApplicationBasePath.Length)));

                if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
                {
                    throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered,
                        MessageStrings.Scaffolding_additionalSteps));
                }
            }

            /* build has passed if we are here
             * does model exist?
             *  - throw error if not
             * 
             * does Endpoints File exist??
                yes --> nothing keep going
                no --> create endpoints file

                ef?? --> create or activate db context.
            */

/*            var readmeGenerator = ActivatorUtilities.CreateInstance<ReadMeGenerator>(ServiceProvider);
            try
            {
                await readmeGenerator.GenerateReadmeForArea();
            }
            catch (Exception ex)
            {
                Logger.LogMessage(string.Format(MessageStrings.ReadmeGenerationFailed, ex.Message));
                throw ex.Unwrap(Logger);
            }

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}", MessageStrings.ScaffoldingSuccessful_unregistered,
                    MessageStrings.Scaffolding_additionalSteps));
            }*/
        }

        internal string ValidateAndGetOutputPath(MinimalApiGeneratorCommandLineModel commandLineModel, string outputFileName)
        {
            string outputFolder = string.IsNullOrEmpty(commandLineModel.RelativeFolderPath)
                ? AppInfo.ApplicationBasePath
                : Path.Combine(AppInfo.ApplicationBasePath, commandLineModel.RelativeFolderPath);

            var outputPath = Path.Combine(outputFolder, outputFileName);
            return outputPath;
        }

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

        private void ValidateModel(MinimalApiGeneratorCommandLineModel model)
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
        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        private string GetTemplateName(MinimalApiGeneratorCommandLineModel model, bool existingEndpointsFile)
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
    }
}
