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

        internal BaseMethodDeclarationSyntax GetOriginalMethod(ClassDeclarationSyntax classNode, string methodName, Method methodChanges)
        {
            if (classNode?.Members.FirstOrDefault(node
                => node is MethodDeclarationSyntax mds && mds.Identifier.ValueText.Equals(methodName)
                || node is ConstructorDeclarationSyntax cds && cds.Identifier.ValueText.Equals(methodName))
                 is BaseMethodDeclarationSyntax foundMethod)
            {
                var parameters = VerifyParameters(methodChanges.Parameters, foundMethod.ParameterList.Parameters.ToList());
                if (parameters == null)
                {
                    return null;
                }

                return foundMethod;
            }

            return null;
        }

        internal BaseMethodDeclarationSyntax GetModifiedMethod(BaseMethodDeclarationSyntax method, Method methodChanges, CodeChangeOptions options)
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
                var usingList = _codeFile.Usings.ToList();
                usingList.AddRange(usingsWithOptions);
                _codeFile.Usings = usingList.ToArray();
            }

            var usingNodes = CreateUsings(_codeFile.Usings);
            if (usingNodes.Any() && _docRoot.Usings.Count == 0)
            {
                return _docRoot.WithUsings(SyntaxFactory.List(usingNodes));
            }
            else
            {
                var uniqueUsings = SyntaxFactory.List(
                    usingNodes.Where(u => !_docRoot.Usings.Any(
                            oldUsing => oldUsing.Name.ToString().Equals(u.Name.ToString()))));

                return uniqueUsings.Any() ? _docRoot.WithUsings(_docRoot.Usings.AddRange(uniqueUsings)) : _docRoot;
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

        internal BaseMethodDeclarationSyntax AddMethodParameters(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
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
        internal BaseMethodDeclarationSyntax AddCodeSnippetsToMethod(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
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

        private static SyntaxNode UpdateMethod(SyntaxNode originalMethod, CodeSnippet codeChange)
        {
            var children = GetDescendantNodes(originalMethod);
            if (ProjectModifierHelper.StatementExists(children, codeChange.Block))
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
                updatedMethod = AddStatement(originalMethod, codeChange);
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

        private static List<SyntaxNode> GetNodeInsertionList(CodeSnippet codeChange, SyntaxKind syntaxKind)
        {
            var leadingTrivia = GetLeadingTrivia(codeChange.CodeFormatting);
            var trailingTrivia = SyntaxFactory.TriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed);
            var statement = GetStatementWithTrivia(codeChange.Block, leadingTrivia, trailingTrivia);

            if (statement is null)
            {
                return null;
            }

            return syntaxKind == SyntaxKind.CompilationUnit
               ? new List<SyntaxNode> { SyntaxFactory.GlobalStatement(statement) }
               : new List<SyntaxNode> { statement };
        }

        private static SyntaxNode AddStatement(SyntaxNode node, CodeSnippet codeChange)
        {
            var syntaxKind = node.Kind();

            var statement = GetStatementWithTrivia(codeChange.Block, GetLeadingTrivia(codeChange.CodeFormatting),
                    SyntaxFactory.TriviaList(SemiColonTrivia, SyntaxFactory.CarriageReturnLineFeed));

            if (syntaxKind == SyntaxKind.Block && node is BlockSyntax block)
            {
                return codeChange.Prepend
                    ? block.WithStatements(block.Statements.Insert(0, statement))
                    : block.AddStatements(statement);
            }
            else if (syntaxKind == SyntaxKind.CompilationUnit && node is CompilationUnitSyntax compilationUnit)
            {
                var globalStatement = SyntaxFactory.GlobalStatement(statement);
                return codeChange.Prepend
                    ? compilationUnit.WithMembers(compilationUnit.Members.Insert(0, globalStatement))
                    : compilationUnit.AddMembers(globalStatement);
            }

            return node;
        }

        private static StatementSyntax GetStatementWithTrivia(string formattedBlock, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        {
            return SyntaxFactory.ParseStatement(formattedBlock)
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

        private static SyntaxNode GetSpecifiedNode(string specifierStatement, IEnumerable<SyntaxNode> descendantNodes)
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

        internal BaseMethodDeclarationSyntax EditMethodReturnType(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, CodeChangeOptions options)
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

        private static SyntaxNode AddOrUpdateLambda(SyntaxNode originalMethod, CodeSnippet change)
        {
            var rootDescendants = GetDescendantNodes(originalMethod);
            var parent = GetSpecifiedNode(change.Parent, rootDescendants);
            var children = GetDescendantNodes(parent);

            var updatedParent = parent;
            // Check for existing lambda
            if (children.FirstOrDefault(d => d.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || d.IsKind(SyntaxKind.SimpleLambdaExpression)) is LambdaExpressionSyntax existingLambda)
            {
                updatedParent = UpdateLambda(existingLambda, change, parent);
            }
            else // Add a new lambda
            {
                updatedParent = AddLambdaToParent(parent, children, change);
            }

            return originalMethod.ReplaceNode(parent, updatedParent);
        }

        private static SyntaxNode UpdateLambda(LambdaExpressionSyntax existingLambda, CodeSnippet change, SyntaxNode parent)
        {
            var children = GetDescendantNodes(existingLambda);
            var modifiedLambda = UpdateLambdaParameter(existingLambda, children, change);
            modifiedLambda = UpdateLambdaBlock(modifiedLambda, children, change);

            return parent.ReplaceNode(existingLambda, modifiedLambda);
        }

        private static LambdaExpressionSyntax UpdateLambdaParameter(LambdaExpressionSyntax existingLambda, IEnumerable<SyntaxNode> lambdaChildren, CodeSnippet change)
        {
            if (ProjectModifierHelper.StatementExists(lambdaChildren, change.Parameter))
            {
                return existingLambda;
            }

            var lambdaParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier(change.Parameter));
            if (existingLambda is ParenthesizedLambdaExpressionSyntax updatedLambda)
            {
                updatedLambda = updatedLambda.AddParameterListParameters(lambdaParam);
            }
            else
            {
                updatedLambda = SyntaxFactory.ParenthesizedLambdaExpression(existingLambda).AddParameterListParameters(lambdaParam);
            }

            if (updatedLambda == null)
            {
                return existingLambda;
            }

            return updatedLambda;
        }

        private static LambdaExpressionSyntax UpdateLambdaBlock(LambdaExpressionSyntax existingLambda, IEnumerable<SyntaxNode> lambdaChildren, CodeSnippet change)
        {
            if (ProjectModifierHelper.StatementExists(existingLambda.Block, change.Block))
            {
                return existingLambda;
            }

            var updatedBlock = AddStatement(existingLambda.Block, change);
            if (existingLambda.WithBlock(updatedBlock as BlockSyntax) is LambdaExpressionSyntax updatedLambda)
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

            if (!(children.FirstOrDefault(n => n.IsKind(SyntaxKind.ArgumentList)) is ArgumentListSyntax argList))
            {
                return parent;
            }

            var parameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier(change.Parameter))
                .WithTrailingTrivia(SyntaxFactory.Space);

            if (!(AddStatement(SyntaxFactory.Block(), change) is BlockSyntax block))
            {
                return parent;
            }

            var newLambdaExpression = SyntaxFactory.SimpleLambdaExpression(
                parameter,
                block.WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed));

            var argument = SyntaxFactory.Argument(newLambdaExpression);
            var updatedParent = parent.ReplaceNode(argList, argList.AddArguments(argument));

            return updatedParent;
        }

        // return modified parent node with code snippet added
        internal static SyntaxNode AddExpressionToParent(SyntaxNode originalMethod, CodeSnippet change)
        {
            var parent = GetSpecifiedNode(change.Parent, GetDescendantNodes(originalMethod));
            var children = GetDescendantNodes(parent);
            if (ProjectModifierHelper.StatementExists(children, change.Block))
            {
                return originalMethod;
            }

            if (!(children.FirstOrDefault(n => n.IsKind(SyntaxKind.ExpressionStatement)) is ExpressionStatementSyntax exprNode))
            {
                return originalMethod;
            }

            var leadingTrivia = GetLeadingTrivia(change.CodeFormatting);
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
        internal UsingDirectiveSyntax[] CreateUsings(string[] usings)
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
        internal SyntaxList<AttributeListSyntax> CreateAttributeList(CodeBlock[] attributes, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTriviaList leadingTrivia)
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

        // check if the parameters match for the given method, and populate a Dictionary with parameter.Type keys and Parameter.Identifier values.
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

        internal bool PropertyExists(string property, SyntaxList<MemberDeclarationSyntax> members)
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
