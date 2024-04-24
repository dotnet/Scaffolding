// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi
{
    public class MinimalApiCommand : AsyncCommand<MinimalApiCommand.MinimalApiSettings>
    {
        private readonly IAppSettings _appSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;
        private readonly IHostService _hostService;
        private readonly ICodeService _codeService;
        private List<string> _excludeList;

        public MinimalApiCommand(
            IAppSettings appSettings,
            IEnvironmentService environmentService,
            IFileSystem fileSystem,
            IHostService hostService,
            ICodeService codeService,
            ILogger logger)
        {
            _appSettings = appSettings;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _hostService = hostService;
            _logger = logger;
            _codeService = codeService;
            _excludeList = [];
        }

        public override async Task<int> ExecuteAsync(CommandContext context, MinimalApiSettings settings)
        {
            AnsiConsole.MarkupLine("[green]Executing minimalapi command...[/]");
            //setup project settings
            _appSettings.AddSettings("workspace", new WorkspaceSettings());
            _appSettings.Workspace().InputPath = settings.Project;
            AspNetProjectService projectService = new(_appSettings, _logger, _hostService, _environmentService, _codeService);
            StartupService startupService = new(_appSettings, _environmentService, _hostService, _logger);
            await startupService.RunAsync();
            await projectService.RunAsync();
            // Your logic for minimalapi command here
            MinimalApiModel minimalApiModel = await AnsiConsole.Status()
                .WithSpinner()
                .Start("Validating commandline info!", async statusContext =>
                {
                    var apiModel = await ValidateMinimalApiSettingsAsync(settings, projectService);
                    statusContext.Status = "DONE\n\n";
                    return apiModel;
                });

            var templatesAdded = await AnsiConsole.Status()
                .WithSpinner()
                .Start("Running T4 generator for templates!", async statusContext =>
                {
                    var success = await ExecuteTemplates(projectService, minimalApiModel);
                    statusContext.Status = "DONE\n\n";
                    return success;
                });

            var isMinimalApp = await ProjectModifierHelper.IsMinimalApp(_codeService);
            var useTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(_codeService);
            CodeChangeOptions options = new CodeChangeOptions()
            {
                IsMinimalApp = isMinimalApp,
                UsingTopLevelsStatements = useTopLevelsStatements
            };

            var programCsUpdated = await UpdateProgramCs(minimalApiModel, options);
            if (projectService.ProjectService != null)
            {
                CleanupProject(projectService.ProjectService);
            }
            
            return templatesAdded && programCsUpdated ? 0 : -1;
        }

        internal async Task<bool> ExecuteTemplates(AspNetProjectService projectService, MinimalApiModel minimalApiModel)
        {
            var projectPath = _appSettings.Workspace().InputPath;
            var allT4Templates = TemplateFoldersUtilities.GetAllT4Templates(["MinimalApi"]);
            ITextTransformation? textTransformation = null;
            string? t4TemplatePath = null;
            if (minimalApiModel.EfScenario)
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApiEf.tt", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApi.tt", StringComparison.OrdinalIgnoreCase));
            }

            textTransformation = GetMinimalApiTransformation(t4TemplatePath);
            if (textTransformation is null)
            {
                throw new Exception($"Unable to process T4 template '{t4TemplatePath}' correctly");
            }

            var templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , minimalApiModel }
            };

            var t4TemplateName = Path.GetFileNameWithoutExtension(t4TemplatePath);
            var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
            if (!string.IsNullOrEmpty(templatedString))
            {
                return await AddEndpointsMethod(templatedString, minimalApiModel, projectService);
            }

            return false;
        }

        internal async Task<MinimalApiModel> ValidateMinimalApiSettingsAsync(MinimalApiSettings settings, AspNetProjectService projectService)
        {
            MinimalApiModel scaffoldingModel = new();
            var allClasses = await projectService.CodeService.GetAllClassSymbolsAsync();
            var allDocs = await projectService.CodeService.GetAllDocumentsAsync();

            var modelClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(settings.Model, StringComparison.OrdinalIgnoreCase));
            //check for model class, throw if not found
            if (string.IsNullOrEmpty(settings.Model) || modelClassSymbol is null)
            {
                throw new Exception("need a valid value --model class");
            }
            else
            {
                scaffoldingModel.ModelTypeName = settings.Model;
                scaffoldingModel.ModelNamespace = modelClassSymbol.ContainingNamespace.ToDisplayString();
                scaffoldingModel.MethodName = $"Map{settings.Model}Endpoints";
            }

            if (!string.IsNullOrEmpty(settings.Endpoints))
            {
                scaffoldingModel.EndpointsFileName = StringUtil.EnsureCsExtension(settings.Endpoints);
                var existingEndpointsDoc = allDocs.FirstOrDefault(x => x.Name.Equals(scaffoldingModel.EndpointsFileName, StringComparison.OrdinalIgnoreCase) || x.Name.EndsWith(scaffoldingModel.EndpointsFileName, StringComparison.OrdinalIgnoreCase));
                if (existingEndpointsDoc != null) 
                {
                    scaffoldingModel.EndpointsPath = existingEndpointsDoc.FilePath ?? existingEndpointsDoc.Name;
                }
            }

            var dbContextClassName = settings.DataContext;
            if (!string.IsNullOrEmpty(dbContextClassName))
            {
                scaffoldingModel.EfScenario = true;
                var existingDbContextClass = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
                if (existingDbContextClass != null)
                {
                    //using an existing dbcontext model, analyzing DbContext
                    var efDbContextProperties = AnsiConsole.Status()
                        .WithSpinner()
                        .Start($"Analyzing existing DbContext {dbContextClassName}!", statusContext =>
                        {
                            return EfDbContextHelpers.GetEfDbContextProperties(existingDbContextClass, modelClassSymbol);
                        });

                    scaffoldingModel.DbContextClassName = existingDbContextClass.Name;
                    scaffoldingModel.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.ToString();
                }
                else
                {
                    //scaffoldingModel.DbContextClassName = ValidateDbContextName(dbContextClassName);
                }

                if (string.IsNullOrEmpty(settings.DatabaseProvider) || !EfConstants.AllDbProviders.ContainsKey(settings.DatabaseProvider))
                {
                    settings.DatabaseProvider = EfConstants.SqlServer;
                }
            }
            else
            {
                scaffoldingModel.EfScenario = false;
            }

            scaffoldingModel.OpenAPI = settings.Open;
            scaffoldingModel.RelativeFolderPath = settings.RelativeFolderPath;
            //check for relative folder path, if not, create it at base of the project
            //perform t4 templating (use namespace from relative folder path is provided
            var dbContextName = settings.DataContext;
            return scaffoldingModel;
        }

        internal string GetEndpointsOutputPath(MinimalApiModel templateModel)
        {
            var endpointsPath = string.Empty;
            var endpointsFileName = templateModel.EndpointsFileName ?? $"{templateModel.ModelTypeName}Endpoints.cs";
            var baseProjectPath = Path.GetDirectoryName(_appSettings.Workspace().InputPath);
            if (!string.IsNullOrEmpty(baseProjectPath))
            {
                var relativeFolderPath = templateModel.RelativeFolderPath;
                endpointsPath = Path.Combine(baseProjectPath, relativeFolderPath ?? string.Empty, $"{endpointsFileName}");
                endpointsPath = StringUtil.GetUniqueFilePath(endpointsPath);
            }

            return endpointsPath;
        }

        internal static ITextTransformation? GetMinimalApiTransformation(string? templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                return null;
            }

            var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
            ITextTransformation? transformation = null;

            switch (Path.GetFileName(templatePath))
            {
                case "MinimalApi.tt":
                    transformation = new Templates.MinimalApi.MinimalApi() { Host = host };
                    break;
                case "MinimalApiEf.tt":
                    transformation = new Templates.MinimalApi.MinimalApi() { Host = host };
                    break;
            }

            if (transformation != null)
            {
                transformation.Session = host.CreateSession();
            }

            return transformation;
        }

        internal async Task<bool> AddEndpointsMethod(string membersBlockText, MinimalApiModel templateModel, AspNetProjectService projectService)
        {
            if (!string.IsNullOrEmpty(membersBlockText) &&
                templateModel != null)
            {
                CompilationUnitSyntax? docRoot = null;
                ClassDeclarationSyntax? classDeclaration = null;
                var endpointsClassName = $"{templateModel.ModelTypeName}Endpoints";
                Document? endpointsDocument = await GetOrCreateEndpointsDocumentAsync(templateModel, projectService);
                //if document is still null, throw exception, we were unable to create a document
                if (endpointsDocument is null)
                {
                    throw new Exception("");
                }
                
                docRoot = await endpointsDocument.GetSyntaxRootAsync() as CompilationUnitSyntax;
                if (docRoot is not null)
                {
                    var classNode = docRoot.DescendantNodes().FirstOrDefault(
                        node => node is ClassDeclarationSyntax classDeclarationSyntax2 &&
                                classDeclarationSyntax2.Identifier.ValueText.Contains(endpointsClassName) &&
                                classDeclarationSyntax2.IsStaticClass());
                    if (classNode is null)
                    {
                        classDeclaration = ClassModifier.GetNewStaticClassDeclaration(endpointsClassName);
                        docRoot = docRoot.AddMembers(classDeclaration);
                    }
                    else
                    {
                        classDeclaration = classNode as ClassDeclarationSyntax;
                    }
                }

                if (classDeclaration is null || docRoot is null)
                {
                    throw new Exception("class or syntax root for Document should not be null");
                }

                //add new class declaration and replace it ontop of the old class (in the syntax root)
                var modifiedClass = classDeclaration.AddMembers(
                   SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(membersBlockText)).WithLeadingTrivia(SyntaxFactory.Tab));
                docRoot = docRoot.ReplaceNode(classDeclaration, modifiedClass);
                if (!string.IsNullOrEmpty(templateModel.ModelNamespace))
                {
                    docRoot = ClassModifier.AddUsings(docRoot, [templateModel.ModelNamespace]);
                }

                //update Document with new CompilationUnitSyntax
                endpointsDocument = endpointsDocument.WithSyntaxRoot(docRoot);
                endpointsDocument = await CodeAnalysis.Formatting.Formatter.FormatAsync(endpointsDocument);
                return _codeService.TryApplyChanges(endpointsDocument.Project.Solution);
            }

            return false;
        }

        internal async Task<Document?> GetOrCreateEndpointsDocumentAsync(MinimalApiModel templateModel, AspNetProjectService projectService)
        {
            var solution = (await projectService.CodeService.GetWorkspaceAsync())?.CurrentSolution;
            var roslynProject = solution?.GetProject(_appSettings.Workspace().InputPath);
            Document? endpointsDocument = null;
            var endpointsClassName = $"{templateModel.ModelTypeName}Endpoints";
            if (!string.IsNullOrEmpty(templateModel.EndpointsPath))
            {
                var allDocuments = await projectService.CodeService.GetAllDocumentsAsync();
                endpointsDocument = allDocuments.FirstOrDefault(x =>
                    x.Name.Equals(templateModel.EndpointsPath, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(x.FilePath) && x.FilePath.Equals(templateModel.EndpointsPath, StringComparison.OrdinalIgnoreCase)));
            }

            //create new endpoints document
            if (endpointsDocument is null)
            {
                var endpointsCompilationSyntax = ClassModifier.GetNewStaticCompilationUnit(endpointsClassName);
                var newEndpointsFilePath = GetEndpointsOutputPath(templateModel);
                endpointsDocument = roslynProject?.AddDocument(templateModel.EndpointsFileName, endpointsCompilationSyntax, filePath: newEndpointsFilePath);
                _excludeList.Add(templateModel.EndpointsFileName);
            }

            return endpointsDocument;
        }

        internal async Task<bool> UpdateProgramCs(MinimalApiModel templateModel, CodeChangeOptions codeChangeOptions)
        {
            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("minimalApiChanges.json", Assembly.GetExecutingAssembly());
            config = EditConfigForMinimalApi(config, templateModel, codeChangeOptions);
            var projectModifier = new ProjectModifier(
                _appSettings,
                _codeService,
                _logger,
                codeChangeOptions,
                config);
            return await projectModifier.RunAsync();
        }

        internal CodeModifierConfig? EditConfigForMinimalApi(CodeModifierConfig? configToEdit, MinimalApiModel templateModel, CodeChangeOptions codeChangeOptions)
        {
            if (configToEdit is null)
            {
                return null;
            }

            var programCsFile = configToEdit.Files?.FirstOrDefault(x => !string.IsNullOrEmpty(x.FileName) && x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
            if (programCsFile != null && programCsFile.Methods != null && programCsFile.Methods.Count != 0)
            {
                //should only include one change to add "app.Map%MODEL%Method to the Program.cs file. Check the minimalApiChanges.json for more info.
                var addMethodMapping = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
                var addMethodMappingChange = addMethodMapping?.CodeChanges?.FirstOrDefault();
                if (!codeChangeOptions.UsingTopLevelsStatements && addMethodMappingChange != null)
                {
                    addMethodMappingChange = DocumentBuilder.AddLeadingTriviaSpaces(addMethodMappingChange, spaces: 12);
                }

                if (addMethodMappingChange != null)
                {
                    addMethodMappingChange.Block = string.Format(addMethodMappingChange.Block, templateModel.MethodName);
                }
            }

            return configToEdit;
        }

        internal void CleanupProject(IProjectService projectService)
        {
            ClassModifier.RemoveCompileIncludes(_appSettings.Workspace().InputPath, _excludeList);
        }

        public class MinimalApiSettings : CommandSettings
        {
            [CommandOption("--project <PROJECT>")]
            public string Project { get; set; } = default!;

            [CommandOption("--model <MODEL>")]
            public string Model { get; set; } = default!;

            [CommandOption("--endpoints <ENDPOINTS>")]
            public string Endpoints { get; set; } = default!;

            [CommandOption("--dataContext <DATACONTEXT>")]
            public string DataContext { get; set; } = default!;

            [CommandOption("--relativeFolderPath <FOLDERPATH>")]
            public string RelativeFolderPath { get; set; } = default!;

            [CommandOption("--open")]
            public bool Open { get; set; }

            [CommandOption("--dbProvider <DBPROVIDER>")]
            public string DatabaseProvider { get; set; } = default!;
        }
    }
}
