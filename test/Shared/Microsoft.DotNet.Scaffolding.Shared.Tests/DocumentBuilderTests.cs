// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class DocumentBuilderTests : DocumentBuilderTestBase
    {
        [Theory]
        [InlineData(new object[] { new string[] { "System", "System.Test", "System.Data", "", null } })]
        public async Task AddUsingsTest(string[] usings)
        {
            //test class with usings.
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            CodeFile codeFile = new CodeFile
            {
                Usings = usings
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            var newRoot = docBuilder.AddUsings(new CodeChangeOptions());

            Assert.True(newRoot.Usings.Count == 4);
            foreach (var usingString in usings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(newRoot.Usings, node => node.Name.ToString().Equals(usingString));
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "System", "System.Test", "System.Data", "", null } })]
        public async Task AddingUsingsEmptyClassTest(string[] usings)
        {
            //test class without usings
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(EmptyDocument));
            CodeFile codeFile = new CodeFile
            {
                Usings = usings
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            var newRoot = docBuilder.AddUsings(new CodeChangeOptions());

            Assert.True(newRoot.Usings.Count == 3);
            foreach (var usingString in usings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(newRoot.Usings, node => node.Name.ToString().Equals(usingString));
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "System", "System.Duplicate" } })]
        public async Task AddUsingsTestDuplicate(string[] usings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            CodeFile codeFile = new CodeFile
            {
                Usings = usings
            };

            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            docBuilder.AddUsings(new CodeChangeOptions());

            //Get modified SyntaxNode root
            Document changedDoc = docBuilder.GetDocument();
            var root = (CompilationUnitSyntax)await changedDoc.GetSyntaxRootAsync();

            Assert.True(root.Usings.Count == 2);
            foreach (var usingString in usings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(root.Usings, node => node.Name.ToString().Equals(usingString));
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public string Name { get; set; }", "bool IsProperty { get; set; } = false", "", null } })]
        public async Task AddPropertiesTests(string[] properties)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var memberCount = classSyntax.Members.Count;
            var classProperties = properties.Select(p => new CodeBlock { Block = p }).ToArray();
            CodeFile codeFile = new CodeFile
            {
                ClassProperties = classProperties
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            classSyntax = docBuilder.AddProperties(classSyntax, new CodeChangeOptions());
            //Members count should be up by 2.
            Assert.True(classSyntax.Members.Count > memberCount);
            Assert.True(classSyntax.Members.Count == (memberCount + 2));

            //check for all the added members
            foreach (var property in properties)
            {
                if (!string.IsNullOrEmpty(property))
                {
                    Assert.Contains(classSyntax.Members, m => m.ToString().Trim(CodeSnippetTrimChars).Equals(property.Trim(CodeSnippetTrimChars)));
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "Authorize", "Theory", "Empty", "Controller", "", null } })]
        public async Task AddAttributesTests(string[] attributes)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var memberCount = classSyntax.Members.Count;
            var classAttributes = attributes.Select(at => new CodeBlock() { Block = at });
            CodeFile codeFile = new CodeFile
            {
                ClassAttributes = classAttributes.ToArray()
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            classSyntax = docBuilder.AddClassAttributes(classSyntax, new CodeChangeOptions());
            //Members count should be up by 2.
            Assert.True(classSyntax.AttributeLists.Count == 4);

            //check for all the added members
            foreach (var attribute in attributes)
            {
                if (!string.IsNullOrEmpty(attribute))
                {
                    Assert.Contains(classSyntax.AttributeLists, al => al.Attributes.Where(attr => attr.ToString().Equals(attribute, StringComparison.OrdinalIgnoreCase)).Any());
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "System", "System.Test", "System.Data", "", null } })]
        public async Task CreateUsingsTests(string[] usingsStrings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            //Add usings
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var usingDirectiveSyntax = DocumentBuilder.CreateUsings(usingsStrings);
            Assert.True(usingDirectiveSyntax.Length == 3);

            foreach (var usingString in usingsStrings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(usingDirectiveSyntax, usingDirective => usingDirective.Name.ToString().Equals(usingString));
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "Authorize", "Theory", "Empty", "Controller", "", null } })]
        public async Task CreateAttributeListTests(string[] attributeStrings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var classAttributes = attributeStrings.Select(at => new CodeBlock() { Block = at }).ToArray();
            var attributes = DocumentBuilder.CreateAttributeList(classAttributes, new SyntaxList<AttributeListSyntax>(), SyntaxFactory.TriviaList());
            Assert.True(attributes.Count == 4);

            foreach (var attributeString in attributeStrings)
            {
                if (!string.IsNullOrEmpty(attributeString))
                {
                    Assert.Contains(attributes, al => al.Attributes.Where(attr => attr.ToString().Equals(attributeString, StringComparison.OrdinalIgnoreCase)).Any());
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "IServiceCollection", "IApplcationBuilder", "IWebHostEnvironment", "string", "bool" },
                                   new string[] { "services", "app", "env", "testString", "testBool"} })]
        public async Task VerfiyParametersTests(string[] types, string[] vals)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var paramList = CreateParameterList(types, vals);
            var paramDict = ProjectModifierHelper.VerifyParameters(types, paramList);
            Assert.True(paramDict != null);

            foreach (var type in types)
            {
                Assert.True(paramDict.TryGetValue(type, out string value) && !string.IsNullOrEmpty(value));
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public string Name { get; set; }", "bool IsProperty { get; set; } = false", "", null },
                                   new string[] { "public string Name { get; set; }", "bool IsProperty { get; set; } = false" } })]
        public async Task CreateClassPropertiesTests(string[] properties, string[] nonDuplicateProperties)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentEditor emptyDocEditor = await DocumentEditor.CreateAsync(CreateDocument(EmptyDocument));
            var classProperties = properties.Select(p => new CodeBlock { Block = p }).ToArray();
            var classSyntax = await CreateClassSyntax(editor);
            var emptyClassSyntax = await CreateClassSyntax(emptyDocEditor);
            CodeFile codeFile = new CodeFile
            {
                ClassProperties = classProperties
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
            DocumentBuilder emptyDocBuilder = new DocumentBuilder(emptyDocEditor, codeFile, new MSIdentity.Shared.ConsoleLogger());

            var members = docBuilder.CreateClassProperties(classSyntax.Members, MemberLeadingTrivia, MemberTrailingTrivia);
            var membersInEmptyDoc = emptyDocBuilder.CreateClassProperties(emptyClassSyntax.Members, MemberLeadingTrivia, MemberTrailingTrivia);

            //only 2 since one should be a duplicate
            Assert.True(members.Length == 2);
            Assert.True(membersInEmptyDoc.Length == 3);

            foreach (var propertyString in properties)
            {
                if (!string.IsNullOrEmpty(propertyString))
                {
                    Assert.Contains(membersInEmptyDoc, m => m.ToString().Trim(CodeSnippetTrimChars).Equals(propertyString.Trim(CodeSnippetTrimChars)));
                }
            }

            foreach (var propertyString in nonDuplicateProperties)
            {
                Assert.Contains(members, m => m.ToString().Trim(CodeSnippetTrimChars).Equals(propertyString.Trim(CodeSnippetTrimChars)));
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public int Id { get; set; } " },
                                   new string[] { "public string Name { get; set; }", "bool IsProperty { get; set; } = false", "", null } })]
        public async Task PropertyExistsTests(string[] existingProperties, string[] nonExistingProperties)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));

            var classSyntax = await CreateClassSyntax(editor);
            var members = classSyntax.Members;
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());

            foreach (var property in existingProperties)
            {
                Assert.True(DocumentBuilder.PropertyExists(property, members));
            }

            //mpm 
            foreach (var property in nonExistingProperties)
            {
                Assert.False(DocumentBuilder.PropertyExists(property, members));
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "app.DoMethod1()", "app.Method2()" },
                                   new string[] { "var app = builder.Build()", "app.UseHttpsRedirection()", "app.UseStaticFiles()", "app.UseRouting()",  "app.DoMethod1()", "app.Method2()",
                                   "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }"} }
        )]
        public async Task AddGlobalStatementsTests(string[] statementsToAdd, string[] duplicateStatements)
        {
            Document minimalProgramCsDoc = CreateDocument(MinimalProgramCsFile);
            var root = await minimalProgramCsDoc.GetSyntaxRootAsync() as CompilationUnitSyntax;
            var codeChanges = statementsToAdd.Select(s => new CodeSnippet { Block = s }).ToArray();
            var modifiedRoot = DocumentBuilder.ApplyChangesToMethod(root, codeChanges, "filename") as CompilationUnitSyntax;

            foreach (var statementToAdd in statementsToAdd)
            {
                var expression = SyntaxFactory.ParseStatement(statementToAdd);
                var globalStatement = SyntaxFactory.GlobalStatement(expression).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                Assert.True(ProjectModifierHelper.GlobalStatementExists(modifiedRoot, globalStatement));
            }

            var duplicates = duplicateStatements.Select(s => new CodeSnippet { Block = s }).ToArray();
            var rootWithDuplicates = DocumentBuilder.ApplyChangesToMethod(modifiedRoot, duplicates, "filename") as CompilationUnitSyntax;
            Assert.Equal(rootWithDuplicates.Members.Count, modifiedRoot.Members.Count);
        }

        [Fact]
        public async Task GetMethodFromSyntaxRootTests()
        {
            var testDoc = CreateDocument(FullDocument);
            var root = await testDoc.GetSyntaxRootAsync() as CompilationUnitSyntax;
            var methodName = "Test";
            var method = DocumentBuilder.GetMethodFromSyntaxRoot(root, methodName);
            var invalidMethod = DocumentBuilder.GetMethodFromSyntaxRoot(root, "NotTest");

            Assert.NotNull(method);
            Assert.Null(invalidMethod);
            Assert.Equal(method.Identifier.ToString(), methodName);
        }

        [Fact]
        public void AddLeadingTriviaSpacesTests()
        {
            CodeSnippet snippetNullTrivia = new CodeSnippet
            {
                Block = "TestBlock()",
                InsertAfter = "InsertAfter;",
                LeadingTrivia = null
            };

            CodeSnippet snippetWithTrivia = new CodeSnippet
            {
                Block = "TestBlock()",
                InsertAfter = "InsertAfter;",
                LeadingTrivia = new Formatting()
            };

            CodeSnippet snippetNoSpaceTrivia = new CodeSnippet
            {
                Block = "TestBlock()",
                InsertAfter = "InsertAfter;",
                LeadingTrivia = new Formatting { NumberOfSpaces = 0 }
            };

            CodeSnippet snippetFourSpaceTrivia = new CodeSnippet
            {
                Block = "TestBlock()",
                InsertAfter = "InsertAfter;",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            int whitespaceBeingAdded = 4;
            CodeSnippet[] snippets = new CodeSnippet[] { snippetNullTrivia, snippetWithTrivia, snippetNoSpaceTrivia, snippetFourSpaceTrivia };
            var snippetNullTriviaWithAddition = DocumentBuilder.AddLeadingTriviaSpaces(snippetNullTrivia, whitespaceBeingAdded);
            var snippetWithTriviaWithAddition = DocumentBuilder.AddLeadingTriviaSpaces(snippetWithTrivia, whitespaceBeingAdded);
            var snippetNoSpaceTriviaWithAddition = DocumentBuilder.AddLeadingTriviaSpaces(snippetNoSpaceTrivia, whitespaceBeingAdded);
            var snippetFourSpaceTriviaWithAddition = DocumentBuilder.AddLeadingTriviaSpaces(snippetFourSpaceTrivia, whitespaceBeingAdded);

            Assert.True(snippetNullTriviaWithAddition.LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(snippetWithTriviaWithAddition.LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(snippetNoSpaceTriviaWithAddition.LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(snippetFourSpaceTriviaWithAddition.LeadingTrivia.NumberOfSpaces == 4 + whitespaceBeingAdded);

            var formattedCodeSnippets = DocumentBuilder.AddLeadingTriviaSpaces(snippets, whitespaceBeingAdded);
            whitespaceBeingAdded += whitespaceBeingAdded;
            Assert.True(formattedCodeSnippets[0].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[1].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[2].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[3].LeadingTrivia.NumberOfSpaces == 4 + whitespaceBeingAdded);
        }

        [Fact]
        public async Task GetUniqueUsingsTests()
        {
            // Test adding unique usings
            var existingUsings = new[] { "System", "System.Linq" };
            var newUsings = new[] { "System.Collections.Generic", "System.Linq", "System.Text" };
            
            var existingUsingDirectives = DocumentBuilder.CreateUsings(existingUsings);
            var newUsingDirectives = DocumentBuilder.CreateUsings(newUsings);
            
            var uniqueUsings = DocumentBuilder.GetUniqueUsings(existingUsingDirectives, newUsingDirectives);
            
            // Should only add System.Collections.Generic and System.Text (not System.Linq as it already exists)
            Assert.Equal(2, uniqueUsings.Count);
            Assert.Contains(uniqueUsings, u => u.Name.ToString().Equals("System.Collections.Generic"));
            Assert.Contains(uniqueUsings, u => u.Name.ToString().Equals("System.Text"));
            Assert.DoesNotContain(uniqueUsings, u => u.Name.ToString().Equals("System.Linq"));
        }

        [Fact]
        public async Task FilterUsingsWithOptionsTests()
        {
            var codeFile = new CodeFile
            {
                UsingsWithOptions = new[]
                {
                    new CodeBlock { Block = "Microsoft.Graph", Options = new[] { "MicrosoftGraph" } },
                    new CodeBlock { Block = "System.Net.Http", Options = new[] { "DownstreamApi" } },
                    new CodeBlock { Block = "System.Text.Json", Options = null }
                }
            };

            var optionsWithGraph = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = false };
            var optionsWithApi = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = true };
            var optionsWithNeither = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = false };

            var filteredWithGraph = DocumentBuilder.FilterUsingsWithOptions(codeFile, optionsWithGraph);
            var filteredWithApi = DocumentBuilder.FilterUsingsWithOptions(codeFile, optionsWithApi);
            var filteredWithNeither = DocumentBuilder.FilterUsingsWithOptions(codeFile, optionsWithNeither);

            Assert.Contains("Microsoft.Graph", filteredWithGraph);
            Assert.Contains("System.Text.Json", filteredWithGraph);
            Assert.Equal(2, filteredWithGraph.Count);

            Assert.Contains("System.Net.Http", filteredWithApi);
            Assert.Contains("System.Text.Json", filteredWithApi);
            Assert.Equal(2, filteredWithApi.Count);

            Assert.Contains("System.Text.Json", filteredWithNeither);
            Assert.Single(filteredWithNeither);
        }

        [Fact]
        public async Task GetSpecifiedNodeTests()
        {
            var root = await CreateDocument(MinimalProgramCsFile).GetSyntaxRootAsync() as CompilationUnitSyntax;
            var descendants = DocumentBuilder.GetDescendantNodes(root);

            // Test finding a node that exists
            var foundNode = DocumentBuilder.GetSpecifiedNode("builder.Build()", descendants);
            Assert.NotNull(foundNode);
            Assert.Contains("builder.Build()", foundNode.ToString());

            // Test finding a node that doesn't exist
            var notFoundNode = DocumentBuilder.GetSpecifiedNode("NonExistentCode()", descendants);
            Assert.Null(notFoundNode);

            // Test with null or empty specifier
            var nullResult = DocumentBuilder.GetSpecifiedNode(null, descendants);
            Assert.Null(nullResult);

            var emptyResult = DocumentBuilder.GetSpecifiedNode("", descendants);
            Assert.Null(emptyResult);
        }

        [Fact]
        public async Task GetDescendantNodesTests()
        {
            // Test with BlockSyntax
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);
            var blockDescendants = DocumentBuilder.GetDescendantNodes(blockSyntax);
            Assert.NotNull(blockDescendants);
            Assert.NotEmpty(blockDescendants);

            // Test with CompilationUnitSyntax
            var root = await CreateDocument(MinimalProgramCsFile).GetSyntaxRootAsync() as CompilationUnitSyntax;
            var compilationDescendants = DocumentBuilder.GetDescendantNodes(root);
            Assert.NotNull(compilationDescendants);
            Assert.NotEmpty(compilationDescendants);

            // Test with other SyntaxNode types
            var statement = SyntaxFactory.ParseStatement("var x = 10;");
            var otherDescendants = DocumentBuilder.GetDescendantNodes(statement);
            Assert.NotNull(otherDescendants);
        }

        [Fact]
        public async Task GetModifiedMethodTests()
        {
            var editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var methodNode = classSyntax.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(methodNode);

            var methodChanges = new Method
            {
                CodeChanges = new[]
                {
                    new CodeSnippet { Block = "Console.WriteLine(\"Test\");" }
                },
                Attributes = new[]
                {
                    new CodeBlock { Block = "HttpPost" }
                }
            };

            var options = new CodeChangeOptions();
            var output = new StringBuilder();

            var modifiedMethod = DocumentBuilder.GetModifiedMethod("Test.cs", methodNode, methodChanges, options, output);

            Assert.NotNull(modifiedMethod);
            Assert.Contains(modifiedMethod.AttributeLists, al => 
                al.Attributes.Any(attr => attr.ToString().Equals("HttpPost", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task EditMethodReturnTypeTests()
        {
            var editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var methodNode = classSyntax.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(methodNode);

            // Test changing return type
            var methodChanges = new Method
            {
                EditType = new CodeBlock { Block = "Task" }
            };

            var options = new CodeChangeOptions();
            var modifiedMethod = DocumentBuilder.EditMethodReturnType(methodNode, methodChanges, options);

            Assert.NotNull(modifiedMethod);
            Assert.IsType<MethodDeclarationSyntax>(modifiedMethod);
            var modifiedMethodSyntax = (MethodDeclarationSyntax)modifiedMethod;
            Assert.Contains("Task", modifiedMethodSyntax.ReturnType.ToString());

            // Test with null methodChanges
            var unchangedMethod = DocumentBuilder.EditMethodReturnType(methodNode, null, options);
            Assert.Equal(methodNode, unchangedMethod);
        }

        [Fact]
        public async Task AddParametersTests()
        {
            var editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            var classSyntax = await CreateClassSyntax(editor);
            var methodNode = classSyntax.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(methodNode);

            var addParameters = new[]
            {
                new CodeBlock { Block = "string name" },
                new CodeBlock { Block = "int id" }
            };

            var options = new CodeChangeOptions();
            var modifiedMethod = DocumentBuilder.AddParameters(methodNode, addParameters, options);

            Assert.NotNull(modifiedMethod);
            Assert.Equal(2, modifiedMethod.ParameterList.Parameters.Count);
        }

        [Fact]
        public async Task ModifyMethodTests()
        {
            var statement = SyntaxFactory.ParseStatement("{ }");
            var blockSyntax = SyntaxFactory.Block(statement);
            
            var codeChange = new CodeSnippet
            {
                Block = "Console.WriteLine(\"Hello\");",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 },
                TrailingTrivia = new Formatting { Newline = true }
            };

            var output = new StringBuilder();
            var modifiedMethod = DocumentBuilder.ModifyMethod(blockSyntax, codeChange, output);

            Assert.NotNull(modifiedMethod);
            Assert.Contains("Console.WriteLine", modifiedMethod.ToString());
        }

        [Fact]
        public async Task UpdateMethodTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChange = new CodeSnippet
            {
                Block = "app.UseAuthentication();",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 },
                TrailingTrivia = new Formatting { Newline = true }
            };

            var updatedMethod = DocumentBuilder.UpdateMethod(blockSyntax, codeChange);

            Assert.NotNull(updatedMethod);
            Assert.Contains("app.UseAuthentication", updatedMethod.ToString());
            Assert.Contains("app.UseRouting", updatedMethod.ToString());
        }

        [Fact]
        public async Task GetNodeWithUpdatedLambdaTests()
        {
            // Create a simple lambda expression for testing
            var lambdaExpression = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                SyntaxFactory.Block()
            );

            var parentNode = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Action"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(lambdaExpression)
                        )
                    )
                )
            );

            var codeChange = new CodeSnippet
            {
                Block = "Console.WriteLine(x);",
                Parameter = "y",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var updatedParent = DocumentBuilder.GetNodeWithUpdatedLambda(lambdaExpression, codeChange, parentNode);

            Assert.NotNull(updatedParent);
            // The lambda should be updated within the parent
            Assert.Contains("Console.WriteLine", updatedParent.ToString());
        }

        [Fact]
        public async Task AddExpressionToParentTests()
        {
            var root = await CreateDocument(MinimalProgramCsFile).GetSyntaxRootAsync() as CompilationUnitSyntax;
            
            var codeChange = new CodeSnippet
            {
                Block = "UseHttpsRedirection()",
                Parent = "app",
                LeadingTrivia = new Formatting { NumberOfSpaces = 0 }
            };

            var modifiedRoot = DocumentBuilder.AddExpressionToParent(root, codeChange);

            Assert.NotNull(modifiedRoot);
        }

        [Fact]
        public void AddMethodParametersTests()
        {
            var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "TestMethod"
            ).WithParameterList(SyntaxFactory.ParameterList());

            var methodChanges = new Method
            {
                AddParameters = new[]
                {
                    new CodeBlock { Block = "string name" },
                    new CodeBlock { Block = "int age" }
                }
            };

            var options = new CodeChangeOptions();
            var modifiedMethod = DocumentBuilder.AddMethodParameters(method, methodChanges, options);

            Assert.NotNull(modifiedMethod);
            Assert.Equal(2, modifiedMethod.ParameterList.Parameters.Count);

            // Test with null or empty parameters
            var unchangedMethod = DocumentBuilder.AddMethodParameters(method, null, options);
            Assert.Equal(method, unchangedMethod);
        }

        [Fact]
        public void AddCodeSnippetsToMethodTests()
        {
            var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "TestMethod"
            ).WithBody(SyntaxFactory.Block());

            var methodChanges = new Method
            {
                CodeChanges = new[]
                {
                    new CodeSnippet
                    {
                        Block = "Console.WriteLine(\"Hello\");",
                        LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
                    }
                }
            };

            var options = new CodeChangeOptions();
            var output = new StringBuilder();

            var modifiedMethod = DocumentBuilder.AddCodeSnippetsToMethod("Test.cs", method, methodChanges, options, output);

            Assert.NotNull(modifiedMethod);
            Assert.Contains("Console.WriteLine", modifiedMethod.ToString());
        }

        [Fact]
        public async Task ApplyChangesToMethodWithGlobalStatementsTests()
        {
            var root = await CreateDocument(MinimalProgramCsFile).GetSyntaxRootAsync() as CompilationUnitSyntax;
            
            var codeChanges = new[]
            {
                new CodeSnippet
                {
                    Block = "builder.Services.AddAuthentication();",
                    InsertAfter = "builder.Services.AddRazorPages()",
                    LeadingTrivia = new Formatting { NumberOfSpaces = 0, Newline = true }
                }
            };

            var output = new StringBuilder();
            var modifiedRoot = DocumentBuilder.ApplyChangesToMethod(root, codeChanges, "Program.cs", output) as CompilationUnitSyntax;

            Assert.NotNull(modifiedRoot);
            Assert.Contains("AddAuthentication", modifiedRoot.ToString());
        }

        [Fact]
        public async Task ApplyChangesToMethodWithBlockSyntaxTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChanges = new[]
            {
                new CodeSnippet
                {
                    Block = "app.UseAuthentication();",
                    LeadingTrivia = new Formatting { NumberOfSpaces = 4, Newline = true }
                },
                new CodeSnippet
                {
                    Block = "app.UseAuthorization();",
                    LeadingTrivia = new Formatting { NumberOfSpaces = 4, Newline = true }
                }
            };

            var output = new StringBuilder();
            var modifiedBlock = DocumentBuilder.ApplyChangesToMethod(blockSyntax, codeChanges, "Startup.cs", output);

            Assert.NotNull(modifiedBlock);
            Assert.Contains("UseAuthentication", modifiedBlock.ToString());
            Assert.Contains("UseAuthorization", modifiedBlock.ToString());
        }

        [Fact]
        public void UpdateMethodWithInsertBeforeTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); app.Run(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChange = new CodeSnippet
            {
                Block = "app.UseAuthentication();",
                InsertBefore = new[] { "app.Run()" },
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var updatedMethod = DocumentBuilder.UpdateMethod(blockSyntax, codeChange);

            Assert.NotNull(updatedMethod);
            Assert.Contains("UseAuthentication", updatedMethod.ToString());
            
            // Verify insertion order: UseRouting -> UseAuthentication -> Run
            var methodString = updatedMethod.ToString();
            var routingIndex = methodString.IndexOf("UseRouting");
            var authIndex = methodString.IndexOf("UseAuthentication");
            var runIndex = methodString.IndexOf("Run()");
            
            Assert.True(routingIndex < authIndex && authIndex < runIndex);
        }

        [Fact]
        public void UpdateMethodWithInsertAfterTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); app.Run(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChange = new CodeSnippet
            {
                Block = "app.UseAuthentication();",
                InsertAfter = "app.UseRouting()",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var updatedMethod = DocumentBuilder.UpdateMethod(blockSyntax, codeChange);

            Assert.NotNull(updatedMethod);
            Assert.Contains("UseAuthentication", updatedMethod.ToString());
            
            // Verify insertion order: UseRouting -> UseAuthentication -> Run
            var methodString = updatedMethod.ToString();
            var routingIndex = methodString.IndexOf("UseRouting");
            var authIndex = methodString.IndexOf("UseAuthentication");
            var runIndex = methodString.IndexOf("Run()");
            
            Assert.True(routingIndex < authIndex && authIndex < runIndex);
        }

        [Fact]
        public void UpdateMethodWithPrependTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChange = new CodeSnippet
            {
                Block = "var config = builder.Configuration;",
                Prepend = true,
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var updatedMethod = DocumentBuilder.UpdateMethod(blockSyntax, codeChange);

            Assert.NotNull(updatedMethod);
            Assert.Contains("var config", updatedMethod.ToString());
            
            // Verify prepending: config should come before UseRouting
            var methodString = updatedMethod.ToString();
            var configIndex = methodString.IndexOf("var config");
            var routingIndex = methodString.IndexOf("UseRouting");
            
            Assert.True(configIndex < routingIndex);
        }

        [Fact]
        public void UpdateMethodWithCheckBlockTests()
        {
            var blockStatement = SyntaxFactory.ParseStatement("{ app.UseRouting(); }");
            var blockSyntax = SyntaxFactory.Block(blockStatement);

            var codeChange = new CodeSnippet
            {
                Block = "app.UseAuthentication();",
                CheckBlock = "app.UseRouting()",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            // Should not add because CheckBlock already exists
            var updatedMethod = DocumentBuilder.UpdateMethod(blockSyntax, codeChange);
            Assert.Equal(blockSyntax, updatedMethod);

            // Now test with a CheckBlock that doesn't exist
            var codeChange2 = new CodeSnippet
            {
                Block = "app.UseAuthentication();",
                CheckBlock = "app.UseAuthorization()",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var updatedMethod2 = DocumentBuilder.UpdateMethod(blockSyntax, codeChange2);
            Assert.NotEqual(blockSyntax, updatedMethod2);
            Assert.Contains("UseAuthentication", updatedMethod2.ToString());
        }

        [Fact]
        public async Task AddLambdaToParentTests()
        {
            // Create a parent node with an argument list
            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("Configure"),
                SyntaxFactory.ArgumentList()
            );

            var codeChange = new CodeSnippet
            {
                Block = "Console.WriteLine(options);",
                Parameter = "options",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var children = DocumentBuilder.GetDescendantNodes(invocation);
            var updatedParent = DocumentBuilder.AddLambdaToParent(invocation, children, codeChange);

            Assert.NotNull(updatedParent);
            Assert.Contains("options", updatedParent.ToString());
            Assert.Contains("Console.WriteLine", updatedParent.ToString());
        }

        [Fact]
        public void AddLambdaToParentWithExistingBlockTests()
        {
            // Test that it doesn't add duplicate lambda when block already exists
            var lambdaExpression = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("Console.WriteLine(x);")
                )
            );

            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("Action"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(lambdaExpression)
                    )
                )
            );

            var codeChange = new CodeSnippet
            {
                Block = "Console.WriteLine(x);",
                Parameter = "x",
                LeadingTrivia = new Formatting { NumberOfSpaces = 4 }
            };

            var children = DocumentBuilder.GetDescendantNodes(invocation);
            var updatedParent = DocumentBuilder.AddLambdaToParent(invocation, children, codeChange);

            // Should return original parent since block already exists
            Assert.Equal(invocation, updatedParent);
        }

        [Fact]
        public async Task WriteToClassFileAsyncTests()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cs");
            try
            {
                DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
                CodeFile codeFile = new CodeFile
                {
                    Usings = new[] { "System.Threading.Tasks" }
                };
                DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new MSIdentity.Shared.ConsoleLogger());
                docBuilder.AddUsings(new CodeChangeOptions());

                await docBuilder.WriteToClassFileAsync(tempFilePath);

                Assert.True(File.Exists(tempFilePath));
                var fileContent = File.ReadAllText(tempFilePath);
                Assert.Contains("System.Threading.Tasks", fileContent);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [Fact]
        public async Task ApplyTextReplacementsTests()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            try
            {
                File.WriteAllText(tempFilePath, "Original content");

                var document = CreateDocument("Original content");
                var codeFile = new CodeFile
                {
                    Replacements = new[]
                    {
                        new CodeSnippet
                        {
                            Block = "New content",
                            CheckBlock = "New",
                            ReplaceSnippet = new[] { "Original content" }
                        }
                    }
                };

                var fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((path, content) => File.WriteAllText(path, content));

                var options = new CodeChangeOptions();
                await DocumentBuilder.ApplyTextReplacements(codeFile, document, options, fileSystem.Object);

                // Verify WriteAllText was called
                fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

    }
}
