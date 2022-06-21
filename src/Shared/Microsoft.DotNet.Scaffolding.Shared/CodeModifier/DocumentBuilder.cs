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

        internal static BaseMethodDeclarationSyntax GetModifiedMethod(BaseMethodDeclarationSyntax method, Method methodChanges, CodeChangeOptions options)
        {
            method = AddCodeSnippetsToMethod(method, methodChanges, options);
            method = EditMethodReturnType(method, methodChanges, options);
            method = AddMethodParameters(method, methodChanges, options);
            return method;
        }

        public CompilationUnitSyntax AddUsings(CodeChangeOptions options)
        {
            // adding usings
            if (_codeFile.UsingsWithOptions != null && _codeFile.UsingsWithOptions.Any())
            {
                var usingsWithOptions = FilterUsingsWithOptions(_codeFile, options);
                _codeFile.Usings = _codeFile.Usings?.Concat(usingsWithOptions).ToArray() ?? usingsWithOptions.ToArray();
            }

            var usingNodes = CreateUsings(_codeFile.Usings);
            if (usingNodes.Any() && _docRoot.Usings.Count == 0)
            {
                return _docRoot.WithUsings(SyntaxFactory.List(usingNodes));
            }
            else
            {
                var uniqueUsings = GetUniqueUsings(_docRoot.Usings.ToArray(), usingNodes);

                return uniqueUsings.Any() ? _docRoot.WithUsings(_docRoot.Usings.AddRange(uniqueUsings)) : _docRoot;
            }
        }

        internal static SyntaxList<UsingDirectiveSyntax> GetUniqueUsings(UsingDirectiveSyntax[] existingUsings, UsingDirectiveSyntax[] newUsings)
        {
            return SyntaxFactory.List(
                newUsings.Where(u => !existingUsings.Any(oldUsing => oldUsing.Name.ToString().Equals(u.Name.ToString()))));
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
                var memberLeadingTrivia = sampleMember?.GetLeadingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("    "));
                var memberTrailingTrivia = SyntaxFactory.TriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);

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

        internal static BaseMethodDeclarationSyntax AddMethodParameters(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
        {
            if (methodChanges is null || methodChanges.AddParameters is null || !methodChanges.AddParameters.Any())
            {
                return originalMethod;
            }

            // Filter for CodeChangeOptions
            methodChanges.AddParameters = ProjectModifierHelper.FilterCodeBlocks(methodChanges.AddParameters, options);

            return AddParameters(originalMethod, methodChanges.AddParameters, options);
        }

        // Add all the different code snippet.
        internal static BaseMethodDeclarationSyntax AddCodeSnippetsToMethod(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
        {
            var filteredChanges = ProjectModifierHelper.FilterCodeSnippets(methodChanges.CodeChanges, options);

            if (!filteredChanges.Any())
            {
                return originalMethod;
            }

            var blockSyntax = originalMethod.Body;

            var modifiedMethod = ApplyChangesToMethod(blockSyntax, filteredChanges);

            return originalMethod.ReplaceNode(blockSyntax, modifiedMethod);
        }

        internal static SyntaxNode ApplyChangesToMethod(SyntaxNode root, CodeSnippet[] filteredChanges)
        {
            foreach (var change in filteredChanges)
            {
                var update = ModifyMethod(root, change);
                if (update != null)
                {
                    root = root.ReplaceNode(root, update);
                }
            }

            return root;
        }

        internal static MethodDeclarationSyntax GetMethodFromSyntaxRoot(CompilationUnitSyntax root, string methodIdentifier)
        {
            var namespaceNode = root.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
            var classNode = namespaceNode?.Members.OfType<ClassDeclarationSyntax>()?.FirstOrDefault() ??
                            root?.Members.OfType<ClassDeclarationSyntax>()?.FirstOrDefault();  
            if (classNode?.ChildNodes().FirstOrDefault(
                n => n is MethodDeclarationSyntax syntax &&
                syntax.Identifier.ToString().Equals(methodIdentifier, StringComparison.OrdinalIgnoreCase)) is MethodDeclarationSyntax method)
            {
                return method;
            }
            return null;
        }

        internal static CodeSnippet AddLeadingTriviaSpaces(CodeSnippet snippet, int spaces)
        {
            snippet.LeadingTrivia = snippet.LeadingTrivia ?? new Formatting();
            snippet.LeadingTrivia.NumberOfSpaces += spaces;
            return snippet;
        }

        internal static CodeSnippet[] AddLeadingTriviaSpaces(CodeSnippet[] snippets, int spaces)
        {
            for(int i = 0; i < snippets.Length; i++)
            {
                var snippet = snippets[i];
                snippet = AddLeadingTriviaSpaces(snippet, spaces);
                snippets[i] = snippet;
            }

            return snippets;
        }

        internal static SyntaxNode ModifyMethod(SyntaxNode originalMethod, CodeSnippet codeChange)
        {
            SyntaxNode modifiedMethod;
            switch (codeChange.CodeChangeType)
            {
                case CodeChangeType.Lambda:
                    {
                        modifiedMethod = AddOrUpdateLambda(originalMethod, codeChange);
                        break;
                    }
                case CodeChangeType.MemberAccess:
                    {
                        modifiedMethod = AddExpressionToParent(originalMethod, codeChange);
                        break;
                    }
                default:
                    {
                        modifiedMethod = UpdateMethod(originalMethod, codeChange);
                        break;
                    }
            }

            return modifiedMethod != null ? originalMethod.ReplaceNode(originalMethod, modifiedMethod) : originalMethod;
        }

        internal static SyntaxNode UpdateMethod(SyntaxNode originalMethod, CodeSnippet codeChange)
        {
            var children = GetDescendantNodes(originalMethod);
            if (ProjectModifierHelper.StatementExists(children, codeChange.Block) || (!string.IsNullOrEmpty(codeChange.CheckBlock) && ProjectModifierHelper.StatementExists(children, codeChange.CheckBlock)))
            {
                return originalMethod;
            }

            SyntaxNode updatedMethod;
            if (codeChange.InsertBefore?.Any() is true)
            {
                updatedMethod = InsertBefore(codeChange, children, originalMethod);
            }
            else if (!string.IsNullOrEmpty(codeChange.InsertAfter))
            {
                updatedMethod = InsertAfter(codeChange, children, originalMethod);
            }
            else
            {
                updatedMethod = GetBlockStatement(originalMethod, codeChange);
            }


            return updatedMethod ?? originalMethod;
        }

        private static SyntaxNode InsertBefore(CodeSnippet codeChange, IEnumerable<SyntaxNode> children, SyntaxNode originalMethod)
        {
            var followingNode = GetFollowingNode(codeChange.InsertBefore, children);
            if (followingNode is null)
            {
                return originalMethod;
            }

            var leadingWhitespaceTrivia = followingNode.GetLeadingTrivia().LastOrDefault();
            if (leadingWhitespaceTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                codeChange.LeadingTrivia.NumberOfSpaces += leadingWhitespaceTrivia.Span.Length;
            }

            var newNodes = GetNodeInsertionList(codeChange, originalMethod.Kind());
            if (newNodes is null)
            {
                return originalMethod;
            }

            return originalMethod.InsertNodesBefore(followingNode, newNodes);
        }


        private static SyntaxNode InsertAfter(CodeSnippet codeChange, IEnumerable<SyntaxNode> children, SyntaxNode originalMethod)
        {
            var precedingNode = GetSpecifiedNode(codeChange.InsertAfter, children);
            if (precedingNode is null)
            {
                return originalMethod;
            }

            var leadingWhitespaceTrivia = precedingNode.GetLeadingTrivia().LastOrDefault();
            if (leadingWhitespaceTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                codeChange.LeadingTrivia.NumberOfSpaces += leadingWhitespaceTrivia.Span.Length;
            }

            var newNodes = GetNodeInsertionList(codeChange, originalMethod.Kind());
            if (newNodes is null)
            {
                return originalMethod;
            }

            return originalMethod.InsertNodesAfter(precedingNode, newNodes);
        }

        private static SyntaxNode GetFollowingNode(string[] insertBefore, IEnumerable<SyntaxNode> descendantNodes)
        {
            foreach (var specifier in insertBefore)
            {
                if (GetSpecifiedNode(specifier, descendantNodes) is SyntaxNode insertBeforeNode)
                {
                    return insertBeforeNode;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a List of SyntaxNodes with the code change added, useful as an input for methods such as SyntaxNode.InsertNodesBefore that require a list
        /// Additionally, with this method we can use the same code for StatementSyntax and GlobalStatementSyntax nodes
        /// </summary>
        /// <param name="codeChange"></param>
        /// <param name="syntaxKind"></param>
        /// <returns></returns>
        private static List<SyntaxNode> GetNodeInsertionList(CodeSnippet codeChange, SyntaxKind syntaxKind)
        {
            var statement = GetStatementWithTrivia(codeChange);
            if (statement is null)
            {
                return null;
            }

            return syntaxKind == SyntaxKind.CompilationUnit
               ? new List<SyntaxNode> { SyntaxFactory.GlobalStatement(statement) }
               : new List<SyntaxNode> { statement };
        }

        private static SyntaxNode GetBlockStatement(SyntaxNode node, CodeSnippet codeChange)
        {
            var syntaxKind = node.Kind();
            var statement = GetStatementWithTrivia(codeChange);
            if (syntaxKind == SyntaxKind.Block && node is BlockSyntax block)
            {
                block = codeChange.Prepend
                    ? block.WithStatements(block.Statements.Insert(0, statement))
                    : block.AddStatements(statement);

                return block;
            }
            if (syntaxKind == SyntaxKind.CompilationUnit && node is CompilationUnitSyntax compilationUnit)
            {
                var globalStatement = SyntaxFactory.GlobalStatement(statement);
                return codeChange.Prepend
                    ? compilationUnit.WithMembers(compilationUnit.Members.Insert(0, globalStatement))
                    : compilationUnit.AddMembers(globalStatement);
            }

            return node;
        }

        private static StatementSyntax GetStatementWithTrivia(CodeSnippet change)
        {
            var leadingTrivia = GetLeadingTrivia(change.LeadingTrivia);
            var trailingTrivia = GetTrailingTrivia(change.TrailingTrivia);

            return SyntaxFactory.ParseStatement(change.Block)
                .WithAdditionalAnnotations(Formatter.Annotation)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
        }

        private static SyntaxTriviaList GetLeadingTrivia(Formatting codeFormatting)
        {
            var statementLeadingTrivia = SyntaxFactory.TriviaList();
            if (codeFormatting != null)
            {
                if (codeFormatting.Newline)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
                }
                if (codeFormatting.NumberOfSpaces > 0)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.Whitespace(new string(' ', codeFormatting.NumberOfSpaces)));
                }
            }

            return statementLeadingTrivia;
        }

        private static SyntaxTriviaList GetTrailingTrivia(Formatting codeFormatting)
        {
            var statementLeadingTrivia = SyntaxFactory.TriviaList();
            if (codeFormatting != null)
            {
                if (codeFormatting.Semicolon)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SemiColonTrivia);
                }
                if (codeFormatting.Newline)
                {
                    statementLeadingTrivia = statementLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
                }
            }

            return statementLeadingTrivia;
        }

        /// <summary>
        /// Searches through list of nodes and returns the first node that contains the specifierStatement
        /// Note: can be null
        /// </summary>
        /// <param name="specifierStatement"></param>
        /// <param name="descendantNodes"></param>
        /// <returns></returns>
        internal static SyntaxNode GetSpecifiedNode(string specifierStatement, IEnumerable<SyntaxNode> descendantNodes)
        {
            if (string.IsNullOrEmpty(specifierStatement))
            {
                return null;
            }

            var specifiedDescendant =
                descendantNodes.FirstOrDefault(d => d != null && d.ToString().Contains(specifierStatement)) ??
                descendantNodes.FirstOrDefault(d => d != null && d.ToString().Contains(ProjectModifierHelper.TrimStatement(specifierStatement)));

            return specifiedDescendant;
        }

        internal static BaseMethodDeclarationSyntax EditMethodReturnType(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
        {
            if (methodChanges is null || methodChanges.EditType is null || !(originalMethod is MethodDeclarationSyntax modifiedMethod))
            {
                return originalMethod;
            }

            methodChanges.EditType = ProjectModifierHelper.FilterCodeBlocks(new CodeBlock[] { methodChanges.EditType }, options).FirstOrDefault();

            // After filtering, the method type might not need editing
            if (methodChanges.EditType is null)
            {
                return originalMethod;
            }

            var returnTypeString = modifiedMethod.ReturnType.ToFullString();
            if (modifiedMethod.Modifiers.Any(m => m.ToFullString().Contains("async")))
            {
                returnTypeString = $"async {returnTypeString}";
            }

            if (!ProjectModifierHelper.TrimStatement(returnTypeString).Equals(ProjectModifierHelper.TrimStatement(methodChanges.EditType.Block)))
            {
                var typeIdentifier = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(methodChanges.EditType.Block));
                modifiedMethod = modifiedMethod.WithReturnType(typeIdentifier.WithTrailingTrivia(SyntaxFactory.Whitespace(" ")));

                return modifiedMethod;
            }

            return originalMethod;
        }

        internal static BaseMethodDeclarationSyntax AddParameters(BaseMethodDeclarationSyntax methodNode, CodeBlock[] addParameters, CodeChangeOptions toolOptions)
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

        private static SyntaxNode AddOrUpdateLambda(SyntaxNode originalMethod, CodeSnippet change)
        {
            var rootDescendants = GetDescendantNodes(originalMethod);
            var parent = GetSpecifiedNode(change.Parent, rootDescendants);
            if (parent is null)
            {
                return originalMethod;
            }

            var children = GetDescendantNodes(parent);

            var updatedParent = parent;

            // Check for existing lambda
            if (children.FirstOrDefault(
                d => d.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
                || d.IsKind(SyntaxKind.SimpleLambdaExpression)) is LambdaExpressionSyntax existingLambda)
            {
                updatedParent = GetNodeWithUpdatedLambda(existingLambda, change, parent);
            }
            else // Add a new lambda
            {
                updatedParent = AddLambdaToParent(parent, children, change);
            }

            return originalMethod.ReplaceNode(parent, updatedParent);
        }

        /// <summary>
        /// Given an existing lamba expression, updates the parameters and block as necessary
        /// </summary>
        /// <param name="existingLambda"></param>
        /// <param name="change"></param>
        /// <param name="parent"></param>
        /// <returns>parent with updated lambda expression</returns>
        internal static SyntaxNode GetNodeWithUpdatedLambda(LambdaExpressionSyntax existingLambda, CodeSnippet change, SyntaxNode parent)
        {
            var lambdaWithUpdatedParameters = UpdateLambdaParameters(existingLambda, change);
            var lambdaWithUpdatedBlock = UpdateLambdaBlock(lambdaWithUpdatedParameters, change);

            return parent.ReplaceNode(existingLambda, lambdaWithUpdatedBlock);
        }

        private static LambdaExpressionSyntax UpdateLambdaParameters(LambdaExpressionSyntax existingLambda, CodeSnippet change)
        {
            var existingParameters = GetDescendantNodes(existingLambda).Where(n => n.IsKind(SyntaxKind.Parameter));
            if (ProjectModifierHelper.StatementExists(existingParameters, change.Parameter))
            {
                return existingLambda;
            }

            var lambdaParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier(change.Parameter));
            // Lambda might be ParenthesizedLambda or SimpleLambda
            if (existingLambda is ParenthesizedLambdaExpressionSyntax updatedLambda)
            {
                updatedLambda = updatedLambda.AddParameterListParameters(lambdaParam);
            }
            else // if SimpleLambda we are adding a parameter and making it a ParenthesizedLambda
            {
                updatedLambda = SyntaxFactory.ParenthesizedLambdaExpression(existingLambda).AddParameterListParameters(lambdaParam);
            }

            if (updatedLambda == null)
            {
                return existingLambda;
            }

            return updatedLambda;
        }

        private static LambdaExpressionSyntax UpdateLambdaBlock(LambdaExpressionSyntax existingLambda, CodeSnippet change)
        {
            if (ProjectModifierHelper.StatementExists(existingLambda.Body, change.Block))
            {
                return existingLambda;
            }

            var leadingWhitespaceTrivia = existingLambda.GetLeadingTrivia().LastOrDefault();
            if (leadingWhitespaceTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                change.LeadingTrivia.NumberOfSpaces += leadingWhitespaceTrivia.Span.Length;
            }

            if (change.Replace)
            {
                return existingLambda.WithBody(GetStatementWithTrivia(change));
            }

            // Get new lambda block
            var updatedBlock = GetBlockStatement(existingLambda.Body, change);

            // Try to replace existing block with updated block
            if (existingLambda.WithBody(updatedBlock as CSharpSyntaxNode) is LambdaExpressionSyntax updatedLambda)
            {
                return updatedLambda;
            }

            return existingLambda;
        }

        internal static SyntaxNode AddLambdaToParent(SyntaxNode parent, IEnumerable<SyntaxNode> children, CodeSnippet change)
        {
            if (ProjectModifierHelper.StatementExists(children, change.Block))
            {
                return parent;
            }

            // Determine if there is an existing argument list to add the lambda
            if (!(children.FirstOrDefault(n => n.IsKind(SyntaxKind.ArgumentList)) is ArgumentListSyntax argList))
            {
                return parent;
            }

            // Create a lambda parameter 
            var parameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier(change.Parameter))
                .WithTrailingTrivia(SyntaxFactory.Space);

            var parentLeadingWhiteSpace = parent.GetLeadingTrivia().LastOrDefault();
            if (parentLeadingWhiteSpace.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                change.LeadingTrivia.NumberOfSpaces += parentLeadingWhiteSpace.Span.Length;
            }

            // Ensure that block statement is valid
            if (!(GetBlockStatement(SyntaxFactory.Block(), change) is BlockSyntax block))
            {
                return parent;
            }

            // Update white space for non-top-level statements
            if (parentLeadingWhiteSpace.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                block = block
                    .WithOpenBraceToken(block.OpenBraceToken.WithLeadingTrivia(parentLeadingWhiteSpace))
                    .WithCloseBraceToken(block.CloseBraceToken.WithLeadingTrivia(parentLeadingWhiteSpace));
            }

            // Create lambda expression with parameter and block (add leading newline to block for formatting)
            var newLambdaExpression = SyntaxFactory.SimpleLambdaExpression(
                parameter,
                block.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, parentLeadingWhiteSpace));

            // Add lambda to parent block's argument list
            var argument = SyntaxFactory.Argument(newLambdaExpression);
            var updatedParent = parent.ReplaceNode(argList, argList.AddArguments(argument));

            return updatedParent;
        }

        // return modified parent node with code snippet added
        internal static SyntaxNode AddExpressionToParent(SyntaxNode originalMethod, CodeSnippet change)
        {
            // Determine the parent node onto which we are adding
            var parent = GetSpecifiedNode(change.Parent, GetDescendantNodes(originalMethod));
            if (parent is null)
            {
                return originalMethod;
            }

            var children = GetDescendantNodes(parent);
            if (ProjectModifierHelper.StatementExists(children, change.Block))
            {
                return originalMethod;
            }

            // Find parent's expression statement
            if (!(children.FirstOrDefault(n => n.IsKind(SyntaxKind.ExpressionStatement)) is ExpressionStatementSyntax exprNode))
            {
                return originalMethod;
            }

            // Create new expression to update old expression
            var leadingTrivia = GetLeadingTrivia(change.LeadingTrivia);
            var identifier = SyntaxFactory.IdentifierName(change.Block);

            var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                exprNode.Expression.WithTrailingTrivia(leadingTrivia),
                identifier);

            var modifiedExprNode = exprNode.WithExpression(newExpression);

            if (modifiedExprNode == null)
            {
                return originalMethod;
            }

            // Replace existing expression with updated expression
            var updatedParent = parent.ReplaceNode(exprNode, modifiedExprNode);

            return updatedParent != null ? originalMethod.ReplaceNode(parent, updatedParent) : originalMethod;
        }

        internal static IEnumerable<SyntaxNode> GetDescendantNodes(SyntaxNode root)
        {
            if (root is BlockSyntax block)
            {
                return block.Statements;
            }
            else if (root is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.Members;
            }

            return root.DescendantNodes();
        }

        // create UsingDirectiveSyntax[] using a string[] to add to the root of the class (root.Usings).
        internal static UsingDirectiveSyntax[] CreateUsings(string[] usings)
        {
            var usingDirectiveList = new List<UsingDirectiveSyntax>();
            if (usings == null)
            {
                return usingDirectiveList.ToArray();
            }

            var nameLeadingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Space);
            var usingTrailingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed);
            foreach (var usingDirectiveString in usings)
            {
                if (!string.IsNullOrEmpty(usingDirectiveString))
                {
                    //leading space on the value of the using eg. (using' 'Microsoft.Yadada)
                    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingDirectiveString)
                        .WithLeadingTrivia(nameLeadingTrivia))
                        .WithAdditionalAnnotations(Formatter.Annotation)
                        .WithTrailingTrivia(usingTrailingTrivia);

                    usingDirectiveList.Add(usingDirective);
                }
            }

            return usingDirectiveList.ToArray();
        }

        // create AttributeListSyntax using string[] to add on top of a ClassDeclrationSyntax
        internal static SyntaxList<AttributeListSyntax> CreateAttributeList(CodeBlock[] attributes, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTriviaList leadingTrivia)
        {
            if (attributes == null)
            {
                return attributeLists;
            }

            foreach (var attribute in attributes)
            {
                var attributeList = new List<AttributeSyntax>();
                // filter by apps
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

                    attributeLists = attributeLists.Insert(0, attributeListSyntax);
                }
            }

            return attributeLists;
        }

        //here for mostly testing
        internal Document GetDocument() => _documentEditor.GetChangedDocument();

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

        internal static bool PropertyExists(string property, SyntaxList<MemberDeclarationSyntax> members)
        {
            if (string.IsNullOrEmpty(property))
            {
                return false;
            }
            var trimmedProperty = property.Trim(ProjectModifierHelper.CodeSnippetTrimChars);

            return members.Where(m => m.ToString().Trim(ProjectModifierHelper.CodeSnippetTrimChars).Equals(trimmedProperty)).Any();
        }

        private static SyntaxTrivia SemiColonTrivia => SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia()
                    .WithTokens(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
    }
}
