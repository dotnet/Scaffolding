// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;

internal class MinimalApiModel
{
    //Endpoints class info
    public string? EndpointsClassName { get; set; }
    public string EndpointsFileName { get; set; } = default!;
    public string? EndpointsPath { get; set; }
    public string? EndpointsNamespace { get; set; }
    public string? EndpointsMethodName { get; set; }

    //DbContext info
    public bool CreateDbContext { get; set; } = false;
    public string? DbContextClassName { get; set; }
    public string? DbContextClassPath { get; set; }
    public string? DbContextNamespace { get; set; }
    public string? DatabaseProvider  { get; set; }
    public string? PrimaryKeyName { get; set; }
    public string? PrimaryKeyShortTypeName { get; set; }
    public string? PrimaryKeyTypeName { get; set; }
    public bool EfScenario { get; set; } = false;
    public string? EntitySetVariableName { get; set; }

    //Model class info
    public List<string>? ModelProperties { get; set; }
    public string? ModelNamespace { get; set; }
    public string ModelTypeName { get; set; } = default!;
    public string ModelTypePluralName => $"{ModelTypeName}s";
    public string ModelVariable => ModelTypeName.ToLowerInvariant();

    //Project info
    public IAppSettings? AppSettings { get; set; }
    public ICodeService? CodeService { get; set; }
    public CodeChangeOptions? CodeChangeOptions { get; set; }
    public bool OpenAPI { get; set; }
    public bool UseTypedResults { get; set; } = true;
    public string NewDbSetStatement => $"public DbSet<{ModelTypeName}> {ModelTypeName} {{ get; set; }} = default!;";
}
