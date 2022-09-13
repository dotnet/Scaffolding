using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
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
                .DescendantNodes()?.FirstOrDefault(node =>
                    node.IsKind(SyntaxKind.ClassDeclaration));

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

        protected const string Net7Csproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";

        protected const string Net7CsprojVariabledCsproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>$(Var2)$(Var3)</Var1>
    <Var2>net</Var2>
    <Var3>7.0</Var3>
    <TargetFramework>$(Var1)</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";

        protected const string Net7CsprojVariabledCsproj2 = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>net7.0</Var1>
    <TargetFramework>$(Var1)</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
";

        protected const string MultiTfmCsproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFrameworks>net6.0;net7.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";

        protected const string EmptyCsproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
</Project>
";
        protected const string EmptyCsproj2 = @"";

        protected const string MultiTfmVariabledCsproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>$(Var2);$(Var3)</Var1>
    <Var2>net7.0</Var2>
    <Nullable>enable</Nullable>
    <Var3>net6.0</Var3>
    <TargetFrameworks>$(Var1)</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";
        protected const string MultiTfmVariabledCsproj2 = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>$(Var2)$(Var3)</Var1>
    <Var2>net</Var2>
    <Var3>7.0</Var3>
    <Nullable>enable</Nullable>
    <Var4>net6.0</Var4>
    <TargetFrameworks>$(Var1);$(Var4);net5.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";
        protected const string InvalidCsproj = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net69.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";
        protected const string InvalidCsproj2 = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>$(Var2);$(Var3)</Var1>
    <Var2>net77.0</Var2>
    <Nullable>enable</Nullable>
    <Var3>net69.0</Var3>
    <TargetFrameworks>$(Var1)</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
";
        protected const string InvalidCsproj3 = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <Var1>net77.0</Var1>
    <TargetFramework>$(Var1)</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
";

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

                    public void Test()
                    {
                    }
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

        protected const string ProgramCsFileNoNamespace = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;

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
        ";

        protected const string MinimalProgramCsFile = @"
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(""/Error"");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
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

        protected SyntaxTriviaList MemberLeadingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("    "));
        protected SyntaxTriviaList MemberTrailingTrivia = SyntaxFactory.TriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
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
