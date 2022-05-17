using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
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
        [InlineData(new object[] { new string[] { "System", "System.Duplicate"} })]
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
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public string Name { get; set; }", "bool IsProperty { get; set; } = false","" , null } })]
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
        public async Task PropertyExistsTests(string [] existingProperties, string[] nonExistingProperties)
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
            var modifiedRoot = DocumentBuilder.ApplyChangesToMethod(root, codeChanges) as CompilationUnitSyntax;

            foreach (var statementToAdd in statementsToAdd)
            {
                var expression = SyntaxFactory.ParseStatement(statementToAdd);
                var globalStatement = SyntaxFactory.GlobalStatement(expression).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                Assert.True(ProjectModifierHelper.GlobalStatementExists(modifiedRoot, globalStatement));
            }

            var duplicates = duplicateStatements.Select(s => new CodeSnippet { Block = s }).ToArray();
            var rootWithDuplicates = DocumentBuilder.ApplyChangesToMethod(modifiedRoot, duplicates) as CompilationUnitSyntax;
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
            Assert.True(snippetNoSpaceTriviaWithAddition.LeadingTrivia.NumberOfSpaces ==  whitespaceBeingAdded);
            Assert.True(snippetFourSpaceTriviaWithAddition.LeadingTrivia.NumberOfSpaces == 4 + whitespaceBeingAdded);

            var formattedCodeSnippets = DocumentBuilder.AddLeadingTriviaSpaces(snippets, whitespaceBeingAdded);
            whitespaceBeingAdded += whitespaceBeingAdded;
            Assert.True(formattedCodeSnippets[0].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[1].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[2].LeadingTrivia.NumberOfSpaces == whitespaceBeingAdded);
            Assert.True(formattedCodeSnippets[3].LeadingTrivia.NumberOfSpaces == 4 + whitespaceBeingAdded);
        }

    }
}
