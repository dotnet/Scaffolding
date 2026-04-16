// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
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
        if (string.IsNullOrEmpty(dbContextPath) || !File.Exists(dbContextPath) || string.IsNullOrEmpty(dbSetStatement) || string.IsNullOrEmpty(ProjectPath))
        {
            return true;
        }

        string dbContextFileName = Path.GetFileName(dbContextPath);
        string configJson = BuildCodeModifierConfig(dbContextFileName, dbSetStatement);

        _logger.LogInformation($"Adding DbSet property to '{dbContextFileName}'...");

        CodeModificationStep codeModificationStep = new CodeModificationStep(NullLogger<CodeModificationStep>.Instance)
        {
            CodeModifierConfigJsonText = configJson,
            ProjectPath = ProjectPath,
            CodeChangeOptions = CodeChangeOptions
        };

        bool result = await codeModificationStep.ExecuteAsync(context, cancellationToken);
        if (!result)
        {
            _logger.LogError($"Failed to add '{dbSetStatement}' to '{dbContextFileName}'.");
        }

        return result;
    }

    /// <summary>
    /// Builds a <see cref="CodeModificationStep"/>-compatible JSON config that instructs
    /// to insert <paramref name="dbSetStatement"/> as a class property via
    /// <c>ClassProperties</c>. Duplicate detection is built into DocumentBuilder.
    /// </summary>
    internal static string BuildCodeModifierConfig(string dbContextFileName, string dbSetStatement)
    {
        object config = new
        {
            Files = new[]
            {
                new
                {
                    FileName = dbContextFileName,
                    ClassProperties = new[]
                    {
                        new { Block = dbSetStatement }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(config);
    }
}

