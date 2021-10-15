using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    internal class ProjectModifier
    {
        private readonly ProvisioningToolOptions _toolOptions;
        private readonly ApplicationParameters _appParameters;
        private readonly IConsoleLogger _consoleLogger;

        public ProjectModifier(ApplicationParameters applicationParameters, ProvisioningToolOptions toolOptions, IConsoleLogger consoleLogger)
        {
            _toolOptions = toolOptions ?? throw new ArgumentNullException(nameof(toolOptions));
            _appParameters = applicationParameters ?? throw new ArgumentNullException(nameof(applicationParameters));
            _consoleLogger = consoleLogger ?? throw new ArgumentNullException(nameof(consoleLogger));
        }

        /// <summary>
        /// Added "Microsoft identity platform" auth to base or empty C# .NET Core 3.1, .NET 5 and above projects.
        /// Includes adding PackageReferences, modifying Startup.cs, and Layout.cshtml(not yet) changes.
        /// </summary>
        /// <param name="projectType"></param>
        /// <returns></returns>
        public async Task AddAuthCodeAsync()
        {
            if (!string.IsNullOrEmpty(_toolOptions.ProjectFilePath))
            {
                CodeModifierConfig? codeModifierConfig = GetCodeModifierConfig();
                //Get CodeModifierConfig from CodeModifierConfigs folder.
                if (codeModifierConfig != null &&
                    codeModifierConfig.Files != null)
                {
                    //Initialize CodeAnalysis.Project wrapper
                    CodeAnalysis.Project project = await CodeAnalysisHelper.LoadCodeAnalysisProjectAsync(_toolOptions.ProjectFilePath);

                    //is minimal project
                    var isMinimalApp = await ProjectModifierHelper.IsMinimalApp(project);
                    CodeChangeOptions options = new CodeChangeOptions
                    {
                        MicrosoftGraph = _toolOptions.CallsGraph,
                        DownstreamApi = _toolOptions.CallsDownstreamApi
                    };
                    var filteredFiles = codeModifierConfig.Files.Where(f => DocumentBuilder.FilterOptions(f.Options, options));
                    //Go through all the files, make changes using DocumentBuilder.
                    foreach (var file in filteredFiles)
                    {
                        var fileName = file.FileName;
                        
                        //if the file we are modifying is Startup.cs, use Program.cs to find the correct file to edit.
                        if (!string.IsNullOrEmpty(fileName) && fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = await ProjectModifierHelper.GetStartupClass(_toolOptions.ProjectPath, project);
                        }

                        if (!string.IsNullOrEmpty(fileName) && !fileName.Equals("Program.cs"))
                        {
                            await ModifyCsFile(fileName, file, project, options);
                        }
                        // if file is Program.cs
                        else if (!string.IsNullOrEmpty(fileName) && fileName.Equals("Program.cs"))
                        {
                            //only modify Program.cs file if its a minimal hosting app (as we made changes to the Startup.cs file).
                            if (isMinimalApp)
                            {
                                await ModifyProgramCs(file, project, _toolOptions);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Modify Program.cs file to add changes specified in the CodeFile.
        /// </summary>
        /// <param name="programCsFile"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        internal async Task ModifyProgramCs(CodeFile programCsFile, CodeAnalysis.Project project, ProvisioningToolOptions toolOptions)
        {
            string? programCsFilePath = Directory.EnumerateFiles(_toolOptions.ProjectPath, "Program.cs", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrEmpty(programCsFilePath))
            {
                var programDocument = project.Documents.Where(d => d.Name.Equals(programCsFilePath)).FirstOrDefault();
                DocumentEditor documentEditor = await DocumentEditor.CreateAsync(programDocument);
                DocumentBuilder documentBuilder = new DocumentBuilder(documentEditor, programCsFile, _consoleLogger);
                var docRoot = documentEditor.OriginalRoot as CompilationUnitSyntax;
                if (docRoot != null && programDocument != null)
                {
                    //get builder variable
                    var variableDict = ProjectModifierHelper.GetBuilderVariableIdentifier(docRoot.Members);
                    //add usings
                    var newRoot = documentBuilder.AddUsings();
                    //add code snippets/changes.
                    if (programCsFile.Methods != null && programCsFile.Methods.Any())
                    {
                        var globalChanges = programCsFile.Methods.TryGetValue("Global", out var globalMethod);
                        if (globalMethod != null)
                        {
                            foreach (var change in globalMethod.CodeChanges)
                            {
                                //Modify CodeSnippet to have correct variable identifiers present.
                                var formattedChange = ProjectModifierHelper.FormatCodeSnippet(change, variableDict);

                                //if the application calls Microsoft Graph
                                if (toolOptions.CallsGraph)
                                {
                                    if (formattedChange.Options != null)
                                    {
                                        //add code change if MicrosoftGraph condition is present or if DownstreamApi is not present.
                                        if (formattedChange.Options.Contains(CodeChangeOptionStrings.MicrosoftGraph) ||
                                            !formattedChange.Options.Contains(CodeChangeOptionStrings.DownstreamApi))
                                        {
                                            newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                                        }
                                    }
                                    else
                                    {
                                        newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                                    }
                                }

                                //if the application calls a Downstream API
                                if (toolOptions.CallsDownstreamApi)
                                {
                                    if (formattedChange.Options != null)
                                    {
                                        //add code change if DownstreamApi condition is present or if MicrosoftGraph is not present.
                                        if (formattedChange.Options.Contains(CodeChangeOptionStrings.DownstreamApi) ||
                                            !formattedChange.Options.Contains(CodeChangeOptionStrings.MicrosoftGraph))
                                        {
                                            newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                                        }
                                    }
                                    //if no Options are present, add the code changes.
                                    else
                                    {
                                        newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                                    }
                                }

                                //if the application calls neither Microsoft Graph or a Downstream API
                                if (!toolOptions.CallsGraph && !toolOptions.CallsDownstreamApi)
                                {
                                    //if no Options are present, or if they are present, don't contain MicrosoftGraph or DownstreamAPI, add the code changes.
                                    if (formattedChange.Options == null ||
                                        !formattedChange.Options.Contains(CodeChangeOptionStrings.MicrosoftGraph) ||
                                        !formattedChange.Options.Contains(CodeChangeOptionStrings.DownstreamApi))
                                    {
                                        newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                                    }
                                }
                            }
                        }
                    }
                    //replace root node with all the updates.
                    documentEditor.ReplaceNode(docRoot, newRoot);
                    //write to Program.cs file
                    await documentBuilder.WriteToClassFileAsync(programDocument.Name, programCsFilePath);
                }
            }
        }

        internal async Task ModifyCsFile(string fileName, CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            string className = ProjectModifierHelper.GetClassName(fileName);
            string? filePath = Directory.EnumerateFiles(_toolOptions.ProjectPath, fileName, SearchOption.AllDirectories).FirstOrDefault();
            //get the file document to get the document root for editing.

            if (!string.IsNullOrEmpty(filePath))
            {
                var fileDoc = project.Documents.Where(d => d.Name.Equals(filePath)).FirstOrDefault();
                DocumentEditor documentEditor = await DocumentEditor.CreateAsync(fileDoc);
                DocumentBuilder documentBuilder = new DocumentBuilder(documentEditor, file, _consoleLogger);
                var docRoot = documentEditor.OriginalRoot as CompilationUnitSyntax;
                var newRoot = documentBuilder.AddUsings();
                //adding usings
                var namespaceNode = newRoot?.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
                FileScopedNamespaceDeclarationSyntax? fileScopedNamespace = null;
                if (namespaceNode == null)
                {
                    fileScopedNamespace = newRoot?.Members.OfType<FileScopedNamespaceDeclarationSyntax>()?.FirstOrDefault();
                }
                
                //get classNode. All class changes are done on the ClassDeclarationSyntax and then that node is replaced using documentEditor.
                var classNode =
                    namespaceNode?.DescendantNodes()?.Where(node =>
                        node is ClassDeclarationSyntax cds &&
                        cds.Identifier.ValueText.Contains(className)).FirstOrDefault() ??
                    fileScopedNamespace?.DescendantNodes()?.Where(node =>
                        node is ClassDeclarationSyntax cds &&
                        cds.Identifier.ValueText.Contains(className)).FirstOrDefault();

                if (classNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    var modifiedClassDeclarationSyntax = classDeclarationSyntax;

                    //add class properties
                    modifiedClassDeclarationSyntax = documentBuilder.AddProperties(modifiedClassDeclarationSyntax, options);
                    //add class attributes
                    modifiedClassDeclarationSyntax = documentBuilder.AddClassAttributes(modifiedClassDeclarationSyntax, options);

                    //add code snippets/changes.
                    if (file.Methods != null && file.Methods.Any())
                    {
                        modifiedClassDeclarationSyntax = documentBuilder.AddCodeSnippets(modifiedClassDeclarationSyntax, options);
                        modifiedClassDeclarationSyntax = documentBuilder.EditMethodTypes(modifiedClassDeclarationSyntax, options);
                        modifiedClassDeclarationSyntax = documentBuilder.AddMethodParameters(modifiedClassDeclarationSyntax, options);   
                    }
                    //replace class node with all the updates.
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                    newRoot = newRoot.ReplaceNode(classDeclarationSyntax, modifiedClassDeclarationSyntax);
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                }

                if (docRoot != null && newRoot != null)
                {
                    documentEditor.ReplaceNode(docRoot, newRoot);
                }
                await documentBuilder.WriteToClassFileAsync(fileName, filePath);
            }
        }

        private CodeModifierConfig? GetCodeModifierConfig()
        {
            List<CodeModifierConfig> codeModifierConfigs = new List<CodeModifierConfig>();
            if (!string.IsNullOrEmpty(_toolOptions.ProjectType))
            {
                var properties = typeof(Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
               .Where(p => p.PropertyType == typeof(byte[]))
               .ToArray();

                foreach (PropertyInfo propertyInfo in properties)
                {
                    if (propertyInfo.Name.StartsWith("cm") && propertyInfo.Name.Contains(_toolOptions.ProjectType))
                    {
                        byte[] content = (propertyInfo.GetValue(null) as byte[])!;
                        CodeModifierConfig? projectDescription = ReadCodeModifierConfigFromFileContent(content);

                        if (projectDescription == null)
                        {
                            throw new FormatException($"Resource file { propertyInfo.Name } could not be parsed. ");
                        }
                        codeModifierConfigs.Add(projectDescription);
                    }
                }
            }

            var codeModifierConfig = codeModifierConfigs
                .Where(x => x.Identifier != null &&
                       x.Identifier.Equals(_toolOptions.ProjectTypeIdentifier, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

            // CodeModifierConfig, .csproj path cannot be null
            if (codeModifierConfig != null &&
                codeModifierConfig.Files != null &&
                codeModifierConfig.Files.Any())
            {
                return codeModifierConfig;
            }
            return null;
        }

        private CodeModifierConfig? ReadCodeModifierConfigFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
        }
    }
}
