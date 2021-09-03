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

                    //Go through all the files, make changes using DocumentBuilder.
                    foreach (var file in codeModifierConfig.Files)
                    {
                        var fileName = file.FileName;
                        string className = ProjectModifierHelper.GetClassName(fileName);

                        //if the file we are modifying is Startup.cs, use Program.cs to find the correct file to edit.
                        if (!string.IsNullOrEmpty(file.FileName) && file.FileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = await ProjectModifierHelper.GetStartupClass(_toolOptions.ProjectPath, project);
                        }

                        if (!string.IsNullOrEmpty(fileName))
                        {
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
                    }
                }
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
