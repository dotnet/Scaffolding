using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;

namespace Microsoft.DotNet.Scaffolding.Shared.Project
{
    internal static class ProjectModifierHelper
    {
        internal static char[] CodeSnippetTrimChars = new char[] { ' ', '\r', '\n', ';' };
        internal static IEnumerable<string> CodeSnippetTrimStrings = CodeSnippetTrimChars.Select(c => c.ToString());
        internal static char[] Parentheses = new char[] { '(', ')' };
        internal const string VarIdentifier = "var";
        internal const string WebApplicationBuilderIdentifier = "WebApplicationBuilder";

        public const string Main = nameof(Main);

        /// <summary>
        /// Check if Startup.cs or similar file exists.
        /// </summary>
        /// <returns>true if Startup.cs does not exist, false if it does exist.</returns>
        internal static async Task<bool> IsMinimalApp(IModelTypesLocator modelTypesLocator)
        {
            //find Startup if named Startup.
            var startupType = modelTypesLocator.GetType("Startup").FirstOrDefault();
            if (startupType == null)
            {
                //if changed the name in Program.cs, get the class name and check.
                var programDocument = modelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.EndsWith("Program.cs"));
                var startupClassName = await GetStartupClassName(programDocument);
                startupType = modelTypesLocator.GetType(startupClassName).FirstOrDefault();
            }
            return startupType == null;
        }

        internal static async Task<bool> IsUsingTopLevelStatements(List<Document> documents)
        {
            var programDocument = documents?.FirstOrDefault(d => d.Name.EndsWith("Program.cs"));
            if (programDocument != null && await programDocument.GetSyntaxRootAsync() is CompilationUnitSyntax root)
            {
                var fileScopedNamespaceNode = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>()?.FirstOrDefault();
                if (fileScopedNamespaceNode == null)
                {
                    var mainMethod = DocumentBuilder.GetMethodFromSyntaxRoot(root, Main);
                    return mainMethod == null;
                }
            }

            return true;
        }

        internal static async Task<bool> IsUsingTopLevelStatements(IModelTypesLocator modelTypesLocator)
        {
            var programDocument = modelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.EndsWith("Program.cs"));
            if (programDocument != null && await programDocument.GetSyntaxRootAsync() is CompilationUnitSyntax root)
            {
                var fileScopedNamespaceNode = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>()?.FirstOrDefault();
                if (fileScopedNamespaceNode == null)
                {
                    var namespaceNode = root.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
                    var classNode = namespaceNode?.Members.OfType<ClassDeclarationSyntax>()?.FirstOrDefault();
                    var mainMethod = classNode?.ChildNodes().FirstOrDefault(n => n is MethodDeclarationSyntax
                        && ((MethodDeclarationSyntax)n).Identifier.ToString().Equals(Main, StringComparison.OrdinalIgnoreCase));

                    return mainMethod == null;
                }
            }

