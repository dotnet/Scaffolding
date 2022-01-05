using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
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
            if (string.IsNullOrEmpty(_toolOptions.ProjectFilePath))
            {
                return;
            }

            CodeModifierConfig? codeModifierConfig = GetCodeModifierConfig();
            if (codeModifierConfig is null || !codeModifierConfig.Files.Any())
            {
                return;
            }

            //Initialize Microsoft.Build assemblies
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            //Initialize CodeAnalysis.Project wrapper
            CodeAnalysis.Project project = await CodeAnalysisHelper.LoadCodeAnalysisProjectAsync(_toolOptions.ProjectFilePath);

            var isMinimalApp = await ProjectModifierHelper.IsMinimalApp(project);
            CodeChangeOptions options = new CodeChangeOptions
            {
                MicrosoftGraph = _toolOptions.CallsGraph,
                DownstreamApi = _toolOptions.CallsDownstreamApi,
                IsMinimalApp = isMinimalApp
            };

            var filteredFiles = codeModifierConfig.Files.Where(f => ProjectModifierHelper.FilterOptions(f.Options, options));
            //Go through all the files, make changes using DocumentBuilder.
            foreach (var file in filteredFiles)
            {
                await HandleCodeFileAsync(file, project, options);
            }
        }

        private async Task HandleCodeFileAsync(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            if (string.IsNullOrEmpty(file.FileName))
            {
                return;
            }

            if (!string.IsNullOrEmpty(file.AddFilePath))
            {
                AddFile(file);
                return;
            }

            var fileName = file.FileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase)
                    ? await ProjectModifierHelper.GetStartupClass(_toolOptions.ProjectPath, project)
                    : file.FileName;

            if (fileName.Equals("Program.cs"))
            {
                // only modify Program.cs file if it's a minimal hosting app (as we made changes to the Startup.cs file).
                if (options.IsMinimalApp)
                {
                    await ModifyProgramCs(file, project, options);
                }
            }
            else if (fileName.EndsWith(".cs"))
            {
                await ModifyCsFile(fileName, file, project, options);
            }
            else if (fileName.EndsWith(".cshtml"))
            {
                await ModifyCshtmlFile(fileName, file, project, options);
            }
            else if (fileName.EndsWith(".razor"))
            {
                await ModifyRazorFile(fileName, file, project, options);
            }
        }

        /// <summary>
        /// Determines if specified file exists, and if not then creates the 
        /// file based on template stored in AppProvisioningTool.Properties
        /// and adds file to the project
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="FormatException"></exception>
        private void AddFile(CodeFile file)
        {
            var filePath = Path.Combine(_toolOptions.ProjectPath, file.AddFilePath);
            if (File.Exists(filePath))
            {
                return; // File exists, don't need to create
            }

            // Resource names for addFiles prefixed with "add" and contain '_' in place of '.'
            // fileName: "ShowProfile.razor" -> resourceName: "add_ShowProfile_razor"
            var resourceName = file.FileName.Replace('.', '_');
            var propertyInfo = AppProvisioningTool.Properties.Where(
                p => p.Name.StartsWith("add") && p.Name.EndsWith(resourceName)).FirstOrDefault();

            if (propertyInfo is null)
            {
                return;
            }

            byte[] content = (propertyInfo.GetValue(null) as byte[])!;
            string codeFileString = Encoding.UTF8.GetString(content);
            if (string.IsNullOrEmpty(codeFileString))
            {
                throw new FormatException($"Resource file { propertyInfo.Name } could not be parsed. ");
            }

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir))
            {
                Directory.CreateDirectory(fileDir);
                File.WriteAllText(filePath, codeFileString);
            }
        }

        internal async Task ModifyRazorFile(string fileName, CodeFile file, CodeAnalysis.Project project, CodeChangeOptions toolOptions)
        {
            var document = project.Documents.Where(d => d.Name.EndsWith(fileName)).FirstOrDefault();
            if (document is null)
            {
                return;
            }

            var razorChanges = file?.RazorChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, toolOptions));
            var editedDocument = await ProjectModifierHelper.ModifyDocumentText(document, razorChanges);
            await ProjectModifierHelper.UpdateDocument(editedDocument, _consoleLogger);
        }

        internal async Task ModifyCshtmlFile(string fileName, CodeFile cshtmlFile, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            string? filePath = Directory.EnumerateFiles(_toolOptions.ProjectPath, fileName, SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrEmpty(filePath))
            {
                var fileDoc = project.Documents.Where(d => d.Name.Equals(filePath)).FirstOrDefault();
                if (fileDoc != null)
                {
                    //add code snippets/changes.
                    if (cshtmlFile.Methods != null && cshtmlFile.Methods.Any())
                    {
                        var globalChanges = cshtmlFile.Methods.TryGetValue("Global", out var globalMethod);
                        if (globalMethod != null)
                        {
                            var filteredCodeFiles = globalMethod.CodeChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
                            var editedDocument = await ProjectModifierHelper.ModifyDocumentText(fileDoc, filteredCodeFiles);
                            //replace the document
                            await ProjectModifierHelper.UpdateDocument(editedDocument, _consoleLogger);
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
        internal async Task ModifyProgramCs(CodeFile programCsFile, CodeAnalysis.Project project, CodeChangeOptions toolOptions)
        {
            string? programCsFilePath = Directory.EnumerateFiles(_toolOptions.ProjectPath, "Program.cs", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrEmpty(programCsFilePath))
            {
                var programDocument = project.Documents.Where(d => d.Name.Equals(programCsFilePath)).FirstOrDefault();
                DocumentEditor documentEditor = await DocumentEditor.CreateAsync(programDocument);
                DocumentBuilder documentBuilder = new DocumentBuilder(documentEditor, programCsFile, _consoleLogger);
                if (documentEditor.OriginalRoot is CompilationUnitSyntax docRoot && programDocument != null)
                {
                    //get builder variable
                    var variableDict = ProjectModifierHelper.GetBuilderVariableIdentifier(docRoot.Members);
                    //add usings
                    var newRoot = documentBuilder.AddUsings(toolOptions);
                    //add code snippets/changes.
                    if (programCsFile.Methods != null && programCsFile.Methods.Any())
                    {
                        var globalChanges = programCsFile.Methods.TryGetValue("Global", out var globalMethod);
                        if (globalMethod != null)
                        {
                            var filteredCodeFiles = globalMethod.CodeChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, toolOptions));
                            foreach (var change in filteredCodeFiles)
                            {
                                //Modify CodeSnippet to have correct variable identifiers present.
                                var formattedChange = ProjectModifierHelper.FormatCodeSnippet(change, variableDict);
                                newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                            }
                        }
                    }
                    //replace root node with all the updates.
                    documentEditor.ReplaceNode(docRoot, newRoot);
                    //write to Program.cs file
                    await documentBuilder.WriteToClassFileAsync(programCsFilePath);
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
                var newRoot = documentBuilder.AddUsings(options);
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

                if (documentEditor.OriginalRoot is CompilationUnitSyntax docRoot && newRoot != null)
                {
                    documentEditor.ReplaceNode(docRoot, newRoot);
                }

                await documentBuilder.WriteToClassFileAsync(filePath);
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
