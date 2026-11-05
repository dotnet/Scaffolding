// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal sealed class BlazorDbContextFactoryStep(ILogger<BlazorDbContextFactoryStep> logger) : ScaffoldStep
{
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(CrudSettings), out var settingsObject) ||
            settingsObject is not CrudSettings settings ||
            !context.Properties.TryGetValue(nameof(BlazorCrudModel), out var modelObject) ||
            modelObject is not BlazorCrudModel model)
        {
            logger.LogError("Cannot register the Blazor DbContext factory: scaffolding settings or model are missing.");
            return false;
        }

        try
        {
            using var codeService = new CodeService(logger, settings.Project);
            var document = await codeService.GetDocumentAsync("Program.cs")
                ?? throw new InvalidOperationException("Program.cs was not found in the target project.");
            var contextInfo = model.DbContextInfo;
            var contextName = string.IsNullOrEmpty(contextInfo.DbContextNamespace)
                ? contextInfo.DbContextClassName!
                : $"{contextInfo.DbContextNamespace}.{contextInfo.DbContextClassName}";
            var connection = $"builder.Configuration.GetConnectionString(\"{contextInfo.DbContextClassName}\") ?? throw new InvalidOperationException(\"Connection string '{contextInfo.DbContextClassName}' not found.\")";
            var properties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(contextInfo, connection);
            properties.TryGetValue(Constants.CodeModifierPropertyConstants.UseDbMethod, out var useDbMethod);
            var updated = await TryRegisterFactoryAsync(document, contextName, useDbMethod, logger, cancellationToken);
            if (updated is null)
            {
                // Later Blazor recipes must also leave unsupported startup code untouched.
                model.ProjectInfo.CodeChangeOptions?.Remove("EfScenario");
                context.Properties.Remove(Constants.StepConstants.AdditionalCodeModifier);
                return true;
            }

            if (!codeService.TryApplyChanges(updated.Project.Solution))
            {
                logger.LogError("Failed to save the DbContext factory registration in Program.cs.");
                return false;
            }

            logger.LogInformation("Verified DbContext factory registration for '{ContextName}'.", contextName);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            logger.LogError(ex, "Failed to register the Blazor DbContext factory: {Message}", ex.Message);
            return false;
        }
    }

    internal static async Task<Document> RegisterFactoryAsync(
        Document document, string contextName, string? useDbMethod, CancellationToken cancellationToken = default, ILogger? logger = null)
        => await TryRegisterFactoryAsync(document, contextName, useDbMethod, logger ?? NullLogger.Instance, cancellationToken) ?? document;

    private static async Task<Document?> TryRegisterFactoryAsync(
        Document document, string contextName, string? useDbMethod, ILogger logger, CancellationToken cancellationToken)
    {
        var builderName = "builder";
        Document? Warn(string reason)
        {
            var provider = string.IsNullOrWhiteSpace(useDbMethod) ? null : SyntaxFactory.ParseExpression(useDbMethod);
            var arguments = "";
            if (provider is InvocationExpressionSyntax invocation && !invocation.ContainsDiagnostics)
            {
                provider = provider.ReplaceNodes(provider.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Where(identifier => identifier.Identifier.ValueText == "builder"),
                    (identifier, _) => SyntaxFactory.IdentifierName(builderName).WithTriviaFrom(identifier));
                arguments = $"options => options.{provider}";
            }
            else
            {
                reason += " Configure the database provider options for this registration.";
            }

            var registration = $"{builderName}.Services.AddDbContextFactory<{contextName}>({arguments});";
            logger.LogWarning("Could not safely update Program.cs. Add before {BuilderName}.Build(): {Registration} {Reason}",
                builderName, registration, reason);
            return null;
        }

        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
        {
            return Warn("Program.cs is not a C# compilation unit.");
        }

        if (!root.Usings.Any(usingDirective => usingDirective.Name?.ToString() == "Microsoft.EntityFrameworkCore"))
        {
            root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore"))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)));
            document = document.WithSyntaxRoot(root);
        }

        root = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken))!;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel is null)
        {
            return Warn("Cannot analyze Program.cs.");
        }

        var builders = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(variable =>
            variable.Initializer?.Value is InvocationExpressionSyntax invocation &&
            semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method &&
            method.Name is "CreateBuilder" or "CreateSlimBuilder" or "CreateEmptyBuilder" &&
            method.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Builder.WebApplication")
            .ToArray();
        if (builders.Length != 1)
        {
            return Warn("Expected exactly one WebApplication builder declaration.");
        }

        var builder = builders[0];
        builderName = builder.Identifier.ValueText;
        var contextType = semanticModel.Compilation.GetTypeByMetadataName(contextName);
        if (contextType is null)
        {
            return Warn($"Cannot resolve the selected DbContext '{contextName}'.");
        }

        var builds = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax receiver,
                Name.Identifier.ValueText: "Build"
            } && SymbolEqualityComparer.Default.Equals(
                semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol,
                semanticModel.GetDeclaredSymbol(builder, cancellationToken)))
            .ToArray();
        if (builds.Length != 1)
        {
            return Warn("Cannot locate exactly one Build call for the selected WebApplication builder.");
        }

        var build = builds[0];
        var buildStatement = build.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
        if (buildStatement is null)
        {
            return Warn("The Build call is not a startup statement.");
        }

        var container = buildStatement.Parent is GlobalStatementSyntax global ? global.Parent : buildStatement.Parent;
        if (buildStatement.Ancestors().Any(node => node is not
            (BlockSyntax or GlobalStatementSyntax or CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax or
                MethodDeclarationSyntax { Identifier.ValueText: "Main" })))
        {
            return Warn("The Build call is not in unconditional application startup code.");
        }

        List<InvocationExpressionSyntax> registrations = [];
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic,
                GenericNameSyntax generic => generic,
                _ => null
            };
            if (name is null || name.Identifier.ValueText is not
                ("AddDbContext" or "AddDbContextPool" or "ConfigureDbContext" or "AddDbContextFactory" or "AddPooledDbContextFactory"))
            {
                continue;
            }

            var typeArgument = name.Identifier.ValueText == "AddDbContext"
                ? name.TypeArgumentList.Arguments.Last()
                : name.TypeArgumentList.Arguments.First();
            if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(typeArgument, cancellationToken).Type, contextType))
            {
                continue;
            }

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                return Warn($"Cannot resolve registration '{invocation}' for '{contextName}'.");
            }

            if (method.ContainingType.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions")
            {
                registrations.Add(invocation);
            }
        }
        var factories = registrations.Where(invocation =>
            semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol
            { Name: "AddDbContextFactory" or "AddPooledDbContextFactory" }).ToArray();
        foreach (var registration in registrations)
        {
            var statement = registration.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            var parent = statement?.Parent is GlobalStatementSyntax wrapper ? wrapper.Parent : statement?.Parent;
            if (parent != container || statement is not ExpressionStatementSyntax ||
                registration.Ancestors().TakeWhile(node => node != statement).Any(node =>
                    node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or ConditionalExpressionSyntax or
                        ConditionalAccessExpressionSyntax or SwitchExpressionSyntax ||
                    node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression) ||
                    node.IsKind(SyntaxKind.CoalesceExpression) || node.IsKind(SyntaxKind.CoalesceAssignmentExpression)) ||
                registration.SpanStart >= build.SpanStart)
            {
                return Warn($"Registration for '{contextName}' is not an unconditional startup statement before {builderName}.Build().");
            }

            var services = GetServicesExpression(registration, semanticModel, cancellationToken);
            if (services is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax receiver,
                    Name.Identifier.ValueText: "Services"
                } || !SymbolEqualityComparer.Default.Equals(
                    semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol,
                    semanticModel.GetDeclaredSymbol(builder, cancellationToken)))
            {
                return Warn($"Cannot verify that registration '{registration}' uses {builderName}.Services.");
            }
        }

        if (factories.Length != 0)
        {
            return document;
        }

        // Existing AddDbContext options can depend on scoped services. Reuse those options
        // with a scoped factory instead of changing the application's context registration.
        List<StatementSyntax> additions = [];
        var arguments = "lifetime: global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";
        if (registrations.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(useDbMethod) ||
                SyntaxFactory.ParseExpression(useDbMethod) is not InvocationExpressionSyntax providerCall ||
                providerCall.ContainsDiagnostics || providerCall.ArgumentList.Arguments.Count == 0)
            {
                return Warn("The selected database provider did not produce an options invocation.");
            }

            providerCall = providerCall.ReplaceNodes(providerCall.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(identifier => identifier.Identifier.ValueText == "builder"),
                (identifier, _) => SyntaxFactory.IdentifierName(builderName).WithTriviaFrom(identifier));
            var baseName = $"{char.ToLowerInvariant(contextType.Name[0])}{contextType.Name[1..]}ConnectionString";
            var connectionName = baseName;
            var suffix = 1;
            var identifiers = root.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                .Select(token => token.ValueText).ToHashSet(StringComparer.Ordinal);
            while (identifiers.Contains(connectionName))
            {
                connectionName = baseName + suffix++;
            }

            var connectionArgument = providerCall.ArgumentList.Arguments.First();
            additions.Add(SyntaxFactory.ParseStatement($"var {connectionName} = {connectionArgument.Expression};"));
            providerCall = providerCall.ReplaceNode(connectionArgument.Expression, SyntaxFactory.IdentifierName(connectionName));
            arguments = $"options => options.{providerCall}";
        }
        var factory = SyntaxFactory.ParseStatement(
            $"{builderName}.Services.AddDbContextFactory<global::{contextName}>({arguments});");
        additions.Add(factory);
        additions = additions.Select(statement => statement.NormalizeWhitespace()
            .WithLeadingTrivia(buildStatement.GetLeadingTrivia())
            .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine))).ToList();
        root = buildStatement.Parent is GlobalStatementSyntax globalStatement
            ? root.InsertNodesBefore(globalStatement, additions.Select(SyntaxFactory.GlobalStatement))
            : root.InsertNodesBefore(buildStatement, additions);
        var updated = document.WithSyntaxRoot(root);
        var updatedRoot = await updated.GetSyntaxRootAsync(cancellationToken);
        var updatedModel = await updated.GetSemanticModelAsync(cancellationToken);
        if (updatedRoot is null || updatedModel is null ||
            !updatedRoot.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                updatedModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method &&
                method.Name == "AddDbContextFactory" &&
                method.ContainingType.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions" &&
                method.TypeArguments[0].ToDisplayString() == contextType.ToDisplayString()))
        {
            return Warn($"The generated IDbContextFactory<{contextName}> registration could not be resolved.");
        }

        return updated;
    }

    private static ExpressionSyntax? GetServicesExpression(
        InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var method = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol!;
        var services = method.ReducedFrom is not null && invocation.Expression is MemberAccessExpressionSyntax member
            ? member.Expression
            : (invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
                argument.NameColon?.Name.Identifier.ValueText == method.Parameters[0].Name) ??
                invocation.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon is null))?.Expression;
        while (services is ParenthesizedExpressionSyntax parentheses)
        {
            services = parentheses.Expression;
        }

        return services is InvocationExpressionSyntax chained &&
            semanticModel.GetSymbolInfo(chained, cancellationToken).Symbol is IMethodSymbol chainedMethod &&
            chainedMethod.ContainingType.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions"
                ? GetServicesExpression(chained, semanticModel, cancellationToken)
                : services;
    }
}
