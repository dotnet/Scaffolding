using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class ProjectModifierHelperTests : DocumentBuilderTestBase
    {
        [Theory]
        [InlineData(new object[] { new string[] { "Startup.cs", "File.cs", "Test", "", null},
                                   new string[] { "Startup", "File", "", "", "" } })]
        public void GetClassNameTests(string[] classNames, string[] formattedClassNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                string className = classNames[i];
                string formattedClassName = formattedClassNames[i];
                Assert.Equal(ProjectModifierHelper.GetClassName(className), formattedClassName);
            }
        }

        [Fact]
        public async Task GetStartupClassNameTests()
        {
            Document programDocument = CreateDocument(ProgramCsFile);
            Document programDocumentNoStartup = CreateDocument(ProgramCsFileNoStartup);
            Document programDocumentDifferentStartup = CreateDocument(ProgramCsFileWithDifferentStartup);

            string startupName = await ProjectModifierHelper.GetStartupClassName(programDocument);
            string emptyStartupName = await ProjectModifierHelper.GetStartupClassName(programDocumentNoStartup);
            string notStartupName = await ProjectModifierHelper.GetStartupClassName(programDocumentDifferentStartup);
            string nullStartup = await ProjectModifierHelper.GetStartupClassName(null);

            Assert.Equal("Startup", startupName);
            Assert.Equal("", emptyStartupName);
            Assert.Equal("", nullStartup);
            Assert.Equal("NotStartup", notStartupName);
        }

        [Theory]
        [InlineData(new object[] { new string[] { "string.ToString()", "bool.ToString()", "IServices.AddRazorPages()", "IWebSomething.DoWebStuff()", "", "Wrong.Missing()" },
                                   new string[] { "password.ToString()", "IsTrue.ToString()", "services.AddRazorPages()", "app.DoWebStuff()", "", "Wrong.Missing()" } })]
        public void FormatCodeBlockTests(string[] parameterKeys, string[] formattedValues)
        {
            for (int i = 0; i < parameterKeys.Length; i++)
            {
                string paramKey = parameterKeys[i];
                string correctCodeBlock = formattedValues[i];
                string formattedCodeBlock = ProjectModifierHelper.FormatCodeBlock(paramKey, ParameterValues);
                Assert.Equal(formattedCodeBlock, correctCodeBlock);
            }
        }


        [Fact]
        public async Task IsMinimalAppTests()
        {
            var modelTypes = new List<ModelType>();
            var minimalModelTypesLocator = new Mock<IModelTypesLocator>();
            var nonMinimalTypesLocator = new Mock<IModelTypesLocator>();
            //IModelTypesLocator has a Startup.cs file
            nonMinimalTypesLocator.Setup(m => m.GetType("Startup")).Returns(() => { return new List<ModelType> { startupModel }; });
            //IModelTypesLocator does not have a Startup.cs file
            minimalModelTypesLocator.Setup(m => m.GetType("Startup")).Returns(() => { return new List<ModelType> { }; });
            Assert.True(await ProjectModifierHelper.IsMinimalApp(minimalModelTypesLocator.Object));
            Assert.False(await ProjectModifierHelper.IsMinimalApp(nonMinimalTypesLocator.Object));

            //test other IsMinimalApp method
        }

        [Theory]
        [InlineData(
            new object[] {
                new string[] {
                    "\nWebApplication.CreateBuilder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)\n    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"));",
                    "\nWebApplication.CreateBuilder.Services.AddAuthorization(options =>\n{\n    options.FallbackPolicy = options.DefaultPolicy;\n});\n\r",
                    "absdf \n \r asdfsadfasdf sdfasdf \n {} ",
                    "",
                    null
                },
                new string[] {
                    "WebApplication.CreateBuilder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"))",
                    "WebApplication.CreateBuilder.Services.AddAuthorization(options=>{options.FallbackPolicy=options.DefaultPolicy})",
                    "absdfasdfsadfasdfsdfasdf{}",
                    "",
                    ""
                }
            }
        )]
        public void TrimStatementTests(string[] statements, string[] trimmedStatements)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                string statement = statements[i];
                string trimmedStatement = trimmedStatements[i];
                Assert.Equal(trimmedStatement, ProjectModifierHelper.TrimStatement(statement));
            }
        }

        [Theory]
        [InlineData(
            new string[] {
                "var builder = WebApplication.CreateBuilder(args);",
                "var      builderThing = WebApplication.CreateBuilder(args);",
                "var builderVar=WebApplication.CreateBuilder(args);",
                "var realBuilder =             WebApplication.CreateBuilder(args);",
                "WebApplicationBuilder fakeBuilder = WebApplication.CreateBuilder(args);",
                "WebApplicationBuilder           builderr =      WebApplication.CreateBuilder(args);",
                "WebApplicationBuilder Builder=WebApplication.CreateBuilder(args);"
            },
            new string[] {
                "builder",
                "builderThing",
                "builderVar",
                "realBuilder",
                "fakeBuilder",
                "builderr",
                "Builder",
            })
        ]
        public void GetVariablesTests(string[] statements, string[] builderVariableIdentifierValues)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                var member = SyntaxFactory.ParseMemberDeclaration(statements[i]);
                var members = new SyntaxList<MemberDeclarationSyntax>().Add(member);
                string builderVariableIdentifierValue = builderVariableIdentifierValues[i];
                var transform = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(members);
                Assert.True(transform.HasValue);
                Assert.Equal(builderVariableIdentifierValue, transform.Value.Item2);
            }
        }

        [Fact]
        public void FormatCodeSnippetTests()
        {
            CodeSnippet snippet = new CodeSnippet
            {
                Block = "string.ToString()",
                CheckBlock = "((string)Boolean).ToString()",
                Parent = "Builder.Build()",
                InsertAfter = "nonExistentVariable.DoingStuff(string);",
                InsertBefore = new string[] { "nonExistentVariable.DoingOtherStuff()", "Builder.Boolean.ToString()" }
            };

            CodeSnippet correctlyFormattedSnippet = new CodeSnippet
            {
                Block = "myCustomString.ToString()",
                CheckBlock = "((myCustomString)myCustomBoolean).ToString()",
                Parent = "builderVar.Build()",
                InsertAfter = "nonExistentVariable.DoingStuff(myCustomString);",
                InsertBefore = new string[] { "nonExistentVariable.DoingOtherStuff()", "builderVar.myCustomBoolean.ToString()" }
            };

            var variableDict = new Dictionary<string, string>()
            {
                { "string", "myCustomString" },
                { "Boolean", "myCustomBoolean" },
                { "Builder", "builderVar" }
            };

            foreach (var kvp in variableDict)
            {
                snippet = ProjectModifierHelper.UpdateVariables(snippet, kvp.Key, kvp.Value);
            }

            Assert.Equal(correctlyFormattedSnippet.Block, snippet.Block);
            Assert.Equal(correctlyFormattedSnippet.CheckBlock, snippet.CheckBlock);
            Assert.Equal(correctlyFormattedSnippet.InsertBefore, snippet.InsertBefore);
            Assert.Equal(correctlyFormattedSnippet.InsertAfter, snippet.InsertAfter);
            Assert.Equal(correctlyFormattedSnippet.Parent, snippet.Parent);
        }

        [Fact]
        public void FormatGlobalStatementTests()
        {
            string[] globalStatements = new string[]
            {
                "string.ToString()",
                "((string)Boolean).ToString()",
                "Builder.Build()",
                "nonExistentVariable.DoingStuff(string);",
                "nonExistentVariable.DoingOtherStuff()",
                "Builder.Boolean.ToString()" 
            };

            string[] correctlyFormattedStatements = new string[]
            {
                "myCustomString.ToString()",
                "((myCustomString)myCustomBoolean).ToString()",
                "builderVar.Build()",
                "nonExistentVariable.DoingStuff(myCustomString);",
                "nonExistentVariable.DoingOtherStuff()",
                "builderVar.myCustomBoolean.ToString()"
            };

            for (int i = 0; i < globalStatements.Length; i++)
            {
                var formattedGlobalStatements = ProjectModifierHelper.ReplaceValue(globalStatements[i], "string", "myCustomString");
                formattedGlobalStatements = ProjectModifierHelper.ReplaceValue(formattedGlobalStatements, "Boolean", "myCustomBoolean");
                formattedGlobalStatements = ProjectModifierHelper.ReplaceValue(formattedGlobalStatements, "Builder", "builderVar");
                Assert.Equal(correctlyFormattedStatements[i], formattedGlobalStatements);
            }
        }

        [Fact]
        public void FilterOptionsTests()
        {
            var optionsWithGraph = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = false };
            var optionsWithApi = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = true };
            var optionsWithBoth = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = true };
            var optionsWithNeither = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = false };

            var graphOptions = new string[] { "MicrosoftGraph" };
            var apiOptions = new string[] { "DownstreamApi" };
            var bothOptions = new string[] { "DownstreamApi", "MicrosoftGraph" };
            var neitherOptions = new string[] { };

            Assert.True(ProjectModifierHelper.FilterOptions(graphOptions, optionsWithGraph));
            Assert.False(ProjectModifierHelper.FilterOptions(graphOptions, optionsWithApi));
            Assert.True(ProjectModifierHelper.FilterOptions(graphOptions, optionsWithBoth));
            Assert.False(ProjectModifierHelper.FilterOptions(graphOptions, optionsWithNeither));

            Assert.False(ProjectModifierHelper.FilterOptions(apiOptions, optionsWithGraph));
            Assert.True(ProjectModifierHelper.FilterOptions(apiOptions, optionsWithApi));
            Assert.True(ProjectModifierHelper.FilterOptions(apiOptions, optionsWithBoth));
            Assert.False(ProjectModifierHelper.FilterOptions(apiOptions, optionsWithNeither));

            Assert.True(ProjectModifierHelper.FilterOptions(bothOptions, optionsWithGraph));
            Assert.True(ProjectModifierHelper.FilterOptions(bothOptions, optionsWithApi));
            Assert.True(ProjectModifierHelper.FilterOptions(bothOptions, optionsWithBoth));
            Assert.False(ProjectModifierHelper.FilterOptions(bothOptions, optionsWithNeither));

            Assert.True(ProjectModifierHelper.FilterOptions(neitherOptions, optionsWithGraph));
            Assert.True(ProjectModifierHelper.FilterOptions(neitherOptions, optionsWithApi));
            Assert.True(ProjectModifierHelper.FilterOptions(neitherOptions, optionsWithBoth));
            Assert.True(ProjectModifierHelper.FilterOptions(neitherOptions, optionsWithNeither));
        }

        [Fact]
        public void FilterCodeBlocksTests()
        {
            var optionsWithGraph = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = false };
            var optionsWithApi = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = true };
            var optionsWithBoth = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = true };
            var optionsWithNeither = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = false };

            var graphBlock = new CodeBlock { Block = "GraphProperty", Options = new string[] { "MicrosoftGraph" } };
            var apiBlock = new CodeBlock { Block = "DownstreamProperty", Options = new string[] { "DownstreamApi" } };
            var bothBlock = new CodeBlock { Block = "BothProperty", Options = new string[] { "DownstreamApi", "MicrosoftGraph" } };
            var neitherBlock = new CodeBlock { Block = "NeitherProperty", Options = new string[] { } };

            var codeBlocks = new CodeBlock[] { graphBlock, apiBlock, bothBlock, neitherBlock };
            var filteredWithGraph = ProjectModifierHelper.FilterCodeBlocks(codeBlocks, optionsWithGraph);
            var filteredWithApi = ProjectModifierHelper.FilterCodeBlocks(codeBlocks, optionsWithApi);
            var filteredWithBoth = ProjectModifierHelper.FilterCodeBlocks(codeBlocks, optionsWithBoth);
            var filteredWithNeither = ProjectModifierHelper.FilterCodeBlocks(codeBlocks, optionsWithNeither);

            Assert.True(
                filteredWithGraph.Length == 3 &&
                filteredWithGraph.Contains(graphBlock) &&
                !filteredWithGraph.Contains(apiBlock) &&
                filteredWithGraph.Contains(bothBlock) &&
                filteredWithGraph.Contains(neitherBlock));

            Assert.True(
                filteredWithApi.Length == 3 &&
                !filteredWithApi.Contains(graphBlock) &&
                filteredWithApi.Contains(apiBlock) &&
                filteredWithApi.Contains(neitherBlock) &&
                filteredWithApi.Contains(bothBlock));

            Assert.True(
                filteredWithBoth.Length == 4 &&
                filteredWithBoth.Contains(graphBlock) &&
                filteredWithBoth.Contains(apiBlock) &&
                filteredWithBoth.Contains(bothBlock) &&
                filteredWithBoth.Contains(neitherBlock));

            Assert.True(
                filteredWithNeither.Length == 1 &&
                !filteredWithNeither.Contains(graphBlock) &&
                !filteredWithNeither.Contains(apiBlock) &&
                filteredWithNeither.Contains(neitherBlock) &&
                !filteredWithNeither.Contains(bothBlock));
        }


        [Fact]
        public void FilterCodeSnippetsTests()
        {
            var optionsWithGraph = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = false };
            var optionsWithApi = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = true };
            var optionsWithBoth = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = true };
            var optionsWithNeither = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = false };

            var graphSnippet = new CodeSnippet { Block = "GraphProperty", Options = new string[] { "MicrosoftGraph" } };
            var apiSnippet = new CodeSnippet { Block = "DownstreamProperty", Options = new string[] { "DownstreamApi" } };
            var bothSnippet = new CodeSnippet { Block = "BothProperty", Options = new string[] { "DownstreamApi", "MicrosoftGraph" } };
            var neitherSnippet = new CodeSnippet { Block = "NeitherProperty", Options = new string[] { } };

            var codeSnippets = new CodeSnippet[] { graphSnippet, apiSnippet, bothSnippet, neitherSnippet };
            var filteredWithGraph = ProjectModifierHelper.FilterCodeSnippets(codeSnippets, optionsWithGraph);
            var filteredWithApi = ProjectModifierHelper.FilterCodeSnippets(codeSnippets, optionsWithApi);
            var filteredWithBoth = ProjectModifierHelper.FilterCodeSnippets(codeSnippets, optionsWithBoth);
            var filteredWithNeither = ProjectModifierHelper.FilterCodeSnippets(codeSnippets, optionsWithNeither);

            Assert.True(
                filteredWithGraph.Length == 3 &&
                filteredWithGraph.Contains(graphSnippet) &&
                !filteredWithGraph.Contains(apiSnippet) &&
                filteredWithGraph.Contains(bothSnippet) &&
                filteredWithGraph.Contains(neitherSnippet));

            Assert.True(
                filteredWithApi.Length == 3 &&
                !filteredWithApi.Contains(graphSnippet) &&
                filteredWithApi.Contains(apiSnippet) &&
                filteredWithApi.Contains(neitherSnippet) &&
                filteredWithApi.Contains(bothSnippet));

            Assert.True(
                filteredWithBoth.Length == 4 &&
                filteredWithBoth.Contains(graphSnippet) &&
                filteredWithBoth.Contains(apiSnippet) &&
                filteredWithBoth.Contains(bothSnippet) &&
                filteredWithBoth.Contains(neitherSnippet));

            Assert.True(
                filteredWithNeither.Length == 1 &&
                !filteredWithNeither.Contains(graphSnippet) &&
                !filteredWithNeither.Contains(apiSnippet) &&
                filteredWithNeither.Contains(neitherSnippet) &&
                !filteredWithNeither.Contains(bothSnippet));
        }

        [Theory]
        [InlineData("app.UseRouting();", false)]
        [InlineData("", true)]
        public void EmptyBlockStatementExistsTests(string contents, bool contains)
        {
            StatementSyntax emptyBlock = SyntaxFactory.ParseStatement(
                @"
                {                   
                }");

            BlockSyntax emptyBlockSyntax = SyntaxFactory.Block(emptyBlock);
            var children = DocumentBuilder.GetDescendantNodes(emptyBlockSyntax);
            Assert.Equal(ProjectModifierHelper.StatementExists(children, contents), contains);
        }

        [Theory]
        [InlineData("app.UseRouting();", true)]
        [InlineData("app.UseDeveloperExceptionPage();", false)]
        [InlineData("", true)]
        public void BlockStatementExistsTests(string contents, bool contains)
        {
            StatementSyntax block = SyntaxFactory.ParseStatement(
                    @"
                    {
                        app.UseRouting();
                    }");
            BlockSyntax emptyBlockSyntax = SyntaxFactory.Block(block);
            var children = DocumentBuilder.GetDescendantNodes(emptyBlockSyntax);
            Assert.Equal(ProjectModifierHelper.StatementExists(children, contents), contains);
        }

        [Theory]
        [InlineData("app.UseRouting();", false)]
        [InlineData("app.UseDeveloperExceptionPage();", true)]
        [InlineData("env.IsDevelopment()", true)]
        [InlineData("services.AddRazorPages()", true)]
        [InlineData("services.AddRazorPages().AddMvcOptions(options => {})", true)]
        [InlineData("endpoints.MapRazorPages();", true)]
        [InlineData("", true)]
        public void DenseBlockStatementExistsTests(string contents, bool contains)
        {
            StatementSyntax denseBlock = SyntaxFactory.ParseStatement(
                @"
                {
                    app.UseRoutingNot();
                    services.AddRazorPages().AddMvcOptions(options => {}).AddMicrosoftIdentityUI();
                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                });    
                }");

            BlockSyntax denseBlockSyntax = SyntaxFactory.Block(denseBlock);
            var children = DocumentBuilder.GetDescendantNodes(denseBlockSyntax);
            Assert.Equal(ProjectModifierHelper.StatementExists(children, contents), contains);
        }

        [Theory]
        [InlineData(new object[] { new string[] { "Authorize", "Empty" },
                                   new string[] { "Theory", "Controller" },
                                   new string[] { "", null }})]
        public async Task AttributeExistsTests(string[] existingAttributes, string[] nonExistingAttributes, string[] invalidAttributes)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var attributeLists = classSyntax.AttributeLists;

            foreach (var attribute in existingAttributes)
            {
                Assert.True(ProjectModifierHelper.AttributeExists(attribute, attributeLists));
            }

            foreach (var attribute in nonExistingAttributes)
            {
                Assert.False(ProjectModifierHelper.AttributeExists(attribute, attributeLists));
            }

            foreach (var attribute in invalidAttributes)
            {
                Assert.False(ProjectModifierHelper.AttributeExists(attribute, attributeLists));
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "var app = builder.Build()", "app.UseHttpsRedirection()" , "app.UseStaticFiles()", "app.UseRouting()" },
                                   new string[] { "var app2 = builder.Build()", "app2.UseHttpsRedirection()" , "app2.UseStaticFiles()", "app2.UseRouting()" }}
        )]
        public async Task GlobalStatementExistsTests(string[] existingStatements, string[] nonExistingStatements)
        {
            Document minimalProgramCsDoc = CreateDocument(MinimalProgramCsFile);
            var root = await minimalProgramCsDoc.GetSyntaxRootAsync() as CompilationUnitSyntax;
            //test existing global statments in MinimalProgramCsFile
            foreach (var existingStatement in existingStatements)
            {
                var expression = SyntaxFactory.ParseStatement(existingStatement);
                var globalStatement = SyntaxFactory.GlobalStatement(expression).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                Assert.True(ProjectModifierHelper.GlobalStatementExists(root, globalStatement));
            }

            foreach (var nonExistingStatement in nonExistingStatements)
            {
                var expression = SyntaxFactory.ParseStatement(nonExistingStatement);
                var globalStatement = SyntaxFactory.GlobalStatement(expression).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                Assert.False(ProjectModifierHelper.GlobalStatementExists(root, globalStatement));
            }
        }

        private static readonly ModelType startupModel = new ModelType
        {
            Name = "Startup",
            Namespace = "Application",
            FullName = "C:\\Solution\\Application\\Startup.cs"
        };
    }
}
