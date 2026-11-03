// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step that adds a missing DbSet property to an existing DbContext class.
/// This is needed when a pre-existing DbContext is selected for CRUD scaffolding but does
/// not yet contain a DbSet property for the model being scaffolded.
/// Delegates the actual code modification to <see cref="CodeModificationStep"/> using its
/// in-memory JSON config path, consistent with how all other code changes are applied in
/// this project.
/// </summary>
internal class AddDbSetToExistingContextStep : ScaffoldStep
{
    /// <summary>
    /// Path to the .csproj of the project being scaffolded. Required to open the Roslyn workspace.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Code change options forwarded to the underlying <see cref="CodeModificationStep"/>.
    /// Defaults to an empty list, which is correct when no option-filtered blocks are defined.
    /// </summary>
    public IList<string> CodeChangeOptions { get; set; } = [];

    private readonly ILogger _logger;

    public AddDbSetToExistingContextStep(ILogger<AddDbSetToExistingContextStep> logger)
    {
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(DbContextProperties), out object? dbContextPropertiesObj) ||
            dbContextPropertiesObj is not DbContextProperties dbContextProperties)
        {
            return true;
        }

        string? dbContextPath = dbContextProperties.DbContextPath;
        string? dbSetStatement = dbContextProperties.DbSetStatement;

        // Only act when:
        //   The target file already exists on disk (existing context — new contexts are
        //     handled by WithDbContextStep via its T4 template which already includes the DbSet).
        //   A DbSet statement was produced (only set when GetDbContextInfo found no existing DbSet).
        //   The project path is available to initialise the Roslyn workspace.
        if (string.IsNullOrEmpty(dbContextPath) || !File.Exists(dbContextPath) || string.IsNullOrWhiteSpace(dbSetStatement) || string.IsNullOrEmpty(ProjectPath))
        {
            return true;
        }

        dbSetStatement = NormalizeDbSetStatement(dbSetStatement);
        if (string.IsNullOrWhiteSpace(dbSetStatement))
        {
            return true;
        }

        using var codeService = new CodeService(_logger, ProjectPath);
        var solution = (await codeService.GetWorkspaceAsync())?.CurrentSolution;
        var documentId = solution?.GetDocumentIdsWithFilePath(dbContextPath).FirstOrDefault();
        var document = documentId is null ? null : solution?.GetDocument(documentId);
        string? contextProjectPath = document?.Project.FilePath;
        if (document is null || string.IsNullOrEmpty(contextProjectPath))
        {
            _logger.LogError("Unable to find the project owning DbContext '{DbContextPath}'.", dbContextPath);
            return false;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        var contextDeclaration = FindContextDeclaration(semanticModel, dbContextProperties.DbContextFullName);
        if (contextDeclaration is null || semanticModel?.GetDeclaredSymbol(contextDeclaration, cancellationToken) is not INamedTypeSymbol contextSymbol)
        {
            _logger.LogError("Unable to find DbContext '{DbContextFullName}' in '{DbContextPath}'.", dbContextProperties.DbContextFullName, dbContextPath);
            return false;
        }

        if (SyntaxFactory.ParseMemberDeclaration(dbSetStatement) is not PropertyDeclarationSyntax dbSetProperty ||
            dbSetProperty.ContainsDiagnostics ||
            dbSetProperty.Type is not GenericNameSyntax { Identifier.ValueText: "DbSet" } dbSetType ||
            dbSetType.TypeArgumentList.Arguments.Count != 1)
        {
            _logger.LogError("Invalid DbSet property declaration '{DbSetStatement}'.", dbSetStatement);
            return false;
        }

        var modelTypeSyntax = dbSetType.TypeArgumentList.Arguments[0];
        var modelType = semanticModel.GetSpeculativeTypeInfo(contextDeclaration.OpenBraceToken.Span.End,
            modelTypeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        if (modelType is not INamedTypeSymbol { TypeKind: not TypeKind.Error } ||
            !semanticModel.Compilation.IsSymbolAccessibleWithin(modelType, contextSymbol))
        {
            _logger.LogError("Model '{ModelType}' is not accessible to DbContext '{DbContextFullName}'. Place the model in the context project or a shared project referenced by it.",
                modelTypeSyntax.ToString(), dbContextProperties.DbContextFullName);
            return false;
        }

        if (DbSetAlreadyPresent(contextDeclaration, dbSetProperty))
        {
            return true;
        }

        string dbContextFileName = Path.GetFileName(dbContextPath);
        string configJson = BuildCodeModifierConfig(dbContextPath, dbSetStatement, dbContextProperties.DbContextFullName);

        _logger.LogInformation($"Adding DbSet property to '{dbContextFileName}'...");

        CodeModificationStep codeModificationStep = new CodeModificationStep(NullLogger<CodeModificationStep>.Instance)
        {
            CodeModifierConfigJsonText = configJson,
            ProjectPath = contextProjectPath,
            CodeChangeOptions = CodeChangeOptions
        };

        bool result = await codeModificationStep.ExecuteAsync(context, cancellationToken);
        if (!result)
        {
            _logger.LogError($"Failed to add '{dbSetStatement}' to '{dbContextFileName}'.");
            return false;
        }

        string fileContent = await File.ReadAllTextAsync(dbContextPath, cancellationToken);
        var savedDocument = document.WithText(SourceText.From(fileContent));
        var savedSemanticModel = await savedDocument.GetSemanticModelAsync(cancellationToken);
        var savedContext = FindContextDeclaration(savedSemanticModel, dbContextProperties.DbContextFullName);
        if (savedContext is null || !DbSetAlreadyPresent(savedContext, dbSetProperty))
        {
            _logger.LogError("DbSet property was not saved to DbContext '{DbContextFullName}' in '{DbContextPath}'.", dbContextProperties.DbContextFullName, dbContextPath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a <see cref="CodeModificationStep"/>-compatible JSON config that instructs
    /// to insert <paramref name="dbSetStatement"/> as a class property via
    /// <c>ClassProperties</c>. Duplicate detection is built into DocumentBuilder.
    /// </summary>
    /// <param name="dbContextFilePath">The context's full source path, used to select the exact document.</param>
    /// <param name="dbSetStatement">The property declaration to insert.</param>
    /// <param name="dbContextFullName">The exact selected context type identity.</param>
    internal static string BuildCodeModifierConfig(string dbContextFilePath, string dbSetStatement, string? dbContextFullName)
    {
        dbSetStatement = NormalizeDbSetStatement(dbSetStatement);

        object config = new
        {
            Files = new[]
            {
                new
                {
                    FileName = dbContextFilePath,
                    ClassFullName = dbContextFullName,
                    ClassProperties = new[]
                    {
                        new { Block = dbSetStatement }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(config);
    }

    internal static string NormalizeDbSetStatement(string dbSetStatement)
    {
        if (string.IsNullOrWhiteSpace(dbSetStatement))
        {
            return string.Empty;
        }

        string normalized = dbSetStatement.Trim();
        normalized = normalized.TrimEnd(';').TrimEnd();
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"{normalized};";
    }

    /// <summary>
    /// Checks only properties directly owned by the selected context, not comments or other classes.
    /// </summary>
    internal static bool DbSetAlreadyPresent(ClassDeclarationSyntax contextDeclaration, PropertyDeclarationSyntax dbSetProperty)
    {
        return dbSetProperty.Type is GenericNameSyntax { Identifier.ValueText: "DbSet" } &&
            contextDeclaration.Members.OfType<PropertyDeclarationSyntax>().Any(property =>
                property.Identifier.ValueText == dbSetProperty.Identifier.ValueText &&
                SyntaxFactory.AreEquivalent(property.Type, dbSetProperty.Type));
    }

    private static ClassDeclarationSyntax? FindContextDeclaration(SemanticModel? semanticModel, string? fullName)
    {
        return string.IsNullOrEmpty(fullName) ? null : semanticModel?.SyntaxTree.GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(node => semanticModel.GetDeclaredSymbol(node)?.ToDisplayString() == fullName);
    }
}

