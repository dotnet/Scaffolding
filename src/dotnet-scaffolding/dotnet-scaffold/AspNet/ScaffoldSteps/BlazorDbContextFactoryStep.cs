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
            var updated = await RegisterFactoryAsync(document, contextName, properties["$(UseDbMethod)"], cancellationToken);
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
        Document document, string contextName, string useDbMethod, CancellationToken cancellationToken = default)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax
            ?? throw new InvalidOperationException("Program.cs is not a C# compilation unit.");
        if (!root.Usings.Any(usingDirective => usingDirective.Name?.ToString() == "Microsoft.EntityFrameworkCore"))
        {
            root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore"))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)));
            document = document.WithSyntaxRoot(root);
        }

        root = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken))!;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            ?? throw new InvalidOperationException("Cannot analyze Program.cs.");
        var contextType = semanticModel.Compilation.GetTypeByMetadataName(contextName)
            ?? throw new InvalidOperationException($"Cannot resolve the selected DbContext '{contextName}'.");
        var builder = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().SingleOrDefault(variable =>
            variable.Initializer?.Value is InvocationExpressionSyntax invocation &&
            semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method &&
            method.Name == "CreateBuilder" &&
            method.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Builder.WebApplication")
            ?? throw new InvalidOperationException("Expected one WebApplication.CreateBuilder declaration in Program.cs.");
        var builderName = builder.Identifier.ValueText;
        var build = root.DescendantNodes().OfType<InvocationExpressionSyntax>().SingleOrDefault(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax receiver,
                Name.Identifier.ValueText: "Build"
            } && receiver.Identifier.ValueText == builderName)
            ?? throw new InvalidOperationException("Cannot locate the selected WebApplication builder's Build call.");
        var buildStatement = build.Ancestors().OfType<StatementSyntax>().First();
        var container = buildStatement.Parent is GlobalStatementSyntax global ? global.Parent : buildStatement.Parent;
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
                throw new InvalidOperationException($"Cannot resolve registration '{invocation}' for '{contextName}'.");
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
            var statement = registration.Ancestors().OfType<StatementSyntax>().First();
            var parent = statement.Parent is GlobalStatementSyntax wrapper ? wrapper.Parent : statement.Parent;
            if (parent != container || statement is not ExpressionStatementSyntax ||
                registration.Ancestors().Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax) ||
                registration.SpanStart >= build.SpanStart)
            {
                throw new InvalidOperationException($"Registration for '{contextName}' must be an unconditional startup statement before {builderName}.Build().");
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
                throw new InvalidOperationException($"Cannot verify that registration '{registration}' uses {builderName}.Services.");
            }
        }

        if (factories.Length != 0)
        {
            return document;
        }

        // Existing AddDbContext options can depend on scoped services. Reuse those options
        // with a scoped factory instead of changing the application's context registration.
        var providerCall = SyntaxFactory.ParseExpression(useDbMethod);
        providerCall = providerCall.ReplaceNodes(providerCall.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(identifier => identifier.Identifier.ValueText == "builder"),
            (identifier, _) => SyntaxFactory.IdentifierName(builderName).WithTriviaFrom(identifier));
        var arguments = registrations.Count != 0
            ? "lifetime: global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped"
            : $"options => options.{providerCall}";
        var factory = SyntaxFactory.ParseStatement(
            $"{builderName}.Services.AddDbContextFactory<global::{contextName}>({arguments});");
        factory = factory.NormalizeWhitespace()
            .WithLeadingTrivia(buildStatement.GetLeadingTrivia())
            .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));
        root = buildStatement.Parent is GlobalStatementSyntax globalStatement
            ? root.InsertNodesBefore(globalStatement, [SyntaxFactory.GlobalStatement(factory)])
            : root.InsertNodesBefore(buildStatement, [factory]);
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
            throw new InvalidOperationException($"The generated IDbContextFactory<{contextName}> registration could not be resolved.");
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
