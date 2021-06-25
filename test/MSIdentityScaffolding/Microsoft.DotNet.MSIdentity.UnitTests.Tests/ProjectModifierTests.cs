using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.Tool;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{

    public class ProjectModifierTests
    {
        readonly ProjectModifier projectModifier = new ProjectModifier(new ApplicationParameters(), new ProvisioningToolOptions(), new ConsoleLogger());

        [Theory]
        [InlineData(new object[] { new string[] { "Microsoft.AspNetCore.Authentication", "Microsoft.Identity.Web", "Microsoft.Identity.Web.UI" } })]
        public void CreatingUsingsTests(string[] usings)
        {
            var usingDirectives = projectModifier.CreateUsings(usings);
            Assert.True(usingDirectives.Length == 3);
            foreach (var usingDirective in usings)
            {
                //check if they exist
                Assert.True(usingDirectives.Where(u => u.Name.ToString().Equals(usingDirective)).Any());
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "IServiceCollection", "IApplcationBuilder", "IWebHostEnvironment", "string ", "bool" },
                                   new string[] { "services", "app", "env", "testString", "testBool"} })]
        public void VerfiyParametersTests(string[] types, string[] vals)
        {
            var paramList = CreateParameterList(types, vals);
            var paramDict = projectModifier.VerfiyParameters(types, paramList);
            Assert.True(paramDict != null);

            foreach (var type in types)
            {
                Assert.True(paramDict.TryGetValue(type, out string value) && !string.IsNullOrEmpty(value));
            }

        }

        //create ParameterSyntax list for VerifyParametersTests
        private List<ParameterSyntax> CreateParameterList(string[] types, string[] vals)
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

        [Fact]
        public void StatementExistsTests()
        {
            //create a block with app.UseRouting();
            BlockSyntax blockSyntax = SyntaxFactory.Block
            (
                SyntaxFactory.SingletonList<StatementSyntax>
                (
                    SyntaxFactory.ExpressionStatement
                    (
                        SyntaxFactory.InvocationExpression
                        (
                            SyntaxFactory.MemberAccessExpression
                            (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName
                                (
                                    SyntaxFactory.Identifier
                                    (
                                        SyntaxFactory.TriviaList
                                        (
                                            SyntaxFactory.Whitespace("    ")
                                        ),
                                        "app",
                                        SyntaxFactory.TriviaList()
                                    )
                                ),
                                SyntaxFactory.IdentifierName("UseRouting")
                            )
                            .WithOperatorToken
                            (
                                SyntaxFactory.Token(SyntaxKind.DotToken)
                            )
                        )
                        .WithArgumentList
                        (
                            SyntaxFactory.ArgumentList()
                            .WithOpenParenToken
                            (
                                SyntaxFactory.Token(SyntaxKind.OpenParenToken)
                            )
                            .WithCloseParenToken
                            (
                                SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                            )
                        )
                    )
                    .WithSemicolonToken
                    (
                        SyntaxFactory.Token
                        (
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.SemicolonToken,
                            SyntaxFactory.TriviaList
                            (
                                SyntaxFactory.LineFeed
                            )
                        )
                    )
                )
            )
            .WithOpenBraceToken
            (
                SyntaxFactory.Token
                (
                    SyntaxFactory.TriviaList(),
                    SyntaxKind.OpenBraceToken,
                    SyntaxFactory.TriviaList
                    (
                        SyntaxFactory.LineFeed
                    )
                )
            )
            .WithCloseBraceToken
            (
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
            );

            StatementSyntax statement = SyntaxFactory.ParseStatement("app.UseRouting();");
            Assert.True(projectModifier.StatementExists(blockSyntax, statement));
        }

        private Dictionary<string, string> _parameterValues;

        //corresponds to FormatCodeBlockTests
        public Dictionary<string, string> ParameterValues
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

        [Theory]
        [InlineData(new object[] { new string[] { "string.ToString()", "bool.ToString()", "IServices.AddRazorPages()", "IWebSomething.DoWebStuff()", "", "Wrong.Missing()" },
                                   new string[] { "password.ToString()", "IsTrue.ToString()", "services.AddRazorPages()", "app.DoWebStuff()", "", "Wrong.Missing()" } })]
        public void FormatCodeBlockTests(string[] parameterKeys, string[] formattedValues)
        {
            for (int i = 0; i < parameterKeys.Length; i++)
            {
                string paramKey = parameterKeys[i];
                string correctCodeBlock = formattedValues[i];
                string formattedCodeBlock = projectModifier.FormatCodeBlock(paramKey, ParameterValues);
                Assert.Equal(formattedCodeBlock, correctCodeBlock);
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "Startup.cs", "File.cs", "Test", "", null},
                                   new string[] { "Startup", "File", "", "", "" } })]
        public void GetClassNameTests(string[] classNames, string[] formattedClassNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                string className = classNames[i];
                string formattedClassName = formattedClassNames[i];
                Assert.Equal(projectModifier.GetClassName(className), formattedClassName);
            }
        }
    }
}
