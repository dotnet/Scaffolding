using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    internal class DocumentBuilder
    {
        private DocumentEditor _documentEditor;
        private CodeFile _codeFile;
        private CompilationUnitSyntax _docRoot;
        private IConsoleLogger _consoleLogger;

        public DocumentBuilder(
            DocumentEditor  documentEditor,
            CodeFile codeFile,
            IConsoleLogger consoleLogger)
        {
            _documentEditor = documentEditor ?? throw new ArgumentNullException(nameof(_documentEditor));
            _codeFile = codeFile ?? throw new ArgumentNullException(nameof(codeFile));
            _consoleLogger = consoleLogger ?? throw new ArgumentNullException(nameof(consoleLogger));
            _docRoot = (CompilationUnitSyntax)_documentEditor.OriginalRoot ?? throw new ArgumentNullException(nameof(_documentEditor.OriginalRoot));
        }

        public CompilationUnitSyntax AddUsings(CodeChangeOptions options)
        {
            //adding usings
            if (_codeFile.UsingsWithOptions != null && _codeFile.UsingsWithOptions.Any())
            {
                var usingsWithOptions = FilterUsingsWithOptions(_codeFile, options);
                var usingList = _codeFile.Usings.ToList();
                usingList.AddRange(usingsWithOptions);
                _codeFile.Usings = usingList.ToArray();
            }
            var usingNodes = CreateUsings(_codeFile.Usings);
            if (usingNodes.Any() && _docRoot.Usings.Count == 0)
            {
                return _docRoot.WithUsings(new SyntaxList<UsingDirectiveSyntax>(usingNodes));
            }
            else
            {
                var newRoot = _docRoot;
                foreach (var usingNode in usingNodes)
                {
                    //if usings exist (very likely), add it after the last one.
                    if (newRoot.Usings.Count > 0)
                    {
                        var usingName = usingNode.Name.ToString();
                        if (!newRoot.Usings.Any(node => node.Name.ToString().Equals(usingName)))
                        {
                           newRoot = newRoot.InsertNodesAfter(newRoot.Usings.Last(), new List<SyntaxNode> { usingNode } );
                        }
                    }
                }
                return newRoot;
            }
        }

        internal static IList<string> FilterUsingsWithOptions(CodeFile codeFile, CodeChangeOptions options)
        {
            List<string> usingsWithOptions = new List<string>();
            if (codeFile != null)
            {
                var filteredCodeBlocks = codeFile.UsingsWithOptions.Where(us => ProjectModifierHelper.FilterOptions(us.Options, options)).ToList();
                if (filteredCodeBlocks.Any())
                {
                    usingsWithOptions = filteredCodeBlocks.Select(us => us.Block)?.ToList();
                }
            }

            return usingsWithOptions;
        }

        //Add class members to the top of the class.
        public ClassDeclarationSyntax AddProperties(ClassDeclarationSyntax classDeclarationSyntax, CodeChangeOptions options)
        { 
            var modifiedClassDeclarationSyntax = classDeclarationSyntax;
            if (_codeFile.ClassProperties != null && _codeFile.ClassProperties.Any() && classDeclarationSyntax != null)
            {
                _codeFile.ClassProperties = ProjectModifierHelper.FilterCodeBlocks(_codeFile.ClassProperties, options);
                //get a sample member for leading trivia. Trailing trivia will still be semi colon and new line.
                var sampleMember = modifiedClassDeclarationSyntax.Members.FirstOrDefault();
                var memberLeadingTrivia = sampleMember?.GetLeadingTrivia() ?? new SyntaxTriviaList(SyntaxFactory.Whitespace("    "));
                var memberTrailingTrivia = new SyntaxTriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
                
                //create MemberDeclarationSyntax[] with all the Property strings. 
                var classProperties = CreateClassProperties(modifiedClassDeclarationSyntax.Members, memberLeadingTrivia, memberTrailingTrivia);

                if (classProperties.Length > 0)
                {
                    //add to the top of the class.
                    var members = modifiedClassDeclarationSyntax.Members;
                    foreach (var property in classProperties)
                    {
                        members = members.Insert(0, property);
                    }
                    modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax
                        .WithMembers(members);
                }
            }
            return modifiedClassDeclarationSyntax;
        }



        //Add class attributes '[Attribute]' to a class.
        public ClassDeclarationSyntax AddClassAttributes(ClassDeclarationSyntax classDeclarationSyntax, CodeChangeOptions options)
        {
            var modifiedClassDeclarationSyntax = classDeclarationSyntax;
            if (_codeFile.ClassAttributes != null && _codeFile.ClassAttributes.Any() && classDeclarationSyntax != null)
            {
                _codeFile.ClassAttributes = ProjectModifierHelper.FilterCodeBlocks(_codeFile.ClassAttributes, options);
                var classAttributes = CreateAttributeList(_codeFile.ClassAttributes, modifiedClassDeclarationSyntax.AttributeLists, classDeclarationSyntax.GetLeadingTrivia());
                modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax.WithAttributeLists(classAttributes);
            }
            return modifiedClassDeclarationSyntax;
        }

        public async Task WriteToClassFileAsync(string filePath)
        {
            var changedDocument = GetDocument();
            var classFileTxt = await changedDocument.GetTextAsync();
            File.WriteAllText(filePath, classFileTxt.ToString());
            _consoleLogger.LogMessage($"Modified {filePath}.\n");
        }

        internal ClassDeclarationSyntax AddMethodParameters( ClassDeclarationSyntax modifiedClassDeclarationSyntax, CodeChangeOptions options)
        {
            foreach (var method in _codeFile.Methods)
            { //AddParameters
                var methodName = method.Key;
                var methodChanges = method.Value;
                if (!string.IsNullOrEmpty(methodName) &&
                    methodChanges != null &&
                    methodChanges.AddParameters != null &&
                    methodChanges.AddParameters.Any())
                {
                    //get method node from ClassDeclarationSyntax
                    IDictionary<string, string> parameterValues = null;

                    var originalMethodNode = modifiedClassDeclarationSyntax?
                        .DescendantNodes()
                        .Where(
                            node => node is MethodDeclarationSyntax mds &&
                            mds.Identifier.ValueText.Equals(methodName) &&
                            (parameterValues = VerfiyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                        .FirstOrDefault();

                    var methodNode = modifiedClassDeclarationSyntax?
                        .DescendantNodes()
                        .Where(
                            node => node is MethodDeclarationSyntax mds &&
                            mds.Identifier.ValueText.Equals(methodName) &&
                            (parameterValues = VerfiyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                        .FirstOrDefault();

                    if (methodNode == null)
                    {
                        originalMethodNode = modifiedClassDeclarationSyntax?
                        .DescendantNodes()
                        .Where(
                            node => node is ConstructorDeclarationSyntax mds &&
                            mds.Identifier.ValueText.Equals(methodName))
                        .FirstOrDefault();

                        methodNode = modifiedClassDeclarationSyntax?
                        .DescendantNodes()
                        .Where(
                            node => node is ConstructorDeclarationSyntax mds &&
                            mds.Identifier.ValueText.Equals(methodName))
                        .FirstOrDefault();
                    }

                    //methodNode is either MethodDeclarationSynax, or ConstructorDeclarationSyntax
                    if (methodNode != null)
                    {
                        //Filter for CodeChangeOptions
                        methodChanges.AddParameters = ProjectModifierHelper.FilterCodeBlocks(methodChanges.AddParameters, options);
                    }
                    
                    if (methodNode is MethodDeclarationSyntax methodDeclratation)
                    {
                        methodNode = AddParameters(methodDeclratation, methodChanges.AddParameters, options);
                    }
                    else if (methodNode is ConstructorDeclarationSyntax constructorDeclaration)
                    {
                        methodNode = AddParameters(constructorDeclaration, methodChanges.AddParameters, options);
                    }
                    modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax.ReplaceNode(originalMethodNode, methodNode);
                }
            }
            return modifiedClassDeclarationSyntax;
        }

        //Add all the different code snippet.
        internal ClassDeclarationSyntax AddCodeSnippets(ClassDeclarationSyntax modifiedClassDeclarationSyntax, CodeChangeOptions options)
        {                        
            //code changes are chunked together by methods. Easier for Document modifications.
            if (_codeFile.Methods != null)
            {
                foreach (var method in _codeFile.Methods)
                {
                    var methodName = method.Key;
                    var methodChanges = method.Value;

                    if (!string.IsNullOrEmpty(methodName) &&
                        methodChanges != null &&
                        methodChanges.CodeChanges != null)
                    {
                        //get method node from ClassDeclarationSyntax
                        IDictionary<string, string> parameterValues = null;
                        var methodNode = modifiedClassDeclarationSyntax?
                            .DescendantNodes()
                            .Where(
                                node => node is MethodDeclarationSyntax mds &&
                                mds.Identifier.ValueText.Equals(methodName) &&
                                (parameterValues = VerfiyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                            .FirstOrDefault();

                        //check for constructor as its not a MethodDeclarationSyntax but ConstructorDeclarationSyntax
                        if (methodNode == null)
                        {
                            methodNode = modifiedClassDeclarationSyntax?
                            .DescendantNodes()
                            .Where(node2 => node2 is ConstructorDeclarationSyntax cds &&
                               cds.Identifier.ValueText.Equals(methodName) && 
                               (parameterValues = VerfiyParameters(methodChanges.Parameters, cds.ParameterList.Parameters.ToList())) != null)
                            .FirstOrDefault();
                        }
                        var newMethod = methodNode;
                        
                        //get method's BlockSyntax
                        var blockSyntaxNode = newMethod?.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
                        var modifiedBlockSyntaxNode = newMethod?.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
                        if (modifiedBlockSyntaxNode != null && parameterValues != null && blockSyntaxNode != null && modifiedClassDeclarationSyntax != null)
                        {
                            methodChanges.CodeChanges = ProjectModifierHelper.FilterCodeSnippets(methodChanges.CodeChanges, options);
                            //do all the CodeChanges for the method.
                            foreach (var change in methodChanges.CodeChanges)
                            {
                                //filter by options
                                if (!string.IsNullOrEmpty(change.Block))
                                {
                                    //CodeChange.Parent and CodeChange.Type go together.
                                    if (!string.IsNullOrEmpty(change.Parent) && change.Options.Any())
                                    {
                                        modifiedBlockSyntaxNode = AddCodeSnippetOnParent(change, modifiedBlockSyntaxNode, parameterValues);
                                    }

                                    //if there is no CodeChange.Parent, check if to InsertAfter a statement.
                                    else if (!string.IsNullOrEmpty(change.InsertAfter))
                                    {
                                        modifiedBlockSyntaxNode = AddCodeSnippetAfterNode(change, modifiedBlockSyntaxNode, parameterValues);
                                    }
                                    //if there are no Parent or InsertAfter in CodeChange,
                                    //just insert statement at the end of the block
                                    else
                                    {
                                        modifiedBlockSyntaxNode = AddCodeSnippetInPlace(change, modifiedBlockSyntaxNode, parameterValues);
                                    }
                                }
                            }
                            //replace the BlockSyntax of a MethodDeclarationSyntax of a ClassDeclarationSyntax
                            modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax.ReplaceNode(blockSyntaxNode, modifiedBlockSyntaxNode);
                            //newMethod = newMethod.ReplaceNode(blockSyntaxNode, blockSyntaxNode);
                            //modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax.ReplaceNode(methodNode, newMethod);
                        }
                    }
                }
            }
            return modifiedClassDeclarationSyntax;
        }

        internal ClassDeclarationSyntax EditMethodTypes(ClassDeclarationSyntax modifiedClassDeclarationSyntax, CodeChangeOptions options)
        {
            foreach (var method in _codeFile.Methods)
            { //AddParameters
                var methodName = method.Key;
                var methodChanges = method.Value;
                if (!string.IsNullOrEmpty(methodName) &&
                    methodChanges != null &&
                    methodChanges.EditType != null)
                {
                    methodChanges.EditType = ProjectModifierHelper.FilterCodeBlocks(new CodeBlock[] { methodChanges.EditType }, options).FirstOrDefault();
                    //if after filtering, the method type might not need editing
                    if (methodChanges.EditType != null)
                    {
                        //get method node from ClassDeclarationSyntax
                        IDictionary<string, string> parameterValues = null;

                        var originalMethodNode = modifiedClassDeclarationSyntax?
                            .DescendantNodes()
                            .Where(
                                node => node is MethodDeclarationSyntax mds &&
                                mds.Identifier.ValueText.Equals(methodName) &&
                                (parameterValues = VerfiyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                            .FirstOrDefault();
                        
                        var methodNode = modifiedClassDeclarationSyntax?
                            .DescendantNodes()
                            .Where(
                                node => node is MethodDeclarationSyntax mds &&
                                mds.Identifier.ValueText.Equals(methodName) &&
                                (parameterValues = VerfiyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                            .FirstOrDefault();
                        
                        if (originalMethodNode != null && methodNode != null && methodNode is MethodDeclarationSyntax methodDeclarationSyntax && methodChanges.EditType != null)
                        {
                            var returnTypeString = methodDeclarationSyntax.ReturnType.ToFullString();
                            if (methodDeclarationSyntax.Modifiers.Any(m => m.ToFullString().Contains("async")))
                            {
                                returnTypeString = $"async {returnTypeString}";
                            }
                            if (!ProjectModifierHelper.TrimStatement(returnTypeString).Equals(ProjectModifierHelper.TrimStatement(methodChanges.EditType.Block)))
                            {
                                var typeIdentifier = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(methodChanges.EditType.Block));
                                methodDeclarationSyntax = methodDeclarationSyntax.WithReturnType(typeIdentifier.WithTrailingTrivia(SyntaxFactory.Whitespace(" ")));
                                modifiedClassDeclarationSyntax = modifiedClassDeclarationSyntax.ReplaceNode(originalMethodNode, methodDeclarationSyntax);
                            }
                        }
                    }
                }
            }
            return modifiedClassDeclarationSyntax;
        }

        private BaseMethodDeclarationSyntax AddParameters(BaseMethodDeclarationSyntax methodNode, CodeBlock[] addParameters, CodeChangeOptions toolOptions)
        {
            var newMethod = methodNode;
            List<ParameterSyntax> newParameters = new List<ParameterSyntax>();
            foreach (var parameter in addParameters)
            {
                var identifier = SyntaxFactory.Identifier(parameter.Block).WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
                var parameterSyntax = SyntaxFactory.Parameter(identifier);
                if (!newMethod.ParameterList.Parameters.Any(p => p.ToFullString().Equals(parameter.Block)))
                {
                    newParameters.Add(parameterSyntax);
                }
            }
            if (newParameters.Any())
            {
                newMethod = newMethod.AddParameterListParameters(newParameters.ToArray());
            }
           
            return newMethod;
        }

        //For inserting global statements in a minimal hosting C# file (.NET 6 Preview 7+)
        internal static CompilationUnitSyntax AddGlobalStatements(CodeSnippet change, CompilationUnitSyntax root)
        {
            var newRoot = root;
            var statementTrailingTrivia = new SyntaxTriviaList();
            var statementLeadingTrivia = new SyntaxTriviaList();

            if (change.CodeFormatting != null)
            {
                if (change.CodeFormatting.Newline)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
                }
                if (change.CodeFormatting.NumberOfSpaces > 0)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.Whitespace(new string(' ', change.CodeFormatting.NumberOfSpaces)));
                }
            }

            var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(change.Block)).WithLeadingTrivia(statementLeadingTrivia).WithTrailingTrivia(statementTrailingTrivia);
            if (!ProjectModifierHelper.GlobalStatementExists(newRoot, globalStatement, change.CheckBlock))
            {
                //insert after, before, or at the end of file.
                //insert global statement after particular statement
                if (!string.IsNullOrEmpty(change.InsertAfter) || change.InsertBefore != null)
                {
                    MemberDeclarationSyntax insertAfterStatement = null;
                    if (!string.IsNullOrEmpty(change.InsertAfter))
                    {
                        insertAfterStatement = newRoot.Members.Where(st => st.ToString().Contains(change.InsertAfter)).FirstOrDefault();
                    }
                    if (insertAfterStatement != null && insertAfterStatement is GlobalStatementSyntax insertAfterGlobalStatment)
                    {
                        newRoot = newRoot.InsertNodesAfter(insertAfterGlobalStatment, new List<SyntaxNode> { globalStatement });
                    }
                    else
                    {
                        //find a statement to insert before.
                        foreach (var insertBeforeText in change.InsertBefore)
                        {
                            var insertBeforeStatement = newRoot.Members.Where(st => st.ToString().Contains(insertBeforeText)).FirstOrDefault();
                            if (insertBeforeStatement != null && insertBeforeStatement is GlobalStatementSyntax insertBeforeGlobalStatment)
                            {
                                newRoot = newRoot.InsertNodesBefore(insertBeforeGlobalStatment, new List<SyntaxNode> { globalStatement });
                                //exit if we found a statement to insert before
                                break;
                            }
                        }
                    }
                }
                //insert global statement at the top of the file
                else if (change.Append)
                {
                    newRoot = newRoot.WithMembers(newRoot.Members.Insert(0, globalStatement));
                }
                else if (!string.IsNullOrEmpty(change.Parent))
                {
                    var parentNode = newRoot.DescendantNodes().Where(n => n is ExpressionStatementSyntax && n.ToString().Contains(change.Parent)).FirstOrDefault();
                    if (change.Options.Contains(CodeChangeType.MemberAccess))
                    {
                        if (parentNode is ExpressionStatementSyntax exprNode)
                        {
                            var modifiedExprNode = ProjectModifierHelper.AddSimpleMemberAccessExpression(exprNode, change.Block, leadingTrivia: statementLeadingTrivia, trailingTrivia: statementTrailingTrivia);
                            //modifiedExprNode = modifiedExprNode.WithTrailingTrivia(modifiedExprNode.GetTrailingTrivia().Add(SyntaxFactory));
                            if (modifiedExprNode != null)
                            {
                                newRoot = newRoot.ReplaceNode(exprNode, modifiedExprNode);
                            }
                        }
                    }
                }
                //insert global statement at the end of the file
                else
                {
                    newRoot = newRoot.WithMembers(newRoot.Members.Add(globalStatement));
                }
            }
            return newRoot;
        }

        //add code snippet to parent node
        internal BlockSyntax AddCodeSnippetOnParent(
            CodeSnippet change,
            BlockSyntax modifiedBlockSyntaxNode,
            IDictionary<string, string> parameterValues)
        {
            string parentBlock = ProjectModifierHelper.FormatCodeBlock(change.Parent, parameterValues).Trim(ProjectModifierHelper.CodeSnippetTrimChars);
            if (!string.IsNullOrEmpty(parentBlock))
            {
                //get the parent node to add CodeSnippet onto.
                var parentNode = modifiedBlockSyntaxNode.DescendantNodes().Where(n => n is ExpressionStatementSyntax && n.ToString().Contains(parentBlock)).FirstOrDefault();
                if (parentNode is ExpressionStatementSyntax exprNode)
                {
                    //add a SimpleMemberAccessExpression to parent node.
                    if (change.Options.Contains(CodeChangeType.MemberAccess))
                    {
                        var trailingTrivia = exprNode.GetTrailingTrivia();
                        if (trailingTrivia.Where(x => x.ToString().Trim(' ').Equals(";")).Any())
                        {
                            trailingTrivia = trailingTrivia.Insert(0, SemiColonTrivia);
                        }
                        var modifiedExprNode = ProjectModifierHelper.AddSimpleMemberAccessExpression(exprNode, change.Block, new SyntaxTriviaList(), new SyntaxTriviaList())?.WithTrailingTrivia(trailingTrivia);
                        if (modifiedExprNode != null)
                        {
                            modifiedExprNode = modifiedExprNode.WithTrailingTrivia(trailingTrivia);
                            modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.ReplaceNode(parentNode, modifiedExprNode);
                        }
                    }
                    //add within Lambda block of parent node.
                    else if (change.Options.Contains(CodeChangeType.InLambdaBlock))
                    {
                        var modifiedExprNode = AddExpressionInLambdaBlock(exprNode, change.InsertAfter, change.Block, parameterValues);
                        if (modifiedExprNode != null)
                        {
                            modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.ReplaceNode(parentNode, modifiedExprNode);
                        }
                    }
                }
            }
            return modifiedBlockSyntaxNode;
        }

        internal ExpressionStatementSyntax AddExpressionInLambdaBlock(
            ExpressionStatementSyntax expression,
            string insertAfterBlock,
            string codeSnippet,
            IDictionary<string, string> parameterValues)
        {
            BlockSyntax blockToEdit;
            if (!string.IsNullOrEmpty(codeSnippet))
            {
                if (!string.IsNullOrEmpty(insertAfterBlock))
                {
                    string insertAfterFormattedBlock = ProjectModifierHelper.FormatCodeBlock(insertAfterBlock, parameterValues);
                    blockToEdit = expression.DescendantNodes().FirstOrDefault(node =>
                                    node is BlockSyntax &&
                                    node.ToString().Trim(ProjectModifierHelper.CodeSnippetTrimChars).Contains(insertAfterBlock)) as BlockSyntax;
                }
                else
                {
                    blockToEdit = expression.DescendantNodes()
                        .Where(node => node is BlockSyntax)
                        .FirstOrDefault() as BlockSyntax;
                }

                if (blockToEdit != null)
                {
                    var sampleStatement = blockToEdit.Statements.FirstOrDefault();
                    var innerTrailingTrivia = sampleStatement?.GetTrailingTrivia() ?? new SyntaxTriviaList();
                    var innerLeadingTrivia = sampleStatement?.GetLeadingTrivia() ?? new SyntaxTriviaList();

                    if (!innerTrailingTrivia.Contains(SemiColonTrivia))
                    {
                        innerTrailingTrivia = innerTrailingTrivia.Insert(0, SemiColonTrivia);
                    }

                    StatementSyntax innerStatement = SyntaxFactory.ParseStatement(codeSnippet)
                        .WithAdditionalAnnotations(Formatter.Annotation)
                        .WithLeadingTrivia(innerLeadingTrivia)
                        .WithTrailingTrivia(innerTrailingTrivia);

                    if (!ProjectModifierHelper.StatementExists(blockToEdit, innerStatement))
                    {
                        var newBlock = blockToEdit.WithStatements(blockToEdit.Statements.Add(innerStatement));
                        return expression.ReplaceNode(blockToEdit, newBlock);
                    }
                }
            }
            return null;
        }

        internal BlockSyntax AddCodeSnippetAfterNode(
            CodeSnippet change,
            BlockSyntax modifiedBlockSyntaxNode,
            IDictionary<string, string> parameterValues)
        {
            string insertAfterBlock = ProjectModifierHelper.FormatCodeBlock(change.InsertAfter, parameterValues);
            
            if (!string.IsNullOrEmpty(insertAfterBlock) && !string.IsNullOrEmpty(change.Block))
            {
                var insertAfterNode =
                    modifiedBlockSyntaxNode.DescendantNodes().Where(node => node != null && node.ToString().Contains(insertAfterBlock)).FirstOrDefault() ??
                    modifiedBlockSyntaxNode.DescendantNodes().Where(node => node != null && node.ToString().Contains(ProjectModifierHelper.TrimStatement(insertAfterBlock))).FirstOrDefault();
                if (insertAfterNode != null)
                {
                    var leadingTrivia = insertAfterNode.GetLeadingTrivia();
                    var trailingTrivia = new SyntaxTriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
                    string formattedCodeBlock = ProjectModifierHelper.FormatCodeBlock(change.Block, parameterValues);

                    StatementSyntax statement = SyntaxFactory.ParseStatement(formattedCodeBlock)
                        .WithAdditionalAnnotations(Formatter.Annotation)
                        .WithTrailingTrivia(trailingTrivia)
                        .WithLeadingTrivia(leadingTrivia);
                    //check if statement already exists.
                    if (!ProjectModifierHelper.StatementExists(modifiedBlockSyntaxNode, statement))
                    {
                        modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.InsertNodesAfter(insertAfterNode, new List<StatementSyntax>() { statement });
                    }
                }
            }
            return modifiedBlockSyntaxNode;
        }

        internal BlockSyntax AddCodeSnippetInPlace(
            CodeSnippet change,
            BlockSyntax modifiedBlockSyntaxNode,
            IDictionary<string, string> parameterValues)
        {
            string formattedCodeBlock = ProjectModifierHelper.FormatCodeBlock(change.Block, parameterValues);

            //using defaults for leading and trailing trivia
            var trailingTrivia = new SyntaxTriviaList(SyntaxFactory.CarriageReturnLineFeed);
            var leadingTrivia = new SyntaxTriviaList();
            if (change.CodeFormatting != null)
            {
                if (change.CodeFormatting.NumberOfSpaces > 0)
                {
                    leadingTrivia = leadingTrivia.Add(SyntaxFactory.Whitespace(new string(' ', change.CodeFormatting.NumberOfSpaces)));
                }
                if (change.CodeFormatting.Newline)
                {
                    change.Block = "\n" + change.Block;
                }
            }
            //set leading and trailing trivia if block has any existing statements.
/*            if (modifiedBlockSyntaxNode.Statements.Any())
            {
                trailingTrivia = modifiedBlockSyntaxNode.Statements[0].GetTrailingTrivia();
                leadingTrivia = modifiedBlockSyntaxNode.Statements[0].GetLeadingTrivia();
            }*/
            StatementSyntax statement = SyntaxFactory.ParseStatement(formattedCodeBlock)
                                            .WithAdditionalAnnotations(Formatter.Annotation)
                                            .WithTrailingTrivia(trailingTrivia)
                                            .WithLeadingTrivia(leadingTrivia);
            //check if statement already exists.
            if (!ProjectModifierHelper.StatementExists(modifiedBlockSyntaxNode, statement))
            {
                if (change.Options != null && change.Options.Any() &&
                   change.Options.Contains(CodeChangeType.LambdaExpression) &&
                    !string.IsNullOrEmpty(change.Parameter))
                {
                    var arg = SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier(change.Parameter)))
                                .WithBlock(SyntaxFactory.Block()));
                    if (arg != null)
                    {
                        var argList = SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().Add(arg));
                        var parsedExpression = SyntaxFactory.ParseExpression(formattedCodeBlock);

                        var expression = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(parsedExpression, argList))
                                            .WithAdditionalAnnotations(Formatter.Annotation)
                                            .WithTrailingTrivia(trailingTrivia)
                                            .WithLeadingTrivia(leadingTrivia);
                        modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.WithStatements(modifiedBlockSyntaxNode.Statements.Add(expression));
                    }
                }
                else if (change.Append)
                {
                    modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.WithStatements(new SyntaxList<StatementSyntax>(modifiedBlockSyntaxNode.Statements.Insert(0, statement)));
                }
                else
                {
                    modifiedBlockSyntaxNode = modifiedBlockSyntaxNode.WithStatements(modifiedBlockSyntaxNode.Statements.Add(statement));
                }
            }
            return modifiedBlockSyntaxNode;
        }

        //create UsingDirectiveSyntax[] using a string[] to add to the root of the class (root.Usings).
        internal UsingDirectiveSyntax[] CreateUsings(string[] usings)
        {
            var usingDirectiveList = new List<UsingDirectiveSyntax>();
            if (usings != null && usings.Any())
            {
                foreach (var usingDirectiveString in usings)
                {
                    if (!string.IsNullOrEmpty(usingDirectiveString))
                    {
                        //leading space on the value of the using eg. (using' 'Microsoft.Yadada)
                        var nameLeadingTrivia = new SyntaxTriviaList(SyntaxFactory.Space);
                        var additionalAnnotation = Formatter.Annotation;
                        var usingTrailingTrivia = new SyntaxTriviaList(SyntaxFactory.CarriageReturnLineFeed);
                        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingDirectiveString).WithLeadingTrivia(nameLeadingTrivia))
                            .WithAdditionalAnnotations(additionalAnnotation)
                            .WithTrailingTrivia(usingTrailingTrivia);
                        usingDirectiveList.Add(usingDirective);
                    }

                }
            }
            return usingDirectiveList.ToArray();
        }

        //create AttributeListSyntax using string[] to add on top of a ClassDeclrationSyntax
        internal SyntaxList<AttributeListSyntax> CreateAttributeList(CodeBlock[] attributes, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTriviaList leadingTrivia)
        {
            var syntaxList = attributeLists;
            
            if (attributes != null && attributes.Any())
            {
                foreach (var attribute in attributes)
                {
                    var attributeList = new List<AttributeSyntax>();
                    //filter by apps
                    if (!string.IsNullOrEmpty(attribute.Block) && !ProjectModifierHelper.AttributeExists(attribute.Block, attributeLists))
                    {
                        attributeList.Add(SyntaxFactory.Attribute(SyntaxFactory.ParseName(attribute.Block)));
                    }
                    if (attributeList.Any())
                    {
                        var attributeListSyntax = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)).WithLeadingTrivia(leadingTrivia);
                        if (!leadingTrivia.ToString().Contains("\n"))
                        {
                            attributeListSyntax = attributeListSyntax.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);                        
                        }
                        syntaxList = syntaxList.Insert(0, attributeListSyntax);
                    }  
                }
            }
            return syntaxList;   
        }

        //check if the parameters match for the given method, and populate a Dictionary with parameter.Type keys and Parameter.Identifier values.
        internal IDictionary<string, string> VerfiyParameters(string[] parametersToCheck, List<ParameterSyntax> foundParameters)
        {
            IDictionary<string, string> parametersWithNames = new Dictionary<string, string>();
            if (foundParameters.Any() && parametersToCheck != null && parametersToCheck.Any())
            {
                var pars = foundParameters.ToList();
                foreach (var parameter in parametersToCheck)
                {
                    //Trim(' ') for the additional whitespace at the end of the parameter.Type string. Parameter.Type should be a singular word.
                    var verifiedParams = pars.Where(p => (p.Type?.ToFullString()?.Trim(' ')?.Equals(parameter)).GetValueOrDefault(false));
                    if (verifiedParams.Any())
                    {
                        parametersWithNames.Add(parameter, verifiedParams.First().Identifier.ValueText);
                    }
                }

                //Dictionary should have the same number of parameters we are trying to verify.
                if (parametersWithNames.Count == parametersToCheck.Length)
                {
                    return parametersWithNames;
                }
                else
                {
                    return null;
                }
            }
            return parametersWithNames;
        }

        //here for mostly testing
        internal Document GetDocument()
        {
            return _documentEditor.GetChangedDocument();
        }

        internal CompilationUnitSyntax GetChangedRoot()
        {
            return _documentEditor.GetChangedRoot() as CompilationUnitSyntax;
        }

        //create MemberDeclarationSyntax[] to add at the top of ClassDeclarationSynax. Created from the property strings.
        internal MemberDeclarationSyntax[] CreateClassProperties(
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxTriviaList leadingTrivia,
            SyntaxTriviaList trailingTrivia)
        {
            var propertyDeclarationList = new List<MemberDeclarationSyntax>();
            if (_codeFile.ClassProperties != null && _codeFile.ClassProperties.Any())
            {
                foreach (var classProperty in _codeFile.ClassProperties)
                {
                    if (!string.IsNullOrEmpty(classProperty.Block) && !PropertyExists(classProperty.Block, members))
                    {
                        var additionalAnnotation = Formatter.Annotation;
                        var classPropertyDeclaration = SyntaxFactory.ParseMemberDeclaration(classProperty.Block)
                            ?.WithAdditionalAnnotations(additionalAnnotation)
                            ?.WithTrailingTrivia(trailingTrivia)
                            ?.WithLeadingTrivia(leadingTrivia);

                        if (classPropertyDeclaration != null)
                        {
                            propertyDeclarationList.Add(classPropertyDeclaration);
                        }
                    }
                }
            }
            return propertyDeclarationList.ToArray();
        }

        internal bool PropertyExists(string property, SyntaxList<MemberDeclarationSyntax> members)
        {
            if (!string.IsNullOrEmpty(property))
            {
                if (members.Where(m => m.ToString().Trim(ProjectModifierHelper.CodeSnippetTrimChars).Equals(property.Trim(ProjectModifierHelper.CodeSnippetTrimChars))).Any())
                {
                    return true;
                }
            }
            return false;
        }

        private static SyntaxTrivia SemiColonTrivia
        {
            get
            {
                return SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia()
                    .WithTokens(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
            }
        }
    }
}
