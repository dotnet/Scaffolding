// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class IdentityDatabaseGuidanceStep(ILogger<IdentityDatabaseGuidanceStep> logger) : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public required string DbContextName { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ProjectPath) || string.IsNullOrEmpty(DbContextName))
        {
            logger.LogError("Unable to provide Identity database guidance because the project or DbContext is missing.");
            return Task.FromResult(false);
        }

        logger.LogInformation("Identity scaffolding does not create migrations or update the database.");
        logger.LogInformation("Review your EF Core model changes. If a migration is needed, choose a migration name and use compatible dotnet-ef tooling. For example:");
        logger.LogInformation("dotnet ef migrations add AddIdentity --project \"{ProjectPath}\" --context \"{DbContextName}\"", ProjectPath, DbContextName);
        logger.LogInformation("Apply migrations explicitly using your normal database deployment workflow.");
        return Task.FromResult(true);
    }
}
