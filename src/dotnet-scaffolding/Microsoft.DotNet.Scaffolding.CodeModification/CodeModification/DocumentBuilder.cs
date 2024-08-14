// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Data;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

internal class DocumentBuilder
{
    private readonly CodeFile _codeFile;
    private readonly ILogger _consoleLogger;
    private readonly Document _document;
    private readonly IList<string> _options;

    public DocumentBuilder(
        Document document,
        CodeFile codeFile,
        IList<string> options,
        ILogger consoleLogger)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _codeFile = codeFile ?? throw new ArgumentNullException(nameof(codeFile));
        _options = options ?? [];
        _consoleLogger = consoleLogger ?? throw new ArgumentNullException(nameof(consoleLogger));
    }

    public async Task<Document> RunAsync()
    {
        var document = _document;
        var syntaxRoot = await document.GetSyntaxRootAsync() as CompilationUnitSyntax;
        var modifiedRoot = ModifyRoot(syntaxRoot, _options);
        if (modifiedRoot != null)
        {
            return document.WithSyntaxRoot(modifiedRoot);
        }

        //return the same document if syntax root came back null.
        return document;
    }

    internal static BaseMethodDeclarationSyntax GetModifiedMethod(string fileName, BaseMethodDeclarationSyntax method, Method methodChanges, IList<string> options, StringBuilder? output)
    {
        method = ModifyMethodAttributes(method, methodChanges, options);
        method = AddCodeSnippetsToMethod(fileName, method, methodChanges, options, output);
        method = EditMethodReturnType(method, methodChanges, options);
        method = AddMethodParameters(method, methodChanges, options);
        return method;
    }

    private static BaseMethodDeclarationSyntax ModifyMethodAttributes(BaseMethodDeclarationSyntax method, Method methodChanges, IList<string> options)
    {
        if (methodChanges.Attributes != null && methodChanges.Attributes.Any())
        {
            var attributes = ProjectModifierHelper.FilterCodeBlocks(methodChanges.Attributes, options);
            var methodAttributes = CreateAttributeList(attributes, method.AttributeLists, method.GetLeadingTrivia());

            method = method.WithAttributeLists(methodAttributes);
        }

        return method;
    }

    public CompilationUnitSyntax? ModifyRoot(CompilationUnitSyntax? root, IList<string> options)
    {
        if (root is null)
        {
            return null;
        }

        root = AddUsings(root, options);
        if (!string.IsNullOrEmpty(_codeFile.FileName) && _codeFile.FileName.Equals("Program.cs") &&
            _codeFile.Methods != null && _codeFile.Methods.TryGetValue("Global", out var globalChanges))
        {
            var filteredChanges = ProjectModifierHelper.FilterCodeSnippets(globalChanges.CodeChanges, options);
            var updatedIdentifer = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(root.Members);
            if (updatedIdentifer.HasValue)
            {
                (string oldValue, string newValue) = updatedIdentifer.Value;
                filteredChanges = ProjectModifierHelper.UpdateVariables(filteredChanges, oldValue, newValue);
            }

            if (!options.Contains(Constants.UseTopLevelStatements, StringComparer.OrdinalIgnoreCase))
            {
                var mainMethod = root?.DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(n => ProjectModifierHelper.Main.Equals(n.Identifier.ToString(), StringComparison.OrdinalIgnoreCase));
                if (mainMethod != null
                    && ApplyChangesToMethod(mainMethod.Body, filteredChanges, _codeFile.FileName) is BlockSyntax updatedBody)
                {
                    var updatedMethod = mainMethod.WithBody(updatedBody);
                    return root?.ReplaceNode(mainMethod, updatedMethod);
                }
            }
            else if (root.Members.Any(node => node.IsKind(SyntaxKind.GlobalStatement)))
            {
                return ApplyChangesToMethod(root, filteredChanges, _codeFile.FileName) as CompilationUnitSyntax;
            }
        }
        else if (!string.IsNullOrEmpty(_codeFile.FileName))
        {
            var namespaceNode = root?.Members.OfType<BaseNamespaceDeclarationSyntax>()?.FirstOrDefault();
            string className = ProjectModifierHelper.GetClassName(_codeFile.FileName);
            // get classNode. All class changes are done on the ClassDeclarationSyntax and then that node is replaced using documentEditor.
            var classDeclarationSyntax = namespaceNode?.
                DescendantNodes().
                OfType<ClassDeclarationSyntax>().
                FirstOrDefault(node =>
                    node.Identifier.ValueText.Contains(className));
            if (classDeclarationSyntax != null)
            {
                var modifiedClassDeclarationSyntax = classDeclarationSyntax;
                //add class properties
                modifiedClassDeclarationSyntax = AddProperties(modifiedClassDeclarationSyntax, options);
                //add class attributes
                modifiedClassDeclarationSyntax = AddClassAttributes(modifiedClassDeclarationSyntax, options);
                //add code snippets/changes.
                if (_codeFile.Methods != null)
                {
                    modifiedClassDeclarationSyntax = ModifyMethods(_codeFile.FileName, modifiedClassDeclarationSyntax, _codeFile.Methods, options);
                }

                if (root is SyntaxNode syntaxRoot)
                {
                    root = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclarationSyntax);
                }
            }
        }

        return root;
    }

    public CompilationUnitSyntax AddUsings(CompilationUnitSyntax docRoot, IList<string> options)
    {
        // adding usings
        if (_codeFile.UsingsWithOptions != null && _codeFile.UsingsWithOptions.Any())
        {
            var usingsWithOptions = FilterUsingsWithOptions(_codeFile, options);
            _codeFile.Usings = _codeFile.Usings?.Concat(usingsWithOptions).ToArray() ?? usingsWithOptions.ToArray();
        }

        var usingNodes = CreateUsings(_codeFile.Usings);
        if (usingNodes.Any() && docRoot.Usings.Count == 0)
        {
            return docRoot.WithUsings(SyntaxFactory.List(usingNodes));
        }
        else
        {
            var uniqueUsings = GetUniqueUsings(docRoot.Usings.ToArray(), usingNodes);
            return uniqueUsings.Any() ? docRoot.WithUsings(docRoot.Usings.AddRange(uniqueUsings)) : docRoot;
        }
    }

    internal static SyntaxList<UsingDirectiveSyntax> GetUniqueUsings(UsingDirectiveSyntax[] existingUsings, UsingDirectiveSyntax[] newUsings)
    {
        return SyntaxFactory.List(
            newUsings.Where(u => !existingUsings.Any(oldUsing => oldUsing.Name != null && oldUsing.Name.ToString().Equals(u.Name?.ToString())))
                     .OrderBy(us => us.Name?.ToString()));
    }

    internal static IList<string> FilterUsingsWithOptions(CodeFile codeFile, IList<string> options)
    {
        List<string> usingsWithOptions = [];
        if (codeFile != null)
        {
            var filteredCodeBlocks = codeFile.UsingsWithOptions?.Where(us => ProjectModifierHelper.FilterOptions(us.Options, options)).ToList();
            if (filteredCodeBlocks != null && filteredCodeBlocks.Count != 0)
            {
                usingsWithOptions = filteredCodeBlocks.Where(cb => !string.IsNullOrEmpty(cb.Block)).Select(cb => cb.Block!).ToList();
            }
        }

        return usingsWithOptions;
    }

    //Add class members to the top of the class.
    public ClassDeclarationSyntax AddProperties(ClassDeclarationSyntax classDeclarationSyntax, IList<string> options)
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
    public ClassDeclarationSyntax AddClassAttributes(ClassDeclarationSyntax classDeclarationSyntax, IList<string> options)
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

    internal static ClassDeclarationSyntax ModifyMethods(string fileName, ClassDeclarationSyntax classNode, Dictionary<string, Method> methods, IList<string> options, StringBuilder? output = null)
    {
        foreach ((string methodName, Method methodChanges) in methods)
        {
            if (methodChanges is null || methodChanges.Parameters is null)
            {
                continue;
            }

            var methodNode = ProjectModifierHelper.GetOriginalMethod(classNode, methodName, methodChanges);
            if (methodNode is null)
            {
                continue;
            }

            var parameters = ProjectModifierHelper.VerifyParameters(methodChanges.Parameters, methodNode.ParameterList.Parameters.ToList());
            foreach ((string oldValue, string newValue) in parameters)
            {
                methodChanges.CodeChanges = ProjectModifierHelper.UpdateVariables(methodChanges.CodeChanges, oldValue, newValue);
            }

            var updatedMethodNode = GetModifiedMethod(fileName, methodNode, methodChanges, options, output);
            if (updatedMethodNode != null)
            {
                classNode = classNode.ReplaceNode(methodNode, updatedMethodNode);
            }
        }

        return classNode;
    }

    internal static BaseMethodDeclarationSyntax AddMethodParameters(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, IList<string> options)
    {
        if (methodChanges is null || methodChanges.AddParameters is null || !methodChanges.AddParameters.Any())
        {
            return originalMethod;
        }

        // Filter for IList<string>
        methodChanges.AddParameters = ProjectModifierHelper.FilterCodeBlocks(methodChanges.AddParameters, options);
        return AddParameters(originalMethod, methodChanges.AddParameters, options);
    }

    // Add all the different code snippet.
    internal static BaseMethodDeclarationSyntax AddCodeSnippetsToMethod(string fileName, BaseMethodDeclarationSyntax originalMethod, Method methodChanges, IList<string> options, StringBuilder? output)
    {
        if (methodChanges.CodeChanges == null || !methodChanges.CodeChanges.Any())
        {
            return originalMethod;
        }

        var filteredChanges = ProjectModifierHelper.FilterCodeSnippets(methodChanges.CodeChanges, options);

        if (filteredChanges is null || !filteredChanges.Any())
        {
            return originalMethod;
        }

        var blockSyntax = originalMethod.Body;
        var modifiedMethod = ApplyChangesToMethod(blockSyntax, filteredChanges, fileName, output);

        if (blockSyntax != null && modifiedMethod != null)
        {
            return originalMethod.ReplaceNode(blockSyntax, modifiedMethod);
        }

        return originalMethod;
    }

    internal static SyntaxNode? ApplyChangesToMethod(SyntaxNode? root, CodeSnippet[]? filteredChanges, string? fileName = null, StringBuilder? output = null)
    {
        if (root is null)
        {
            return null;
        }

        bool changesMade = false;
        if (filteredChanges is null)
        {
            return root;
        }

        foreach (var change in filteredChanges)
        {
            var update = ModifyMethod(root, change, output);
            if (update != null)
            {
                changesMade = true;
                root = root.ReplaceNode(root, update);
            }
        }

        if (!changesMade)
        {
            output?.AppendLine(value: $"No modifications made for file: {fileName}");
        }
        else
        {
            output?.AppendLine($"Modified {fileName}");
        }

        return root;
    }

    internal static MethodDeclarationSyntax? GetMethodFromSyntaxRoot(CompilationUnitSyntax root, string methodIdentifier)
    {
        BaseNamespaceDeclarationSyntax? namespaceNode = root.Members.OfType<NamespaceDeclarationSyntax>()?.FirstOrDefault();
        if (namespaceNode == null)
        {
            namespaceNode = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>()?.FirstOrDefault();
        }

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

    public static CodeSnippet AddLeadingTriviaSpaces(CodeSnippet snippet, int spaces)
    {
        snippet.LeadingTrivia = snippet.LeadingTrivia ?? new Formatting();
        snippet.LeadingTrivia.NumberOfSpaces += spaces;
        return snippet;
    }

    internal static CodeSnippet[] AddLeadingTriviaSpaces(CodeSnippet[] snippets, int spaces)
    {
        for (int i = 0; i < snippets.Length; i++)
        {
            var snippet = snippets[i];
            snippet = AddLeadingTriviaSpaces(snippet, spaces);
            snippets[i] = snippet;
        }

        return snippets;
    }

    internal static SyntaxNode ModifyMethod(SyntaxNode originalMethod, CodeSnippet codeChange, StringBuilder? output = null)
    {
        SyntaxNode? modifiedMethod = null;
        try
        {
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
        }
        catch
        {
            //output?.Append(value: $"Error modifying method {originalMethod}\nCodeChange:{codeChange.ToJson()}");
        }

        return modifiedMethod != null ? originalMethod.ReplaceNode(originalMethod, modifiedMethod) : originalMethod;
    }

    internal static SyntaxNode UpdateMethod(SyntaxNode originalMethod, CodeSnippet codeChange)
    {
        var children = GetDescendantNodes(originalMethod);
        if (children is null)
        {
            return originalMethod;
        }

        //check for CodeChange.Block and CodeChange.CheckBlock for  block's are easy to check.
        if (ProjectModifierHelper.StatementExists(children, codeChange.Block) || !string.IsNullOrEmpty(codeChange.CheckBlock) && ProjectModifierHelper.StatementExists(children, codeChange.CheckBlock))
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

    private static SyntaxNode? GetFollowingNode(string[]? insertBefore, IEnumerable<SyntaxNode> descendantNodes)
    {
        if (insertBefore != null)
        {
            foreach (var specifier in insertBefore)
            {
                if (GetSpecifiedNode(specifier, descendantNodes) is SyntaxNode insertBeforeNode)
                {
                    return insertBeforeNode;
                }
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
    private static List<SyntaxNode>? GetNodeInsertionList(CodeSnippet codeChange, SyntaxKind syntaxKind)
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
    internal static SyntaxNode? GetSpecifiedNode(string? specifierStatement, IEnumerable<SyntaxNode> descendantNodes)
    {
        if (string.IsNullOrEmpty(specifierStatement))
        {
            return null;
        }

        var specifiedDescendant =
            descendantNodes?.FirstOrDefault(d => d != null && d.ToString().Contains(specifierStatement)) ??
            descendantNodes?.FirstOrDefault(d => d != null && d.ToString().Contains(ProjectModifierHelper.TrimStatement(specifierStatement)));

        return specifiedDescendant;
    }

    internal static BaseMethodDeclarationSyntax EditMethodReturnType(BaseMethodDeclarationSyntax originalMethod, Method methodChanges, IList<string> options)
    {
        if (methodChanges is null || methodChanges.EditType is null || !(originalMethod is MethodDeclarationSyntax modifiedMethod))
        {
            return originalMethod;
        }

        methodChanges.EditType = ProjectModifierHelper.FilterCodeBlocks(new CodeBlock[] { methodChanges.EditType }, options).FirstOrDefault();

        // After filtering, the method type might not need editing
        if (methodChanges.EditType is null || string.IsNullOrEmpty(methodChanges.EditType.Block))
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

    internal static BaseMethodDeclarationSyntax AddParameters(BaseMethodDeclarationSyntax methodNode, CodeBlock[] addParameters, IList<string> toolOptions)
    {
        var newMethod = methodNode;
        List<ParameterSyntax> newParameters = new List<ParameterSyntax>();
        foreach (var parameter in addParameters)
        {
            if (string.IsNullOrEmpty(parameter.Block))
            {
                continue;
            }

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
        var descendants = GetDescendantNodes(originalMethod);
        if (descendants is null)
        {
            return originalMethod;
        }

        var parent = GetSpecifiedNode(change.Parent, descendants);
        if (parent is null)
        {
            return originalMethod;
        }

        var children = GetDescendantNodes(parent);
        if (children is null)
        {
            return originalMethod;
        }

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
        var existingParameters = GetDescendantNodes(existingLambda)?.Where(n => n.IsKind(SyntaxKind.Parameter));
        if (existingParameters is null || string.IsNullOrEmpty(change.Parameter))
        {
            return existingLambda;
        }

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
        if (ProjectModifierHelper.StatementExists(children, change.Block) ||
            string.IsNullOrEmpty(change.Parameter) ||
            // Determine if there is an existing argument list to add the lambda
            !(children.FirstOrDefault(n => n.IsKind(SyntaxKind.ArgumentList)) is ArgumentListSyntax argList))
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
            block.WithLeadingTrivia(parentLeadingWhiteSpace));

        // Add lambda to parent block's argument list
        var argument = SyntaxFactory.Argument(newLambdaExpression);
        var updatedParent = parent.ReplaceNode(argList, argList.AddArguments(argument));

        return updatedParent;
    }

    // return modified parent node with code snippet added
    internal static SyntaxNode AddExpressionToParent(SyntaxNode originalMethod, CodeSnippet change)
    {
        // Determine the parent node onto which we are adding
        var descendantNodes = GetDescendantNodes(originalMethod);
        if (descendantNodes is null)
        {
            return originalMethod;
        }

        var parent = GetSpecifiedNode(change.Parent, descendantNodes);
        if (parent is null)
        {
            return originalMethod;
        }

        var children = GetDescendantNodes(parent);
        if (children is null)
        {
            return originalMethod;
        }

        if (ProjectModifierHelper.StatementExists(children, change.Block))
        {
            return originalMethod;
        }

        // Find parent's expression statement
        var exprNode = children.FirstOrDefault(n => n.IsKind(SyntaxKind.ExpressionStatement)) as ExpressionStatementSyntax;
        var invocationExpression = children.FirstOrDefault(n => n.IsKind(SyntaxKind.InvocationExpression)) as InvocationExpressionSyntax;
        if (exprNode is null && invocationExpression is null)
        {
            return originalMethod;
        }

        // Create new expression to update old expression
        var leadingTrivia = GetLeadingTrivia(change.LeadingTrivia);
        var identifier = SyntaxFactory.IdentifierName(change.Block);
        SyntaxNode? updatedParent = null;
        if (exprNode != null)
        {
            var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                exprNode.Expression.WithTrailingTrivia(leadingTrivia),
                identifier);

            var modifiedExprNode = exprNode.WithExpression(newExpression);
            if (modifiedExprNode is null)
            {
                return originalMethod;
            }

            // Replace existing expression with updated expression
            updatedParent = parent.ReplaceNode(exprNode, modifiedExprNode);
        }
        //add the scenario to check for an InvocationExpressionSyntax and update it if needed.
        else if (invocationExpression != null)
        {
            // Parse the method call string into an InvocationExpressionSyntax, helps extract the ArgumentList out.
            var identifierExpression = SyntaxFactory.ParseExpression(change.Block) as InvocationExpressionSyntax;
            //create a new MemberAccessExpression with the parsed method call and the identifier.
            var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocationExpression,
                identifierExpression?.Expression as IdentifierNameSyntax ?? identifier
            );

            InvocationExpressionSyntax? modifiedExprNode = null;
            if (identifierExpression != null)
            {
                // Combine the original invocation expression with the parsed method call's argument list
                modifiedExprNode = SyntaxFactory.InvocationExpression(
                    newExpression,
                    identifierExpression.ArgumentList
                );
            }
            else
            {
                modifiedExprNode = invocationExpression.WithExpression(newExpression);
            }

            // Ensure modifiedExprNode is not null
            if (modifiedExprNode is null)
            {
                return originalMethod;
            }

            // Replace existing expression with updated expression
            updatedParent = parent.ReplaceNode(invocationExpression, modifiedExprNode);
        }

        return updatedParent != null ? originalMethod.ReplaceNode(parent, updatedParent) : originalMethod;
    }

    internal static IEnumerable<SyntaxNode>? GetDescendantNodes(SyntaxNode root)
    {
        if (root is BlockSyntax block)
        {
            return block.Statements;
        }
        else if (root is CompilationUnitSyntax compilationUnit)
        {
            return compilationUnit.Members;
        }

        return root?.DescendantNodes();
    }

    // create UsingDirectiveSyntax[] using a string[] to add to the root of the class (root.Usings).
    internal static UsingDirectiveSyntax[] CreateUsings(string[]? usings)
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


