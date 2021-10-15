using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            new object[] {
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
                }
            }
        )]
        public void GetVariablesTests(string[] statements, string[] builderVariableIdentifierValues)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                var members = new SyntaxList<MemberDeclarationSyntax>() { SyntaxFactory.ParseMemberDeclaration(statements[i]) } ;
                string builderVariableIdentifierValue = builderVariableIdentifierValues[i];
                var variableDict = ProjectModifierHelper.GetBuilderVariableIdentifier(members);
                variableDict.TryGetValue("WebApplication.CreateBuilder", out string builderVariableString);
                if (!string.IsNullOrEmpty(builderVariableString))
                {
                    Assert.Equal(builderVariableIdentifierValue, builderVariableString);
                }    
                
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

            var formattedSnippet = ProjectModifierHelper.FormatCodeSnippet(snippet, variableDict);

            Assert.Equal(correctlyFormattedSnippet.Block, formattedSnippet.Block);
            Assert.Equal(correctlyFormattedSnippet.CheckBlock, formattedSnippet.CheckBlock);
            Assert.Equal(correctlyFormattedSnippet.InsertBefore, formattedSnippet.InsertBefore);
            Assert.Equal(correctlyFormattedSnippet.InsertAfter, formattedSnippet.InsertAfter);
            Assert.Equal(correctlyFormattedSnippet.Parent, formattedSnippet.Parent);
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

            var variablesDict = new Dictionary<string, string>()
            {
                { "string", "myCustomString" },
                { "Boolean", "myCustomBoolean" },
                { "Builder", "builderVar" }
            };

            for (int i = 0; i < globalStatements.Length; i++)
            {
                Assert.Equal(correctlyFormattedStatements[i], ProjectModifierHelper.FormatGlobalStatement(globalStatements[i], variablesDict));
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