            return true;
        }

        // Returns true when there is no Startup.cs or equivalent
        internal static async Task<bool> IsMinimalApp(List<Document> documents)
        {
            if (documents.Where(d => d.Name.EndsWith("Startup.cs")).Any())
            {
                return false;
            }

            // if changed the name in Program.cs, get the class name and check.
            var programDocument = documents.FirstOrDefault(d => d.Name.EndsWith("Program.cs"));
            var startupClassName = await GetStartupClassName(programDocument);

            return string.IsNullOrEmpty(startupClassName); // If project has UseStartup in Program.cs, it is not a minimal app
        }

        // Get Startup class name from CreateHostBuilder in Program.cs. If Program.cs is not being used, method
        // will return null.
        internal static async Task<string> GetStartupClass(List<Document> documents)
        {
            string startupClassName = string.Empty;
            if (documents != null && documents.Any())
            {
                var programCsDocument = documents.FirstOrDefault(d => d.Name.Equals("Program.cs"));
                startupClassName = await GetStartupClassName(programCsDocument);
            }

            return string.IsNullOrEmpty(startupClassName) ? string.Empty : string.Concat(startupClassName, ".cs");
        }

        internal static async Task<string> GetStartupClassName(Document programDoc)
        {
            if (programDoc != null && await programDoc.GetSyntaxRootAsync() is CompilationUnitSyntax root)
            {
                var namespaceNode = root.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
                var programClassNode =
                    namespaceNode?.DescendantNodes()
                        .FirstOrDefault(node =>
                            node is ClassDeclarationSyntax cds &&
                            cds.Identifier
                               .ValueText.Contains("Program")) ??
                    root?.DescendantNodes()
                        .FirstOrDefault(node =>
                            node is ClassDeclarationSyntax cds &&
                            cds.Identifier
                               .ValueText.Contains("Program"));

                var useStartupNode = programClassNode?.DescendantNodes()?
                    .FirstOrDefault(node =>
                        node is MemberAccessExpressionSyntax maes &&
                        maes.ToString()
                            .Contains("webBuilder.UseStartup"));

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

        internal static string GetClassName(string className)
        {
            string formattedClassName = string.Empty;
            if (!string.IsNullOrEmpty(className))
            {
                string[] blocks = className.Split(".cs");
                if (blocks.Length > 1)
                {
                    return blocks[0];
                }
            }
            return formattedClassName;
        }

        internal static BaseMethodDeclarationSyntax GetOriginalMethod(ClassDeclarationSyntax classNode, string methodName, Method methodChanges)
        {
            if (classNode?.Members.FirstOrDefault(node
                => node is MethodDeclarationSyntax mds && mds.Identifier.ValueText.Equals(methodName)
                || node is ConstructorDeclarationSyntax cds && cds.Identifier.ValueText.Equals(methodName))
                 is BaseMethodDeclarationSyntax foundMethod)
            {
                return foundMethod;
            }

            return null;
        }

        // check if the parameters match for the given method, and populate a Dictionary with parameter.Type keys and Parameter.Identifier values.
        internal static IDictionary<string, string> VerifyParameters(string[] parametersToCheck, List<ParameterSyntax> foundParameters)
        {
            IDictionary<string, string> parametersWithNames = new Dictionary<string, string>();
            if (foundParameters.Any() && parametersToCheck != null && parametersToCheck.Any())
            {
                var pars = foundParameters.ToList();
                foreach (var parameter in parametersToCheck)
                {
                    //Trim(' ') for the additional whitespace at the end of the parameter.Type string. Parameter.Type should be a singular word.
                    var verifiedParams = pars.Where(p => (p.Type?.ToFullString()?.Trim(' ')?.Equals(parameter)).GetValueOrDefault(false));
                    if (verifiedParams.Any())
                    {
                        parametersWithNames.Add(parameter, verifiedParams.First().Identifier.ValueText);
                    }
                }

                //Dictionary should have the same number of parameters we are trying to verify.
                if (parametersWithNames.Count == parametersToCheck.Length)
                {
                    return parametersWithNames;
                }
                else
                {
                    return null;
                }
            }

            return parametersWithNames;
        }

        /// <summary>
        /// Format a string of a SimpleMemberAccessExpression(eg., Type.Value)
        /// Replace Type with its value from the parameterDict.
        /// </summary>
        /// <param name="codeBlock">SimpleMemberAccessExpression string</param>
        /// <param name="parameterDict">IDictionary with parameter type keys and values</param>
        /// <param name="trim">Whether to trim the resulting string</param>
        /// <returns></returns>
        internal static string FormatCodeBlock(string codeBlock, IDictionary<string, string> parameterDict, bool trim = false)
        {
            string formattedCodeBlock = codeBlock;
            if (!string.IsNullOrEmpty(codeBlock) && parameterDict != null)
            {
                string value = Regex.Replace(codeBlock, "^([^.]*).", "");
                string param = Regex.Replace(codeBlock, "[*^.].*", "");
                if (parameterDict.TryGetValue(param, out string parameter))
                {
                    formattedCodeBlock = $"{parameter}.{value}";
                }
            }

            return trim ? formattedCodeBlock.Trim(CodeSnippetTrimChars) : formattedCodeBlock;
        }

        /// <summary>
        /// Looks for oldValue in codeBlock and replaces with newValue
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns>codeBlock where any instance of 'oldValue' is replaced with 'newValue'</returns>
        internal static string ReplaceValue(string codeBlock, string oldValue, string newValue)
        {
            string formattedStatement = codeBlock;
            if (!string.IsNullOrEmpty(formattedStatement) && !string.IsNullOrEmpty(oldValue) && !string.IsNullOrEmpty(newValue))
            {
                return formattedStatement.Replace(oldValue, newValue);
            }

            return formattedStatement;
        }

        /// <summary>
        /// Replaces all instances of the old value with the new value
        /// </summary>
        /// <param name="changes"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns>updated CodeSnippet array</returns>
        internal static CodeSnippet[] UpdateVariables(CodeSnippet[] changes, string oldValue, string newValue) => changes.Select(c => UpdateVariables(c, oldValue, newValue)).ToArray();

        /// <summary>
        /// Replaces all instances of the old value with the new value
        /// </summary>
        /// <param name="change"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns>updated CodeSnippet</returns>
        internal static CodeSnippet UpdateVariables(CodeSnippet change, string oldValue, string newValue) //TODO
        {
            // format CodeSnippet fields for any variables or parameters.
            if (!string.IsNullOrEmpty(change.Block))
            {
                change.Block = ReplaceValue(change.Block, oldValue, newValue);
            }
            if (!string.IsNullOrEmpty(change.Parent))
            {
                change.Parent = ReplaceValue(change.Parent, oldValue, newValue);
            }
            if (!string.IsNullOrEmpty(change.CheckBlock))
            {
                change.CheckBlock = ReplaceValue(change.CheckBlock, oldValue, newValue);
            }
            if (!string.IsNullOrEmpty(change.InsertAfter))
            {
                change.InsertAfter = ReplaceValue(change.InsertAfter, oldValue, newValue);
            }
            if (change.InsertBefore != null && change.InsertBefore.Any())
            {
                for (int i = 0; i < change.InsertBefore.Count(); i++)
                {
                    change.InsertBefore[i] = ReplaceValue(change.InsertBefore[i], oldValue, newValue);
                }
            }

            return change;
        }

        /// <summary>
        /// Trim ' ', '\r', '\n' and replace any whitespace with no spaces.
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        internal static string TrimStatement(string statement)
        {
            StringBuilder sb = new StringBuilder(statement);
            if (!string.IsNullOrEmpty(statement))
            {
                foreach (string replacement in CodeSnippetTrimStrings)
                {
                    sb.Replace(replacement, string.Empty);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Searches through the list of nodes to find replacement variable identifier names
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        internal static (string, string)? GetBuilderVariableIdentifierTransformation(SyntaxList<MemberDeclarationSyntax> members)
        {
            if (!(members.FirstOrDefault(
                m => TrimStatement(m.ToString())
                .Contains("=WebApplication.CreateBuilder")) is SyntaxNode memberNode))
            {
                return null;
            }

            var memberString = TrimStatement(memberNode.ToString());

            var start = 0;
            if (memberString.Contains(VarIdentifier))
            {
                start = memberString.IndexOf(VarIdentifier) + VarIdentifier.Length;
            }
            else if (memberString.Contains(WebApplicationBuilderIdentifier))
            {
                start = memberString.IndexOf(WebApplicationBuilderIdentifier) + WebApplicationBuilderIdentifier.Length;
            }
            if (start > 0)
            {
                var end = memberString.IndexOf("=");
                return ("WebApplication.CreateBuilder", memberString.Substring(start, end - start));
            }

            return null;
        }

        internal static bool GlobalStatementExists(CompilationUnitSyntax root, GlobalStatementSyntax statement)
        {
            if (root != null && statement != null)
            {
                var formattedStatementString = TrimStatement(statement.ToString());
                bool foundStatement = root.Members.Where(st => TrimStatement(st.ToString()).Contains(formattedStatementString)).Any();

                if (foundStatement)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool AttributeExists(string attribute, SyntaxList<AttributeListSyntax> attributeList)
        {
            if (attributeList.Any() && !string.IsNullOrEmpty(attribute))
            {
                return attributeList.Where(al => al.Attributes.Where(attr => attr.ToString().Equals(attribute, StringComparison.OrdinalIgnoreCase)).Any()).Any();
            }
            return false;
        }

        internal static bool StatementExists(IEnumerable<SyntaxNode> nodes, string statement)
        {
            statement = statement.TrimEnd(Parentheses);
            return nodes.Any(n => StatementExists(n, statement));
        }

        internal static bool StatementExists(SyntaxNode node, string statement) => node.ToString().Contains(statement.Trim(), StringComparison.Ordinal);

        // Filter out CodeSnippets that are invalid using FilterOptions
        internal static CodeSnippet[] FilterCodeSnippets(CodeSnippet[] codeSnippets, CodeChangeOptions options) => codeSnippets.Where(cs => FilterOptions(cs.Options, options)).ToArray();

        /// <summary>
        /// Filter Options string array to matching CodeChangeOptions.
        /// Primary use to filter out CodeBlocks and Files that apply in Microsoft Graph and Downstream API scenarios
        /// </summary>
        /// <param name="options">string [] in cm_*.json files for code modifications</param>
        /// <param name="codeChangeOptions">based on cli parameters</param>
        /// <returns>true if the CodeChangeOptions apply, false otherwise. </returns>
        internal static bool FilterOptions(string[] options, CodeChangeOptions codeChangeOptions)
        {
            //if no options are passed, CodeBlock is valid
            if (options == null)
            {
                return true;
            }

            //if options have a "Skip", every CodeBlock is invalid
            if (options.Contains(CodeChangeOptionStrings.Skip))
            {
                return false;
            }
            // for example, program.cs is only modified when codeChangeOptions.IsMinimalApp is true
            if (options.Contains(CodeChangeOptionStrings.MinimalApp) && !codeChangeOptions.IsMinimalApp)
            {
                return false;
            }
            //if its a minimal app and options have a "NonMinimalApp", don't add the CodeBlock
            if (options.Contains(CodeChangeOptionStrings.NonMinimalApp) && codeChangeOptions.IsMinimalApp)
            {
                return false;
            }

            //an app will either support DownstreamApi, MicrosoftGraph, both, or neither.
            if (codeChangeOptions.DownstreamApi)
            {
                if (options.Contains(CodeChangeOptionStrings.DownstreamApi) ||
                    !options.Contains(CodeChangeOptionStrings.MicrosoftGraph))
                {
                    return true;
                }
            }
            if (codeChangeOptions.MicrosoftGraph)
            {
                if (options.Contains(CodeChangeOptionStrings.MicrosoftGraph) ||
                    !options.Contains(CodeChangeOptionStrings.DownstreamApi))
                {
                    return true;
                }
            }
            if (!codeChangeOptions.DownstreamApi && !codeChangeOptions.MicrosoftGraph)
            {
                if (options == null ||
                    (!options.Contains(CodeChangeOptionStrings.MicrosoftGraph) &&
                    !options.Contains(CodeChangeOptionStrings.DownstreamApi)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces text within document or appends text to the end of the document
        /// depending on whether change.ReplaceSnippet is set 
        /// </summary>
        /// <param name="fileDoc"></param>
        /// <param name="codeChanges"></param>
        /// <returns>updated document, or null if no changes made</returns>
        internal static async Task<Document> ModifyDocumentText(Document fileDoc, IEnumerable<CodeSnippet> codeChanges)
        {
            if (fileDoc is null || codeChanges is null || !codeChanges.Any())
            {
                return null;
            }

            var sourceText = await fileDoc.GetTextAsync();
            var sourceFileString = sourceText?.ToString() ?? null;
            if (sourceFileString is null)
            {
                return null;
            }

            var trimmedSourceFile = TrimStatement(sourceFileString);
            var applicableCodeChanges = codeChanges.Where(c => !trimmedSourceFile.Contains(TrimStatement(c.Block)));
            if (!applicableCodeChanges.Any())
            {
                return null;
            }

            foreach (var change in applicableCodeChanges)
            {
                // If doing a code replacement, replace ReplaceSnippet in source with Block
                if (change.ReplaceSnippet != null)
                {
                    var replaceSnippet = string.Join(Environment.NewLine, change.ReplaceSnippet);
                    if (sourceFileString.Contains(replaceSnippet))
                    {
                        sourceFileString = sourceFileString.Replace(replaceSnippet, change.Block);
                    }
                    else
                    {
                        // TODO: Generate readme file when replace snippets not found in file
                    }
                }
                else
                {
                    sourceFileString += change.Block; // Otherwise appending block to end of file
                }
            }

            var updatedSourceText = SourceText.From(sourceFileString);
            return fileDoc.WithText(updatedSourceText);
        }

        internal static async Task<string> UpdateDocument(Document document)
        {
            Debugger.Launch();
            var classFileTxt = await document.GetTextAsync();
            File.WriteAllText(document.Name, classFileTxt.ToString(), new UTF8Encoding(false));

            return $"Modified {document.Name}.\n"; // todo strings.
        }

        // Filter out CodeBlocks that are invalid using FilterOptions
        internal static CodeBlock[] FilterCodeBlocks(CodeBlock[] codeBlocks, CodeChangeOptions options)
        {
            var filteredCodeBlocks = new HashSet<CodeBlock>();
            if (codeBlocks != null && codeBlocks.Any() && options != null)
            {
                foreach (var codeBlock in codeBlocks)
                {
                    if (FilterOptions(codeBlock.Options, options))
                    {
                        filteredCodeBlocks.Add(codeBlock);
                    }
                }
            }
            return filteredCodeBlocks.ToArray();
        }
    }
}
