using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter.CodeChange;
using Microsoft.DotNet.MSIdentity.Tool;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
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
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            docBuilder.AddUsings();
            Document changedDoc = docBuilder.GetDocument();
            var root = (CompilationUnitSyntax)await changedDoc.GetSyntaxRootAsync();

            Assert.True(root.Usings.Count == 4);
            foreach (var usingString in usings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(root.Usings, node => node.Name.ToString().Equals(usingString));
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
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            docBuilder.AddUsings();
            Document changedDoc = docBuilder.GetDocument();
            var root = (CompilationUnitSyntax)await changedDoc.GetSyntaxRootAsync();

            Assert.True(root.Usings.Count == 3);
            foreach (var usingString in usings)
            {
                if (!string.IsNullOrEmpty(usingString))
                {
                    Assert.Contains(root.Usings, node => node.Name.ToString().Equals(usingString));
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

            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            docBuilder.AddUsings();

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
            CodeFile codeFile = new CodeFile
            {
                ClassProperties = properties
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            classSyntax = docBuilder.AddProperties(classSyntax);
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
            CodeFile codeFile = new CodeFile
            {
                ClassAttributes = attributes
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            classSyntax = docBuilder.AddClassAttributes(classSyntax);
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
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());
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

        [Theory]
        [InlineData(new object[] { new string[] { "Authorize", "Theory", "Empty", "Controller", "", null } })]
        public async Task CreateAttributeListTests(string[] attributeStrings)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());
            var attributes = docBuilder.CreateAttributeList(attributeStrings, new SyntaxList<AttributeListSyntax>(), new SyntaxTriviaList());
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
        [InlineData(new object[] { new string[] { "string.ToString()", "bool.ToString()", "IServices.AddRazorPages()", "IWebSomething.DoWebStuff()", "", "Wrong.Missing()" },
                                   new string[] { "password.ToString()", "IsTrue.ToString()", "services.AddRazorPages()", "app.DoWebStuff()", "", "Wrong.Missing()" } })]
        public async Task FormatCodeBlockTests(string[] parameterKeys, string[] formattedValues)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());
            for (int i = 0; i < parameterKeys.Length; i++)
            {
                string paramKey = parameterKeys[i];
                string correctCodeBlock = formattedValues[i];
                string formattedCodeBlock = docBuilder.FormatCodeBlock(paramKey, ParameterValues);
                Assert.Equal(formattedCodeBlock, correctCodeBlock);
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "IServiceCollection", "IApplcationBuilder", "IWebHostEnvironment", "string", "bool" },
                                   new string[] { "services", "app", "env", "testString", "testBool"} })]
        public async Task VerfiyParametersTests(string[] types, string[] vals)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());
            var paramList = CreateParameterList(types, vals);
            var paramDict = docBuilder.VerfiyParameters(types, paramList);
            Assert.True(paramDict != null);

            foreach (var type in types)
            {
                Assert.True(paramDict.TryGetValue(type, out string value) && !string.IsNullOrEmpty(value));
            }
        }

        [Fact]
        public async Task StatementExistsTests()
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());
            //create a block with app.UseRouting();
            StatementSyntax block = SyntaxFactory.ParseStatement(
                @"
                {
                    app.UseRouting();
                }");
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

            StatementSyntax emptyBlock = SyntaxFactory.ParseStatement(
                @"
                {                   
                }");

            BlockSyntax blockSyntax = SyntaxFactory.Block(block);
            BlockSyntax emptyBlockSyntax = SyntaxFactory.Block(emptyBlock);
            BlockSyntax denseBlockSyntax = SyntaxFactory.Block(denseBlock);
            StatementSyntax statement = SyntaxFactory.ParseStatement("app.UseRouting();");
            StatementSyntax statement2 = SyntaxFactory.ParseStatement("app.UseDeveloperExceptionPage();");
            StatementSyntax statement3 = SyntaxFactory.ParseStatement("endpoints.MapRazorPages();");
            StatementSyntax statement4 = SyntaxFactory.ParseStatement("env.IsDevelopment()");
            StatementSyntax statement5 = SyntaxFactory.ParseStatement("services.AddRazorPages()");
            StatementSyntax statement6 = SyntaxFactory.ParseStatement("services.AddRazorPages().AddMvcOptions(options => {})");

            Assert.True(docBuilder.StatementExists(blockSyntax, statement));
            Assert.False(docBuilder.StatementExists(emptyBlockSyntax, statement));
            Assert.False(docBuilder.StatementExists(denseBlockSyntax, statement));
            Assert.True(docBuilder.StatementExists(denseBlockSyntax, statement2));
            Assert.True(docBuilder.StatementExists(denseBlockSyntax, statement3));
            Assert.True(docBuilder.StatementExists(denseBlockSyntax, statement4));
            Assert.True(docBuilder.StatementExists(denseBlockSyntax, statement5));
            Assert.True(docBuilder.StatementExists(denseBlockSyntax, statement6));
        }

        [Theory]
        [InlineData(new object[] { new string[] { "static readonly string[] scopeRequiredByApi = new string[] { \"access_as_user\" }", "public string Name { get; set; }", "bool IsProperty { get; set; } = false", "", null },
                                   new string[] { "public string Name { get; set; }", "bool IsProperty { get; set; } = false" } })]
        public async Task CreateClassPropertiesTests(string[] properties, string[] nonDuplicateProperties)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));
            DocumentEditor emptyDocEditor = await DocumentEditor.CreateAsync(CreateDocument(EmptyDocument));

            var classSyntax = await CreateClassSyntax(editor);
            var emptyClassSyntax = await CreateClassSyntax(emptyDocEditor);
            CodeFile codeFile = new CodeFile
            {
                ClassProperties = properties
            };
            DocumentBuilder docBuilder = new DocumentBuilder(editor, codeFile, new ConsoleLogger());
            DocumentBuilder emptyDocBuilder = new DocumentBuilder(emptyDocEditor, codeFile, new ConsoleLogger());

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
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());

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
        [InlineData(new object[] { new string[] { "Authorize", "Empty" },
                                   new string[] { "Theory", "Controller" },
                                   new string[] { "", null }})]
        public async Task AttributeExistsTests(string[] existingAttributes, string[] nonExistingAttributes, string[] invalidAttributes)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(CreateDocument(FullDocument));

            var classSyntax = await CreateClassSyntax(editor);
            var attributeLists = classSyntax.AttributeLists;
            DocumentBuilder docBuilder = new DocumentBuilder(editor, new CodeFile(), new ConsoleLogger());

            foreach (var attribute in existingAttributes)
            {
                Assert.True(docBuilder.AttributeExists(attribute, attributeLists));
            }

            foreach (var attribute in nonExistingAttributes)
            {
                Assert.False(docBuilder.AttributeExists(attribute, attributeLists));
            }

            foreach (var attribute in invalidAttributes)
            {
                Assert.False(docBuilder.AttributeExists(attribute, attributeLists));
            }
        }
    }
}
