using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public abstract class DocumentBuilderTestBase
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string TestProjectName = "TestProject";

        internal static char[] CodeSnippetTrimChars = new char[] { ' ', '\r', '\n', ';' };
        /// <summary>
        /// Use this method for single file unittests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp)
        {
            return CreateProject(new[] { source }, language).Documents.First();
        }

        protected static async Task<ClassDeclarationSyntax> CreateClassSyntax(DocumentEditor editor)
        {
            var docRoot = (CompilationUnitSyntax)await editor.GetChangedDocument().GetSyntaxRootAsync();
            var namespaceNode = docRoot?.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();

            //get classNode. All class changes are done on the ClassDeclarationSyntax and then that node is replaced using documentEditor.
            var classNode = namespaceNode?
                .DescendantNodes()?
                .Where(node =>
                    node is ClassDeclarationSyntax cds)
                .FirstOrDefault();

            return (ClassDeclarationSyntax)classNode;
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// Use this method for multi-file unittests
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        protected static CodeAnalysis.Project CreateProject(string[] sources, string projectPath = null, string language = LanguageNames.CSharp)
        {
            var projectName = string.IsNullOrEmpty(projectPath) ? TestProjectName : Path.GetFileName(projectPath);

            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt =  CSharpDefaultFileExt;

            var projectId = ProjectId.CreateNewId(debugName: projectName);

            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, projectName, projectName, language, projectPath);

            var workspace = new AdhocWorkspace();
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));

            var solution = workspace
                .CurrentSolution
                .AddProject(projectInfo)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source), filePath: newFileName);
                count++;
            }

            workspace.TryApplyChanges(solution);

            return solution.GetProject(projectId);
        }

        //create ParameterSyntax list for VerifyParametersTests
        protected static List<ParameterSyntax> CreateParameterList(string[] types, string[] vals)
        {
            var paramList = new List<ParameterSyntax>();
            if (types.Length == vals.Length)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    var val = vals[i];
                    paramList.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(val))
                        .WithType(SyntaxFactory.IdentifierName
                        (
                            SyntaxFactory.Identifier
                            (
                                SyntaxFactory.TriviaList(),
                                type,
                                SyntaxFactory.TriviaList
                                (
                                    SyntaxFactory.Space
                                )
                            )
                        )
                    ));
                }
            }
            return paramList;
        }

        protected const string FullDocument = @"
            using System;
            using System.Duplicate;

            namespace TestProject
            {
                [Authorize]
                [Empty]
                public class Test
                {
                    static readonly string[] scopeRequiredByApi = new string[] { ""access_as_user"" };
                    public int Id { get; set; }                    
                }
            }";

        protected const string ProgramCsFile = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;

            namespace webapp
            {
                public class Program
                {
                    public static void Main(string[] args)
                    {
                        CreateHostBuilder(args).Build().Run();
                    }

                    public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .ConfigureWebHostDefaults(webBuilder =>
                            {
                                webBuilder.UseStartup<Startup>();
                            });
                }
            }
        ";

        protected const string ProgramCsFileWithDifferentStartup = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;

            namespace webapp
            {
                public class Program
                {
                    public static void Main(string[] args)
                    {
                        CreateHostBuilder(args).Build().Run();
                    }

                    public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .ConfigureWebHostDefaults(webBuilder =>
                            {
                                webBuilder.UseStartup<NotStartup>();
                            });
                }
            }
        ";

        protected const string ProgramCsFileNoStartup = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;

            namespace webapp
            {
                public class Program
                {
                    public static void Main(string[] args)
                    {
                        CreateHostBuilder(args).Build().Run();
                    }

                    public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .ConfigureWebHostDefaults(webBuilder =>
                            {
                                webBuilder.UseStartup<>();
                            });
                }
            }
        ";

        protected const string EmptyDocument = @"
            namespace TestProject
            {
                public class Test
                {
                }
            }";

        protected SyntaxTriviaList MemberLeadingTrivia = new SyntaxTriviaList(SyntaxFactory.Whitespace("    "));
        protected SyntaxTriviaList MemberTrailingTrivia = new SyntaxTriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
        protected static SyntaxTrivia SemiColonTrivia
        {
            get
            {
                return SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia()
                    .WithTokens(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
            }
        }
        //corresponds to FormatCodeBlockTests
        protected Dictionary<string, string> ParameterValues
        {
            get
            {
                if (_parameterValues == null)
                {
                    _parameterValues = new Dictionary<string, string>()
                        {
                            { "string", "password" },
                            { "bool", "IsTrue" },
                            { "IServices", "services" },
                            { "IWebSomething", "app" }
                        };
                }
                return _parameterValues;
            }
        }
        private Dictionary<string, string> _parameterValues;
    }
}
