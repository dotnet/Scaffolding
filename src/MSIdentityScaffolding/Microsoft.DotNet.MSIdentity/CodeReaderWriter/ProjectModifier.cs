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
using Microsoft.DotNet.MSIdentity.Tool;

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
                        string className = GetClassName(fileName);

                        //if the file we are modifying is Startup.cs, use Program.cs to find the correct file to edit.
                        if (!string.IsNullOrEmpty(file.FileName) && file.FileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = await GetStarupClass(project);
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
                                var namespaceNode = docRoot?.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();

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
                                    //adding usings
                                    documentBuilder.AddUsings();
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

        internal string GetClassName(string? className)
        {
            string formattedClassName = string.Empty;
            if (!string.IsNullOrEmpty(className))
            {
                string[] blocks = className.Split(".cs");
                if (blocks.Length == 1)
                {
                    return blocks[0];
                }
            }
            return formattedClassName;
        }

        //Get Startup class name from CreateHostBuilder in Program.cs. If Program.cs is not being used, method
        //will bail out.
        internal async Task<string?> GetStarupClass(CodeAnalysis.Project project)
        {
            var programFilePath = Directory.EnumerateFiles(_toolOptions.ProjectPath, "Program.cs").FirstOrDefault();
            if (!string.IsNullOrEmpty(programFilePath))
            {
                var programDoc = project.Documents.Where(d => d.Name.Equals(programFilePath)).FirstOrDefault();
                var startupClassName = await GetStartupClassName(programDoc);
                string className = startupClassName;
                var startupFilePath = string.Empty;
                if (!string.IsNullOrEmpty(startupClassName))
                {
                    return string.Concat(startupClassName, ".cs");
                }
            }
            return string.Empty;
        }

        internal async Task<string> GetStartupClassName(Document? programDoc)
        {
            if (programDoc != null && await programDoc.GetSyntaxRootAsync() is CompilationUnitSyntax root)
            {
                var namespaceNode = root.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
                var programClassNode = namespaceNode?.DescendantNodes()
                    .Where(node =>
                        node is ClassDeclarationSyntax cds &&
                        cds.Identifier
                           .ValueText.Contains("Program"))
                    .First();

                var nodes = programClassNode?.DescendantNodes();
                var useStartupNode = programClassNode?.DescendantNodes()
                    .Where(node =>
                        node is MemberAccessExpressionSyntax maes &&
                        maes.ToString()
                            .Contains("webBuilder.UseStartup"))
                    .First();

                var useStartupTxt = useStartupNode?.ToString();
                if (!string.IsNullOrEmpty(useStartupTxt))
                {
                    int startIndex = useStartupTxt.IndexOf("<");
                    int endIndex = useStartupTxt.IndexOf(">");
                    if (startIndex > -1 && endIndex > startIndex)
                    {
                        return useStartupTxt.Substring(startIndex + 1, endIndex - startIndex - 1);
                    }
                }
            }
            return string.Empty;
        }
    }
}
