using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Scaffolding.Shared.Project
{
    internal static class ProjectModifierHelper
    {
        internal static char[] CodeSnippetTrimChars = new char[] { ' ', '\r', '\n', ';' };

        /// <summary>
        /// Check if Startup.cs or similar file exists.
        /// </summary>
        /// <returns>true if Startup.cs does not exist, false if it does exist.</returns>
        internal static async Task<bool> IsMinimalApp(Workspace workspace)
        {
            var modelTypesLocator = new ModelTypesLocator(workspace);
            //find Startup if named Startup.
            var startupType = modelTypesLocator.GetType("Startup").FirstOrDefault();
            if (startupType == null)
            {
                //if changed the name in Program.cs, get the class name and check.
                var programDocument = modelTypesLocator.GetAllDocuments().Where(d => d.Name.EndsWith("Program.cs")).FirstOrDefault();
                var startupClassName = await GetStartupClassName(programDocument);
                startupType = modelTypesLocator.GetType(startupClassName).FirstOrDefault();
            }
            return startupType == null;
        }

        internal static async Task<bool> IsMinimalApp(CodeAnalysis.Project project)
        {
            if (project != null)
            {
                var startupDocument = project.Documents.Where(d => d.Name.EndsWith("Startup.cs")).FirstOrDefault();
                if (startupDocument == null)
                {
                    //if changed the name in Program.cs, get the class name and check.
                    var programDocument = project.Documents.Where(d => d.Name.EndsWith("Program.cs")).FirstOrDefault();
                    var startupClassName = await GetStartupClassName(programDocument);
                    if (!string.IsNullOrEmpty(startupClassName))
                    {
                        startupDocument = project.Documents.Where(d => d.Name.EndsWith($"{startupClassName}.cs")).FirstOrDefault();
                    }
                   
                }
                return startupDocument == null;
            }
            return false;
        }

        //Get Startup class name from CreateHostBuilder in Program.cs. If Program.cs is not being used, method
        //will bail out.
        internal static async Task<string> GetStartupClass(string projectPath, CodeAnalysis.Project project)
        {
            var programFilePath = Directory.EnumerateFiles(projectPath, "Program.cs").FirstOrDefault();
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

        internal static async Task<string> GetStartupClassName(Document programDoc)
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

        /// <summary>
        /// Format a string of a SimpleMemberAccessExpression(eg., Type.Value)
        /// Replace Type with its value from the parameterDict.
        /// </summary>
        /// <param name="codeBlock">SimpleMemberAccessExpression string</param>
        /// <param name="parameterDict">IDictionary with parameter type keys and values</param>
        /// <returns></returns>
        internal static string FormatCodeBlock(string codeBlock, IDictionary<string, string> parameterDict)
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
            return formattedCodeBlock;
        }

        internal static string FormatGlobalStatement(string codeBlock, IDictionary<string, string> replacements)
        {
            string formattedStatement = string.Empty;
            if (!string.IsNullOrEmpty(codeBlock) && replacements != null && replacements.Any())
            {
                foreach (var key in replacements.Keys)
                {
                    replacements.TryGetValue(key, out string value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        formattedStatement = codeBlock.Replace(key, value);
                    }
                }
            }
            return formattedStatement;
        }

        /// <summary>
        /// Trim ' ', '\r', '\n' and repalce any whitespace with no spaces.
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        internal static string TrimStatement(string statement)
        {
            string trimmedStatement = statement;
            if (!string.IsNullOrEmpty(statement))
            {
                trimmedStatement = statement.Trim(CodeSnippetTrimChars).Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty); ;
            }
            return trimmedStatement;
        }
    }
}
