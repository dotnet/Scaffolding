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
        private readonly DocumentEditor _documentEditor;
        private readonly CodeFile _codeFile;
        private readonly CompilationUnitSyntax _docRoot;
        private readonly IConsoleLogger _consoleLogger;

        public DocumentBuilder(
            DocumentEditor documentEditor,
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
                            newRoot = newRoot.InsertNodesAfter(newRoot.Usings.Last(), new List<SyntaxNode> { usingNode });
                            // TODO add new line if necessary after usings
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

        internal SyntaxNode AddMethodParameters(string methodName, Method methodChanges, SyntaxNode modifiedClassDeclarationSyntax, CodeChangeOptions options)
        {
            if (string.IsNullOrEmpty(methodName) ||
                methodChanges is null || methodChanges.AddParameters is null ||
                !methodChanges.AddParameters.Any())
            {
                return modifiedClassDeclarationSyntax;
            }
            //get method node from ClassDeclarationSyntax
            IDictionary<string, string> parameterValues = null;

            var originalMethodNode = modifiedClassDeclarationSyntax?
                .DescendantNodes()
                .Where(
                    node => node is MethodDeclarationSyntax mds &&
                    mds.Identifier.ValueText.Equals(methodName) &&
                    (parameterValues = VerifyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                .FirstOrDefault();

            var methodNode = modifiedClassDeclarationSyntax?
                .DescendantNodes()
                .Where(
                    node => node is MethodDeclarationSyntax mds &&
                    mds.Identifier.ValueText.Equals(methodName) &&
                    (parameterValues = VerifyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
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
            return modifiedClassDeclarationSyntax;
        }

        //Add all the different code snippet.
        internal SyntaxNode AddCodeSnippetsToMethod(string methodName, Method methodChanges, SyntaxNode originalMethod, CodeChangeOptions options)
        {
            var filteredChanges = ProjectModifierHelper.FilterCodeSnippets(originalMethod, methodChanges.CodeChanges, options);
             
            if (!filteredChanges.Any())
            {
                return originalMethod;
            }

            // get method node from ClassDeclarationSyntax

            //check for constructor as its not a MethodDeclarationSyntax but ConstructorDeclarationSyntax
            if (originalMethod?.DescendantNodes().Where(node
                => node is MethodDeclarationSyntax mds && mds.Identifier.ValueText.Equals(methodName)
                || node is ConstructorDeclarationSyntax cds && cds.Identifier.ValueText.Equals(methodName))
                .FirstOrDefault() is BaseMethodDeclarationSyntax foundMethod)
            {
                var parameters = VerifyParameters(methodChanges.Parameters, foundMethod.ParameterList.Parameters.ToList());
                if (parameters == null)
                {
                    return null;
                }

                var blockSyntax = foundMethod.Body;

                var modifiedMethod = ModifyMethod(blockSyntax, filteredChanges, parameters);

                originalMethod = originalMethod.ReplaceNode(originalMethod, modifiedMethod);

            }

            return originalMethod;
        }

        internal static SyntaxNode ModifyMethod(SyntaxNode root, CodeSnippet[] filteredChanges, IDictionary<string, string> parameterValues = null)
        {
            var syntaxKind = root.Kind();
            foreach (var change in filteredChanges)
            {
                var update = ModifyMethod(root, change, syntaxKind, change.CodeChangeType, parameterValues);
                if (update != null)
                {
                    root = root.ReplaceNode(root, update);
                }
            }

            return root;
        }

        internal static SyntaxNode ModifyMethod(SyntaxNode originalMethod, CodeSnippet codeChange, SyntaxKind syntaxKind, CodeChangeType codeChangeType = CodeChangeType.Default, IDictionary<string, string> parameterValues = null)
        {
            var modifiedMethod = originalMethod;

            if (codeChangeType != CodeChangeType.Default)
            {
                var updatedNode = GetNodeWithModifiedChild(modifiedMethod, codeChange, syntaxKind, parameterValues);
                return updatedNode != null ? modifiedMethod.ReplaceNode(modifiedMethod, updatedNode) : originalMethod;
            }

            if (!string.IsNullOrEmpty(codeChange.InsertAfter))
            {
                var precedingNode = GetPrecedingNode(codeChange.InsertAfter, modifiedMethod, parameterValues);
                if (precedingNode != null)
                {
                    var leadingTrivia = GetLeadingTrivia(codeChange);
                    var trailingTrivia = new SyntaxTriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
                    var nodeToInsert = GetNode(syntaxKind, codeChange.Block, leadingTrivia, trailingTrivia);
                    var updatedNode = modifiedMethod.InsertNodesAfter(precedingNode, new List<SyntaxNode> { nodeToInsert });

                    return updatedNode != null ? modifiedMethod.ReplaceNode(modifiedMethod, updatedNode) : originalMethod;
                }
            }

            return AddCodeSnippet(syntaxKind, modifiedMethod, codeChange);
        }

        private static SyntaxNode GetNode(SyntaxKind syntaxKind, string codeChange, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        {
            if (syntaxKind == SyntaxKind.CompilationUnit)
            {
                return GetMember(codeChange, leadingTrivia, trailingTrivia);
                // TODO: getGlobalStatement?
            }
            if (syntaxKind == SyntaxKind.Block)
            {
                return GetStatement(codeChange, leadingTrivia, trailingTrivia);
            }

            return null;
        }

        private static SyntaxNode AddCodeSnippet(SyntaxKind syntaxKind, SyntaxNode node, CodeSnippet codeChange)
        {
            if (syntaxKind == SyntaxKind.CompilationUnit && node is CompilationUnitSyntax compilationUnit)
            {
                var member = GetGlobalStatement(codeChange); 

                return codeChange.Prepend
                    ? compilationUnit.WithMembers(compilationUnit.Members.Insert(0, member))
                    : compilationUnit.AddMembers(member);
            }
            if (syntaxKind == SyntaxKind.Block && node is BlockSyntax block)
            {
                var statement = GetStatement(codeChange.Block, node.GetLeadingTrivia(), node.GetTrailingTrivia());

                return codeChange.Prepend
                  ? block.WithStatements(block.Statements.Insert(0, statement))
                  : block.AddStatements(statement);
            }

            return node;
        }

        private static StatementSyntax GetStatement(string formattedBlock, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        {
            return 
                SyntaxFactory.ParseStatement(formattedBlock)
                .WithAdditionalAnnotations(Formatter.Annotation)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
        }

        private static GlobalStatementSyntax GetGlobalStatement(CodeSnippet change)
        {
            var statementLeadingTrivia = GetLeadingTrivia(change);
            var statementTrailingTrivia = new SyntaxTriviaList();

            var globalStatement = SyntaxFactory.GlobalStatement(GetStatement(change.Block, statementLeadingTrivia, statementTrailingTrivia));
            return globalStatement;
        }

        private static SyntaxTriviaList GetLeadingTrivia(CodeSnippet change)
        {
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

            return statementLeadingTrivia;
        }

        private static MemberDeclarationSyntax GetMember(string formattedBlock, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        {
            return
                SyntaxFactory.GlobalStatement(GetStatement(formattedBlock, leadingTrivia, trailingTrivia));
                //SyntaxFactory.ParseMemberDeclaration(formattedBlock)
                //.WithAdditionalAnnotations(Formatter.Annotation)
                //.WithLeadingTrivia(leadingTrivia)
                //.WithTrailingTrivia(trailingTrivia);
        }

        private static SyntaxNode GetNodeWithModifiedChild(SyntaxNode originalMethod, CodeSnippet change, SyntaxKind syntaxKind, IDictionary<string, string> parameterValues = null)
        {
            string parentBlock = ProjectModifierHelper.FormatCodeBlock(change.Parent, parameterValues).Trim(ProjectModifierHelper.CodeSnippetTrimChars);
            if (string.IsNullOrEmpty(parentBlock))
            {
                return null;
            }

            var parent = GetParent(originalMethod, parentBlock, syntaxKind);
            
            SyntaxNode updatedParent = null;
            if (change.CodeChangeType == CodeChangeType.LambdaParameter)
            {
                updatedParent = UpdateLambdaParameter(change.Block, parent);
            }
            else if (change.CodeChangeType == CodeChangeType.InLambdaBlock)
            {
                updatedParent = ModifyMethod(parent, change, syntaxKind, CodeChangeType.Default, parameterValues);
            }
            else if (change.CodeChangeType == CodeChangeType.MemberAccess)
            {
                updatedParent = AddCodeSnippetOnParent(change, parent);
            }

            return updatedParent != null ? originalMethod.ReplaceNode(parent, updatedParent) : originalMethod;
            // if there is no CodeChange.Parent, check if to InsertAfter a statement.
        }

        private static SyntaxNode GetParent(SyntaxNode originalMethod, string parentBlock, SyntaxKind syntaxKind)
        {
            if (syntaxKind == SyntaxKind.CompilationUnit)
            {
                return (originalMethod as CompilationUnitSyntax).Members.FirstOrDefault(n => n.ToString().Contains(parentBlock));
            }
            if (syntaxKind == SyntaxKind.Block)
            {
                return (originalMethod as BlockSyntax).Statements.FirstOrDefault(n => n.ToString().Contains(parentBlock));
            }

            return null;
        }

        private static SyntaxNode GetPrecedingNode(string insertAfter, SyntaxNode syntaxNode, IDictionary<string, string> parameterValues = null)
        {
            string insertAfterStatement = ProjectModifierHelper.FormatCodeBlock(insertAfter, parameterValues);

            if (string.IsNullOrEmpty(insertAfterStatement))
            {
                return null;
            }

            var insertAfterNode =
                syntaxNode.DescendantNodes().Where(node => node != null && node.ToString().Contains(insertAfterStatement)).FirstOrDefault() ??
                syntaxNode.DescendantNodes().Where(node => node != null && node.ToString().Contains(ProjectModifierHelper.TrimStatement(insertAfterStatement))).FirstOrDefault();

            return insertAfterNode;
        }

        internal SyntaxNode EditMethodType(string methodName, Method methodChanges, SyntaxNode modifiedClassDeclarationSyntax, CodeChangeOptions options)
        {
            if (string.IsNullOrEmpty(methodName) || methodChanges is null || methodChanges.EditType is null)
            {
                return modifiedClassDeclarationSyntax;
            }

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
                        (parameterValues = VerifyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
                    .FirstOrDefault();

                var methodNode = modifiedClassDeclarationSyntax?
                    .DescendantNodes()
                    .Where(
                        node => node is MethodDeclarationSyntax mds &&
                        mds.Identifier.ValueText.Equals(methodName) &&
                        (parameterValues = VerifyParameters(methodChanges.Parameters, mds.ParameterList.Parameters.ToList())) != null)
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

        ////For inserting global statements in a minimal hosting C# file (.NET 6 Preview 7+)
        //internal static SyntaxNode AddGlobalStatements(CodeSnippet change, CompilationUnitSyntax root)
        //{
        //    var newRoot = root;
        //    var statementTrailingTrivia = new SyntaxTriviaList();
        //    var statementLeadingTrivia = new SyntaxTriviaList();

        //    if (change.CodeFormatting != null)
        //    {
        //        if (change.CodeFormatting.Newline)
        //        {
        //            statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        //        }
        //        if (change.CodeFormatting.NumberOfSpaces > 0)
        //        {
        //            statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.Whitespace(new string(' ', change.CodeFormatting.NumberOfSpaces)));
        //        }
        //    }

        //    var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(change.Block)).WithLeadingTrivia(statementLeadingTrivia).WithTrailingTrivia(statementTrailingTrivia);
        //    if (ProjectModifierHelper.GlobalStatementExists(newRoot, globalStatement, change.CheckBlock))
        //    {
        //        return newRoot;
        //    }

        //    GetMethodModification(change, root);
        //    return ModifyMethod(change, root, globalStatement);

        //    // //insert after, before, or at the end of file.
        //    // //insert global statement after particular statement
        //    // if (!string.IsNullOrEmpty(change.InsertAfter) || change.InsertBefore != null)
        //    // {
        //    //     MemberDeclarationSyntax insertAfterStatement = null;
        //    //     if (!string.IsNullOrEmpty(change.InsertAfter))
        //    //     {
        //    //         insertAfterStatement = newRoot.Members.Where(st => st.ToString().Contains(change.InsertAfter)).FirstOrDefault();
        //    //     }
        //    //     if (insertAfterStatement != null && insertAfterStatement is GlobalStatementSyntax insertAfterGlobalStatment)
        //    //     {
        //    //         newRoot = newRoot.InsertNodesAfter(insertAfterGlobalStatment, new List<SyntaxNode> { globalStatement });
        //    //     }
        //    //     else
        //    //     {
        //    //         //find a statement to insert before.
        //    //         foreach (var insertBeforeText in change.InsertBefore)
        //    //         {
        //    //             var insertBeforeStatement = newRoot.Members.Where(st => st.ToString().Contains(insertBeforeText)).FirstOrDefault();
        //    //             if (insertBeforeStatement != null && insertBeforeStatement is GlobalStatementSyntax insertBeforeGlobalStatment)
        //    //             {
        //    //                 newRoot = newRoot.InsertNodesBefore(insertBeforeGlobalStatment, new List<SyntaxNode> { globalStatement });
        //    //                 //exit if we found a statement to insert before
        //    //                 break;
        //    //             }
        //    //         }
        //    //     }
        //    // }
        //    // else if (!string.IsNullOrEmpty(change.Parent))
        //    // {
        //    //     return AddCodeSnippetOnParent(change, root);
        //    //     //newRoot = ModifyGlobalParent(newRoot, change, statementLeadingTrivia, statementTrailingTrivia);
        //    // }
        //    // //insert global statement at the beginning or end of the file
        //    // else
        //    // {
        //    //     var updatedMembers = change.Prepend ? newRoot.Members.Insert(0, globalStatement) : newRoot.Members.Add(globalStatement);
        //    //     newRoot = newRoot.WithMembers(updatedMembers);
        //    // }

        //    // return newRoot;
        //}

        //add code snippet to parent node
        internal static SyntaxNode AddCodeSnippetOnParent(
            CodeSnippet change,
            SyntaxNode parent)
        {
            var exprNode = parent.DescendantNodes().FirstOrDefault(n => n.Kind() == SyntaxKind.ExpressionStatement) as ExpressionStatementSyntax;
            var leadingTrivia = GetLeadingTrivia(change);
            var identifier = SyntaxFactory.IdentifierName(change.Block);
            var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                exprNode.Expression.WithTrailingTrivia(leadingTrivia),
                identifier);

            var modifiedExprNode = exprNode.WithExpression(newExpression);

            if (modifiedExprNode != null)
            {
                return parent.ReplaceNode(exprNode, modifiedExprNode);
            }

            return null;
        }

        //private static SyntaxToken GetFormattedStatement(CodeSnippet change, SyntaxTriviaList trailingTrivia, SyntaxTriviaList leadingTrivia, IDictionary<string, string> parameterValues)
        //{

        //    //using defaults for leading and trailing trivia
        //    if (trailingTrivia == null)
        //        {
        //            trailingTrivia = new SyntaxTriviaList(SyntaxFactory.CarriageReturnLineFeed);
        //        }
        //    if (leadingTrivia == null)
        //    {
        //        leadingTrivia = new SyntaxTriviaList();
        //    }

        //    if (change.CodeFormatting != null)
        //    {
        //        if (change.CodeFormatting.NumberOfSpaces > 0)
        //        {
        //            leadingTrivia = leadingTrivia.Add(SyntaxFactory.Whitespace(new string(' ', change.CodeFormatting.NumberOfSpaces)));
        //        }
        //        if (change.CodeFormatting.Newline)
        //        {
        //            change.Block = "\n" + change.Block;
        //        }
        //    }
        //    //set leading and trailing trivia if block has any existing statements.
        //    var formattedCodeBlock = SyntaxFactory.ParseToken(formattedCodeBlock)
        //                                    .WithAdditionalAnnotations(Formatter.Annotation)
        //                                    .WithTrailingTrivia(trailingTrivia)
        //                                    .WithLeadingTrivia(leadingTrivia);
        //}

        private static SyntaxNode UpdateLambdaParameter(string parameter, SyntaxNode parent, SyntaxTriviaList? leadingTrivia = null, SyntaxTriviaList? trailingTrivia = null)
        {
            var existingLambda = parent.DescendantNodes().FirstOrDefault(n => n is ParenthesizedLambdaExpressionSyntax) as ParenthesizedLambdaExpressionSyntax;
            // todo also SimpleLambdaExpressionSyntax

            var lambdaParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter));
            if (lambdaParam is null)
            {
                return null;
            }

            var updatedLambda = existingLambda.AddParameterListParameters(lambdaParam);
            var updatedParent = parent.ReplaceNode(existingLambda, updatedLambda);

            return updatedParent;
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
        internal IDictionary<string, string> VerifyParameters(string[] parametersToCheck, List<ParameterSyntax> foundParameters)
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

        private static SyntaxTrivia SemiColonTrivia => SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia()
                    .WithTokens(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
    }
}
