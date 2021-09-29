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

                    //Go through all the files, make changes using DocumentBuilder.
                    foreach (var file in codeModifierConfig.Files)
                    {
                        var fileName = file.FileName;

                        //if the file we are modifying is Startup.cs, use Program.cs to find the correct file to edit.
                        if (!string.IsNullOrEmpty(fileName) && fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = await ProjectModifierHelper.GetStartupClass(_toolOptions.ProjectPath, project);
                        }

                        if (!string.IsNullOrEmpty(fileName) && !fileName.Equals("Program.cs"))
                        {
                            await ModifyCsFile(fileName, file, project);
                        }
                        // if file is Program.cs
                        else if (!string.IsNullOrEmpty(fileName) && fileName.Equals("Program.cs"))
                        {
                            //only modify Program.cs file if its a minimal hosting app (as we made changes to the Startup.cs file).
                            if (isMinimalApp)
                            {
                                await ModifyProgramCs(file, project);
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
        internal async Task ModifyProgramCs(CodeFile programCsFile, CodeAnalysis.Project project)
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
                                //format CodeChange.Block and CodeChange.Parent for any variables or parameters.
                                change.Block = ProjectModifierHelper.FormatGlobalStatement(change.Block, variableDict);
                                if (!string.IsNullOrEmpty(change.Parent))
                                {
                                    change.Parent = ProjectModifierHelper.FormatGlobalStatement(change.Parent, variableDict);
                                }
                                newRoot = DocumentBuilder.AddGlobalStatements(change, newRoot);
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

        internal async Task ModifyCsFile(string fileName, CodeFile file, CodeAnalysis.Project project)
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
                //adding usings
                var newRoot = documentBuilder.AddUsings();
                var namespaceNode = newRoot?.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();

                //get classNode. All class changes are done on the ClassDeclarationSyntax and then that node is replaced using documentEditor.
                var classNode = namespaceNode?
                    .DescendantNodes()?
                    .Where(node =>
                        node is ClassDeclarationSyntax cds &&
                        cds.Identifier.ValueText.Contains(className))
                    .FirstOrDefault();

                if (classNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    var modifiedClassDeclarationSyntax = classDeclarationSyntax;

                    //add class properties
                    modifiedClassDeclarationSyntax = documentBuilder.AddProperties(modifiedClassDeclarationSyntax);
                    //add class attributes
                    modifiedClassDeclarationSyntax = documentBuilder.AddClassAttributes(modifiedClassDeclarationSyntax);

                    //add code snippets/changes.
                    if (file.Methods != null && file.Methods.Any())
                    {
                        modifiedClassDeclarationSyntax = documentBuilder.AddCodeSnippets(file, modifiedClassDeclarationSyntax); ;
                    }
                    //replace class node with all the updates.
                    documentEditor.ReplaceNode(classDeclarationSyntax, modifiedClassDeclarationSyntax);
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
