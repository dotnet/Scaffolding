using System;
using System.IO;
using System.Linq;
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
        private readonly IConsoleLogger _consoleLogger;

        public ProjectModifier(ProvisioningToolOptions toolOptions, IConsoleLogger consoleLogger)
        {
            _toolOptions = toolOptions ?? throw new ArgumentNullException(nameof(toolOptions));
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
            CodeAnalysis.Project project = await CodeAnalysisHelper.LoadCodeAnalysisProjectAsync(_toolOptions.ProjectFilePath);
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
                await HandleCodeFileAsync(file, project, options);
            }
        }

        private CodeModifierConfig? GetCodeModifierConfig()
        {
            if (string.IsNullOrEmpty(_toolOptions.ProjectType))
            {
                return null;
            }

            var propertyInfo = AppProvisioningTool.Properties.Where(
                p => p.Name.StartsWith("cm") && p.Name.Contains(_toolOptions.ProjectType)).FirstOrDefault();
            if (propertyInfo is null)
            {
                return null;
            }

            byte[] content = (propertyInfo.GetValue(null) as byte[])!;
            CodeModifierConfig? codeModifierConfig = ReadCodeModifierConfigFromFileContent(content);
            if (codeModifierConfig is null)
            {
                throw new FormatException($"Resource file { propertyInfo.Name } could not be parsed. ");
            }

            if (!codeModifierConfig.Identifier.Equals(_toolOptions.ProjectTypeIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return codeModifierConfig;
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

        private async Task HandleCodeFileAsync(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            if (!string.IsNullOrEmpty(file.AddFilePath))
            {
                AddFile(file);
            }
            else if (file.FileName.EndsWith(".cs"))
            {
                await ModifyCsFile(file, project, options);
            }
            else if (file.FileName.EndsWith(".cshtml"))
            {
                await ModifyCshtmlFile(file, project, options);
            }
            else if (file.FileName.EndsWith(".razor"))
            {
                await ModifyRazorFile(file, project, options);
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
            // e.g. fileName: "ShowProfile.razor" -> resourceName: "add_ShowProfile_razor"
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

            //get the file document to get the document root for editing.
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
        private static CompilationUnitSyntax? ModifyRoot(DocumentBuilder documentBuilder, CodeChangeOptions options, CodeFile file)
        {
            var newRoot = documentBuilder.AddUsings(options);
            if (file.FileName.Equals("Program.cs"))
            {
                var variableDict = ProjectModifierHelper.GetBuilderVariableIdentifier(newRoot.Members);
                if (file.Methods.TryGetValue("Global", out var globalMethod))
                {
                    var filteredChanges = globalMethod.CodeChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
                    if (!filteredChanges.Any())
                    {
                        return null;
                    }

                    foreach (var change in filteredChanges)
                    {
                        //Modify CodeSnippet to have correct variable identifiers present.
                        var formattedChange = ProjectModifierHelper.FormatCodeSnippet(change, variableDict);
                        newRoot = DocumentBuilder.AddGlobalStatements(formattedChange, newRoot);
                    }
                }
            }

            else
            {
                var namespaceNode = newRoot?.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
                FileScopedNamespaceDeclarationSyntax? fileScopedNamespace = null;
                if (namespaceNode is null)
                {
                    fileScopedNamespace = newRoot?.Members.OfType<FileScopedNamespaceDeclarationSyntax>()?.FirstOrDefault();
                }

                string className = ProjectModifierHelper.GetClassName(file.FileName);
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
            }

            return newRoot;
        }

        internal async Task ModifyCshtmlFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            var fileDoc = project.Documents.Where(d => d.Name.EndsWith(file.FileName)).FirstOrDefault();
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

        internal async Task ModifyRazorFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions toolOptions)
        {
            var document = project.Documents.Where(d => d.Name.EndsWith(file.FileName)).FirstOrDefault();
            if (document is null)
            {
                return;
            }

            var razorChanges = file.RazorChanges.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, toolOptions));
            if (!razorChanges.Any())
            {
                return;
            }

            var editedDocument = await ProjectModifierHelper.ModifyDocumentText(document, razorChanges);
            if (editedDocument != null)
            {
                await ProjectModifierHelper.UpdateDocument(editedDocument, _consoleLogger);
            }
        }
    }
}
