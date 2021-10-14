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

        [Fact]
        public void FilterOptionsTests()
        {
            var optionsWithGraph = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = false };
            var optionsWithApi = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = true };
            var optionsWithBoth = new CodeChangeOptions { MicrosoftGraph = true, DownstreamApi = true };
            var optionsWithNeither = new CodeChangeOptions { MicrosoftGraph = false, DownstreamApi = false };

            var graphOptions = new string[] { "MicrosoftGraph" } ;
            var apiOptions = new string[] { "DownstreamApi" } ;
            var bothOptions = new string[] { "DownstreamApi", "MicrosoftGraph" } ;
            var neitherOptions = new string[] { } ;

            Assert.True(DocumentBuilder.FilterOptions(graphOptions, optionsWithGraph));
            Assert.False(DocumentBuilder.FilterOptions(graphOptions, optionsWithApi));
            Assert.True(DocumentBuilder.FilterOptions(graphOptions, optionsWithBoth));
            Assert.False(DocumentBuilder.FilterOptions(graphOptions, optionsWithNeither));

            Assert.False(DocumentBuilder.FilterOptions(apiOptions, optionsWithGraph));
            Assert.True(DocumentBuilder.FilterOptions(apiOptions, optionsWithApi));
            Assert.True(DocumentBuilder.FilterOptions(apiOptions, optionsWithBoth));
            Assert.False(DocumentBuilder.FilterOptions(apiOptions, optionsWithNeither));

            Assert.True(DocumentBuilder.FilterOptions(bothOptions, optionsWithGraph));
            Assert.True(DocumentBuilder.FilterOptions(bothOptions, optionsWithApi));
            Assert.True(DocumentBuilder.FilterOptions(bothOptions, optionsWithBoth));
            Assert.False(DocumentBuilder.FilterOptions(bothOptions, optionsWithNeither));

            Assert.True(DocumentBuilder.FilterOptions(neitherOptions, optionsWithGraph));
            Assert.True(DocumentBuilder.FilterOptions(neitherOptions, optionsWithApi));
            Assert.True(DocumentBuilder.FilterOptions(neitherOptions, optionsWithBoth));
            Assert.True(DocumentBuilder.FilterOptions(neitherOptions, optionsWithNeither));
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
            var neitherBlock = new CodeBlock { Block = "NeitherProperty", Options = new string[] {  } };
            
            var codeBlocks = new CodeBlock[] { graphBlock, apiBlock, bothBlock, neitherBlock };
            var filteredWithGraph = DocumentBuilder.FilterCodeBlocks(codeBlocks, optionsWithGraph);
            var filteredWithApi = DocumentBuilder.FilterCodeBlocks(codeBlocks, optionsWithApi);
            var filteredWithBoth = DocumentBuilder.FilterCodeBlocks(codeBlocks, optionsWithBoth);
            var filteredWithNeither = DocumentBuilder.FilterCodeBlocks(codeBlocks, optionsWithNeither);

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
            var filteredWithGraph = DocumentBuilder.FilterCodeSnippets(codeSnippets, optionsWithGraph);
            var filteredWithApi = DocumentBuilder.FilterCodeSnippets(codeSnippets, optionsWithApi);
            var filteredWithBoth = DocumentBuilder.FilterCodeSnippets(codeSnippets, optionsWithBoth);
            var filteredWithNeither = DocumentBuilder.FilterCodeSnippets(codeSnippets, optionsWithNeither);

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

/*        [Theory]
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
        }*/

        [Theory]
        [InlineData(new object[] { new string[] { "System", "System.Test", "System.Data", "", null } })]
        public async Task CreateUsingsTests(string[] usingsStrings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            //Add usings
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var usingDirectiveSyntax = docBuilder.CreateUsings(usingsStrings);
            Assert.True(usingDirectiveSyntax.Length == 3);

            foreach (var usingString in usingsStrings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(usingDirectiveSyntax, usingDirective => usingDirective.Name.ToString().Equals(usingString));
                }
            }
        }

/*        [Theory]
        [InlineData(new object[] { new string[] { "Authorize", "Theory", "Empty", "Controller", "", null } })]
        public async Task CreateAttributeListTests(string[] attributeStrings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var classAttributes = attributeStrings.Select(at => new CodeBlock() { Block = at }).ToArray();
            var attributes = docBuilder.CreateAttributeList(classAttributes, new SyntaxList<AttributeListSyntax>(), new SyntaxTriviaList());
            Assert.True(attributes.Count == 4);

            foreach (var attributeString in attributeStrings)
            {
                if (!string.IsNullOrEmpty(attributeString))
                {
                    Assert.Contains(attributes, al => al.Attributes.Where(attr => attr.ToString().Equals(attributeString, StringComparison.OrdinalIgnoreCase)).Any());
                }
            }
        }*/

        [Theory]
        [InlineData(new object[] { new string[] { "IServiceCollection", "IApplcationBuilder", "IWebHostEnvironment", "string", "bool" },
                                   new string[] { "services", "app", "env", "testString", "testBool"} })]
        public async Task VerfiyParametersTests(string[] types, string[] vals)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new MSIdentity.Shared.ConsoleLogger());
            var paramList = CreateParameterList(types, vals);
            var paramDict = docBuilder.VerfiyParameters(types, paramList);
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
                Assert.True(docBuilder.PropertyExists(property, members));
            }

            //mpm 
            foreach (var property in nonExistingProperties)
            {
                Assert.False(docBuilder.PropertyExists(property, members));
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public string Name { get; set; }", "bool IsProperty { get; set; } = false" },
                                   new string[] { "var app = builder.Build()", "app.UseHttpsRedirection()", "app.UseStaticFiles()", "app.UseRouting()", "bool IsProperty { get; set; } = false" } }
        )]
        public async Task AddGlobalStatementsTests(string[] statementsToAdd, string[] duplicateStatements)
        {
            Document minimalProgramCsDoc = CreateDocument(MinimalProgramCsFile);
            var root = await minimalProgramCsDoc.GetSyntaxRootAsync() as CompilationUnitSyntax;
            foreach (var statementToAdd in statementsToAdd)
            {
                var expression = SyntaxFactory.ParseStatement(statementToAdd);
                var globalStatement = SyntaxFactory.GlobalStatement(expression).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                root = DocumentBuilder.AddGlobalStatements(new CodeSnippet { Block = statementToAdd }, root);
                Assert.True(ProjectModifierHelper.GlobalStatementExists(root, globalStatement));
            }
            var statementCount = root.Members.Count;
            foreach (var duplicateStatement in duplicateStatements)
            {
                root = DocumentBuilder.AddGlobalStatements(new CodeSnippet { Block = duplicateStatement }, root);
                Assert.Equal(statementCount, root.Members.Count);
            }
        }
    }
}
