// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
        private readonly IEnumerable<string> _files;
        private readonly IConsoleLogger _consoleLogger;
        private PropertyInfo? _codeModifierConfigPropertyInfo;
        private const string Main = nameof(Main);
        private readonly StringBuilder _output;

        public ProjectModifier(ProvisioningToolOptions toolOptions, IEnumerable<string> files, IConsoleLogger consoleLogger)
        {
            _toolOptions = toolOptions ?? throw new ArgumentNullException(nameof(toolOptions));
            _files = files;
            _consoleLogger = consoleLogger ?? throw new ArgumentNullException(nameof(consoleLogger));
            _output = new StringBuilder();
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
                var csProjFiles = _files.Where(file => file.EndsWith(".csproj"));
                if (csProjFiles.Count() != 1)
                {
                    var errorMsg = string.Format(Resources.ProjectPathError, _toolOptions.ProjectFilePath);
                    _consoleLogger.LogFailureAndExit(errorMsg);
                }

                _toolOptions.ProjectFilePath = csProjFiles.First();
            }
            string csprojText = File.ReadAllText(_toolOptions.ProjectFilePath);
            _toolOptions.ShortTfms = ProjectModifierHelper.ProcessCsprojTfms(csprojText);

            CodeModifierConfig? codeModifierConfig = GetCodeModifierConfig();
            if (codeModifierConfig is null || !codeModifierConfig.Files.Any())
            {
                return;
            }

            // Initialize CodeAnalysis.Project wrapper
            CodeAnalysis.Project project = CodeAnalysisHelper.LoadCodeAnalysisProject(_toolOptions.ProjectFilePath, _files);
            if (project is null)
            {
                return;
            }

            var isMinimalApp = await ProjectModifierHelper.IsMinimalApp(project.Documents.ToList());
            var useTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(project.Documents.ToList());
            CodeChangeOptions options = new CodeChangeOptions
            {
                MicrosoftGraph = _toolOptions.CallsGraph,
                DownstreamApi = _toolOptions.CallsDownstreamApi,
                IsMinimalApp = isMinimalApp,
                UsingTopLevelsStatements = useTopLevelsStatements
            };

            // Go through all the files, make changes using DocumentBuilder.
            var filteredFiles = codeModifierConfig.Files.Where(f => ProjectModifierHelper.FilterOptions(f.Options, options));
            foreach (var file in filteredFiles)
            {
                await HandleCodeFileAsync(file, project, options);
            }

            _consoleLogger.LogJsonMessage(State.Success, output: _output.ToString().TrimEnd());
        }

        internal static string GetCodeFileString(CodeFile file)
        {
            // Resource files cannot contain '-' (dash) or '.' (period)
            var codeFilePropertyName = $"add_{file.FileName.Replace('.', '_')}";
            var property = AppProvisioningTool.Properties.FirstOrDefault(
                p => p.Name.Equals(codeFilePropertyName))
                ?? throw new FormatException($"Resource property for {file.FileName} could not be found. ");

            var codeFileString = property.GetValue(typeof(Resources))?.ToString();
            if (string.IsNullOrEmpty(codeFileString))
            {
                throw new FormatException($"CodeFile string for {file.FileName} was empty.");
            }

            return codeFileString;
        }

        internal static ClassDeclarationSyntax ModifyMethods(string fileName, ClassDeclarationSyntax classNode, Dictionary<string, Method> methods, CodeChangeOptions options, StringBuilder output)
        {
            foreach ((string methodName, Method methodChanges) in methods)
            {
                if (methodChanges == null)
                {
                    continue;
                }

                var methodNode = ProjectModifierHelper.GetOriginalMethod(classNode, methodName, methodChanges);
                if (methodNode is null)
                {
                    continue;
                }

                var parameters = ProjectModifierHelper.VerifyParameters(methodChanges.Parameters, methodNode.ParameterList.Parameters.ToList());
                foreach ((string oldValue, string newValue) in parameters)
                {
                    methodChanges.CodeChanges = ProjectModifierHelper.UpdateVariables(methodChanges.CodeChanges, oldValue, newValue);
                }

                var updatedMethodNode = DocumentBuilder.GetModifiedMethod(fileName, methodNode, methodChanges, options, output);
                if (updatedMethodNode != null)
                {
                    classNode = classNode.ReplaceNode(methodNode, updatedMethodNode);
                }
            }

            return classNode;
        }

        internal static async Task ModifyCshtmlFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
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
                await ProjectModifierHelper.UpdateDocument(editedDocument);
            }
        }

        /// <summary>
        /// Updates .razor and .html files via string replacement
        /// </summary>
        /// <param name="file"></param>
        /// <param name="project"></param>
        /// <param name="toolOptions"></param>
        /// <returns></returns>
        internal static async Task ApplyTextReplacements(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions toolOptions)
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
                await ProjectModifierHelper.UpdateDocument(editedDocument);
            }
        }

        internal async Task ModifyCsFile(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            if (file.FileName.Equals("Startup.cs"))
            {
                // Startup class file name may be different
                file.FileName = await ProjectModifierHelper.GetStartupClass(project.Documents.ToList()) ?? file.FileName;
            }

            var fileDoc = project.Documents.Where(d => d.Name.EndsWith(file.FileName)).FirstOrDefault();
            if (fileDoc is null || string.IsNullOrEmpty(fileDoc.Name))
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
                await documentBuilder.WriteToClassFileAsync(fileDoc.Name);
            }
        }

        private CodeModifierConfig? GetCodeModifierConfig()
        {
            if (string.IsNullOrEmpty(_toolOptions.ProjectType))
            {
                throw new ArgumentNullException(nameof(_toolOptions.ProjectType));
            }

            if (CodeModifierConfigPropertyInfo is null)
            {
                throw new ArgumentNullException(nameof(CodeModifierConfigPropertyInfo));
            }

            byte[] content = (CodeModifierConfigPropertyInfo.GetValue(null) as byte[])!;
            CodeModifierConfig? codeModifierConfig = ReadCodeModifierConfigFromFileContent(content);
            if (codeModifierConfig is null)
            {
                throw new FormatException(string.Format(Resources.ResourceFileParseError, CodeModifierConfigPropertyInfo.Name));
            }

            if (!string.Equals(codeModifierConfig.Identifier, _toolOptions.ProjectTypeIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException(string.Format(Resources.MismatchedProjectTypeIdentifier, codeModifierConfig.Identifier, _toolOptions.ProjectTypeIdentifier));
            }

            return codeModifierConfig;
        }

        private PropertyInfo? CodeModifierConfigPropertyInfo
        {
            get
            {
                if (_codeModifierConfigPropertyInfo == null)
                {
                    var codeModifierName = $"cm_{_toolOptions.ProjectTypeIdentifier.Replace('-', '_')}";
                    _codeModifierConfigPropertyInfo = AppProvisioningTool.Properties.FirstOrDefault(
                        p => p.Name.Equals(codeModifierName));
                }

                return _codeModifierConfigPropertyInfo;
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
                _consoleLogger.LogMessage(string.Format(Resources.CodeModifierConfigParsingError, _toolOptions.ProjectType, e.Message));
                return null;
            }
        }

        private async Task HandleCodeFileAsync(CodeFile file, CodeAnalysis.Project project, CodeChangeOptions options)
        {
            try
            {
                if (!string.IsNullOrEmpty(file.AddFilePath))
                {
                    AddFile(file);
                    _output.AppendLine(string.Format(Resources.AddedCodeFile, file.AddFilePath));
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

                    _output.AppendLine(string.Format(Resources.ModifiedCodeFile, file.FileName));
                }
            }
            catch (Exception e)
            {
                _output.Append(string.Format(Resources.FailedToModifyCodeFile, file.FileName, e.Message));
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
        private void AddFile(CodeFile file)
        {
            var filePath = Path.Combine(_toolOptions.ProjectPath, file.AddFilePath);
            if (File.Exists(filePath))
            {
                return; // File exists, don't need to create
            }

            var codeFileString = GetCodeFileString(file);

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir))
            {
                Directory.CreateDirectory(fileDir);
                File.WriteAllText(filePath, codeFileString);
            }
        }

        /// <summary>
        /// Modifies root if there any applicable changes
        /// </summary>
        /// <param name="documentBuilder"></param>
        /// <param name="options"></param>
        /// <param name="file"></param>
        /// <returns>modified root if there are changes, else null</returns>
        private SyntaxNode? ModifyRoot(DocumentBuilder documentBuilder, CodeChangeOptions options, CodeFile file)
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
                if (!options.UsingTopLevelsStatements)
                {
                    var mainMethod = root?.DescendantNodes().OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(n => Main.Equals(n.Identifier.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (mainMethod != null
                        && DocumentBuilder.ApplyChangesToMethod(mainMethod.Body, filteredChanges, file.FileName, _output) is BlockSyntax updatedBody)
                    {
                        var updatedMethod = mainMethod.WithBody(updatedBody);
                        return root?.ReplaceNode(mainMethod, updatedMethod);
                    }
                }
                else if (root.Members.Any(node => node.IsKind(SyntaxKind.GlobalStatement)))
                {
                    return DocumentBuilder.ApplyChangesToMethod(root, filteredChanges, file.FileName, _output);
                }
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
                    //add code snippets/changes.
                    modifiedClassDeclarationSyntax = ModifyMethods(file.FileName, modifiedClassDeclarationSyntax, file.Methods, options, _output);

                    //replace class node with all the updates.
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                    root = root.ReplaceNode(classNode, modifiedClassDeclarationSyntax);
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
                }
            }

            return root;
        }
    }
}
