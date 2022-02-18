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
        private readonly IEnumerable<string> _files;
        private readonly IConsoleLogger _consoleLogger;

        public ProjectModifier(ProvisioningToolOptions toolOptions, IEnumerable<string> files, IConsoleLogger consoleLogger)
        {
            _toolOptions = toolOptions ?? throw new ArgumentNullException(nameof(toolOptions));
            _files = files;
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

            // Initialize Microsoft.Build assemblies
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            // Initialize CodeAnalysis.Project wrapper
            CodeAnalysis.Project project = await CodeAnalysisHelper.LoadCodeAnalysisProjectAsync(_toolOptions.ProjectFilePath, _files);
            if (project is null)
            {
                return;
            }

            var isMinimalApp = await ProjectModifierHelper.IsMinimalApp(project);
            CodeChangeOptions options = new CodeChangeOptions
            {
                MicrosoftGraph = _toolOptions.CallsGraph,
                DownstreamApi = _toolOptions.CallsDownstreamApi,
                IsMinimalApp = isMinimalApp
            };

            // Go through all the files, make changes using DocumentBuilder.
            var filteredFiles = codeModifierConfig.Files.Where(f => ProjectModifierHelper.FilterOptions(f.Options, options));
            foreach (var file in filteredFiles)
            {
                await HandleCodeFileAsync(file, project, options, codeModifierConfig.Identifier);
            }
        }

        private CodeModifierConfig? GetCodeModifierConfig()
        {
            if (string.IsNullOrEmpty(_toolOptions.ProjectType))
            {
                return null;
            }

            if (CodeModifierConfigPropertyInfo is null)
            {
                return null;
            }

            byte[] content = (CodeModifierConfigPropertyInfo.GetValue(null) as byte[])!;
            CodeModifierConfig? codeModifierConfig = ReadCodeModifierConfigFromFileContent(content);
            if (codeModifierConfig is null)
            {
                throw new FormatException($"Resource file { CodeModifierConfigPropertyInfo.Name } could not be parsed. ");
            }

            if (!codeModifierConfig.Identifier.Equals(_toolOptions.ProjectTypeIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return codeModifierConfig;
        }

        private PropertyInfo? CodeModifierConfigPropertyInfo
        {
            get
            {
                var identifier = _toolOptions.ProjectTypeIdentifier.Replace('-', '_');
                var propertyInfo = AppProvisioningTool.Properties.FirstOrDefault(p => p.Name.StartsWith("cm") && p.Name.EndsWith(identifier));
                return propertyInfo;
            }
        }

        private CodeModifierConfig? ReadCodeModifierConfigFromFileContent(byte[] fileContent)
        {
            try
            {
                string jsonText = Encoding.UTF8.GetString(fileContent);
                return JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (Exception e)
            {
                _consoleLogger.LogMessage($"Error parsing Code Modifier Config for project type { _toolOptions.ProjectType }, exception: { e.Message }");
                return null;
            }
        }

        private async Task HandleCodeFileAsync(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options, string identifier)
        {
            if (!string.IsNullOrEmpty(file.AddFilePath))
            {
                AddFile(file, identifier);
            }
            else
            {
                switch (file.Extension)
                {
                    case "cs":
                        await ModifyCsFile(file, project, options);
                        break;
                    case "cshtml":
                        await ModifyCshtmlFile(file, project, options);
                        break;
                    case "razor":
                    case "html":
                        await ApplyTextReplacements(file, project, options);
                        break;
                }
            }
        }

        /// <summary>
        /// Determines if specified file exists, and if not then creates the 
        /// file based on template stored in AppProvisioningTool.Properties
        /// then adds file to the project
        /// </summary>
        /// <param name="file"></param>
        /// <param name="identifier"></param>
        /// <exception cref="FormatException"></exception>
        private void AddFile(CodeFile file, string identifier)
        {
            var filePath = Path.Combine(_toolOptions.ProjectPath, file.AddFilePath);
            if (File.Exists(filePath))
            {
                return; // File exists, don't need to create
            }

            var codeFileString = GetCodeFileString(file, identifier);

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir))
            {
                Directory.CreateDirectory(fileDir);
                File.WriteAllText(filePath, codeFileString);
            }
        }

        private string GetCodeFileString(CodeFile file, string identifier)
        {
            var propertyInfo = GetPropertyInfo(file.FileName, identifier);
            if (propertyInfo is null)
            {
                throw new FormatException($"Resource file for {file.FileName} could not be found. ");
            }

            byte[] content = (propertyInfo.GetValue(null) as byte[])!;
            string codeFileString = Encoding.UTF8.GetString(content);
            if (string.IsNullOrEmpty(codeFileString))
            {
                throw new FormatException($"Resource file for {file.FileName} could not be parsed. ");
            }

            return codeFileString;
        }

        private PropertyInfo? GetPropertyInfo(string fileName, string identifier)
        {
            return AppProvisioningTool.Properties.Where(
                p => p.Name.StartsWith("add")
                && p.Name.Contains(identifier.Replace('-', '_')) // Resource files cannot have '-' (dash character)
                && p.Name.Contains(fileName.Replace('.', '_'))) // Resource files cannot have '.' (period character)
                .FirstOrDefault();
        }

        internal async Task ModifyCsFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            if (file.FileName.Equals("Startup.cs"))
            {
                // Startup class file name may be different
                file.FileName = await ProjectModifierHelper.GetStartupClass(project) ?? file.FileName;
            }

            var fileDoc = project.Documents.Where(d => d.Name.Equals(file.FileName)).FirstOrDefault();
            if (fileDoc is null || string.IsNullOrEmpty(fileDoc.FilePath))
            {
                return;
            }

            // get the file document to get the document root for editing.
            DocumentEditor documentEditor = await DocumentEditor.CreateAsync(fileDoc);
            if (documentEditor is null)
            {
                return;
            }

            DocumentBuilder documentBuilder = new DocumentBuilder(documentEditor, file, _consoleLogger);
            var modifiedRoot = ModifyRoot(documentBuilder, options, file);
            if (modifiedRoot != null)
            {
                documentEditor.ReplaceNode(documentEditor.OriginalRoot, modifiedRoot);
                await documentBuilder.WriteToClassFileAsync(fileDoc.FilePath);
            }
        }

        /// <summary>
        /// Modifies root if there any applicable changes
        /// </summary>
        /// <param name="documentBuilder"></param>
        /// <param name="options"></param>
        /// <param name="file"></param>
        /// <returns>modified root if there are changes, else null</returns>
        private static SyntaxNode? ModifyRoot(DocumentBuilder documentBuilder, CodeChangeOptions options, CodeFile file)
        {
            var root = documentBuilder.AddUsings(options);
            if (file.FileName.Equals("Program.cs") && file.Methods.TryGetValue("Global", out var globalChanges))
            {
                
                var filteredChanges = ProjectModifierHelper.FilterCodeSnippets(globalChanges.CodeChanges, options);
                var updatedIdentifer = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(root.Members);
                if (updatedIdentifer.HasValue)
                {
                    (string oldValue, string newValue) = updatedIdentifer.Value;
                    filteredChanges = ProjectModifierHelper.UpdateVariables(filteredChanges, oldValue, newValue);
                }

                var updatedRoot = DocumentBuilder.ApplyChangesToMethod(root, filteredChanges);
                return updatedRoot;
            }

            else
            {
                var namespaceNode = root?.Members.OfType<BaseNamespaceDeclarationSyntax>()?.FirstOrDefault();

                string className = ProjectModifierHelper.GetClassName(file.FileName);
                // get classNode. All class changes are done on the ClassDeclarationSyntax and then that node is replaced using documentEditor.

                if (namespaceNode?.DescendantNodes().FirstOrDefault(node =>
                        node is ClassDeclarationSyntax cds &&
                        cds.Identifier.ValueText.Contains(className)) is ClassDeclarationSyntax classNode)
                {
                    var modifiedClassDeclarationSyntax = classNode;

                    //add class properties
                    modifiedClassDeclarationSyntax = documentBuilder.AddProperties(modifiedClassDeclarationSyntax, options);
                    //add class attributes
                    modifiedClassDeclarationSyntax = documentBuilder.AddClassAttributes(modifiedClassDeclarationSyntax, options);

                    modifiedClassDeclarationSyntax = ModifyMethods(modifiedClassDeclarationSyntax, documentBuilder, file.Methods, options);
                    
                    //add code snippets/changes.

                    //replace class node with all the updates.
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                    root = root.ReplaceNode(classNode, modifiedClassDeclarationSyntax);
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                }
            }

            return root;
        }

        private static ClassDeclarationSyntax ModifyMethods(ClassDeclarationSyntax classNode, DocumentBuilder documentBuilder, Dictionary<string, Method> methods, CodeChangeOptions options)
        {
            foreach ((string methodName, Method methodChanges) in methods)
            {
                if (methodChanges == null)
                {
                    continue;
                }

                var methodNode = documentBuilder.GetOriginalMethod(classNode, methodName, methodChanges);
                if (methodNode is null)
                {
                    continue;
                }

                var updatedMethodNode = documentBuilder.GetModifiedMethod(methodNode, methodChanges, options);
                if (updatedMethodNode != null)
                {
                    classNode = classNode.ReplaceNode(methodNode, updatedMethodNode);
                }
            }

            return classNode;
        }

        internal async Task ModifyCshtmlFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            var fileDoc = project.Documents.FirstOrDefault(d => d.Name.EndsWith(file.FileName));
            if (fileDoc is null || file.Methods is null || !file.Methods.TryGetValue("Global", out var globalMethod))
            {
                return;
            }

            var filteredCodeChanges = globalMethod.CodeChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
            if (!filteredCodeChanges.Any())
            {
                return;
            }

            // add code snippets/changes.
            var editedDocument = await ProjectModifierHelper.ModifyDocumentText(fileDoc, filteredCodeChanges);
            if (editedDocument != null)
            {
                //replace the document
                await ProjectModifierHelper.UpdateDocument(editedDocument, _consoleLogger);
            }
        }

        /// <summary>
        /// Updates .razor and .html files via string replacement
        /// </summary>
        /// <param name="file"></param>
        /// <param name="project"></param>
        /// <param name="toolOptions"></param>
        /// <returns></returns>
        internal async Task ApplyTextReplacements(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions toolOptions)
        {
            var document = project.Documents.FirstOrDefault(d => d.Name.EndsWith(file.FileName));
            if (document is null)
            {
                return;
            }

            var replacements = file.Replacements.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, toolOptions));
            if (!replacements.Any())
            {
                return;
            }

            var editedDocument = await ProjectModifierHelper.ModifyDocumentText(document, replacements);
            if (editedDocument != null)
            {
                await ProjectModifierHelper.UpdateDocument(editedDocument, _consoleLogger);
            }
        }
    }
}
