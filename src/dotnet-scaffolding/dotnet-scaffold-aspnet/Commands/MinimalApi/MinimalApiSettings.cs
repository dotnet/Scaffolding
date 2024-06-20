// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;


internal class MinimalApiSettings : CommandSettings
{
    [CommandOption("--project <PROJECT>")]
    public required string Project { get; set; }

    [CommandOption("--model <MODEL>")]
    public required string Model { get; set; }

    [CommandOption("--endpoints <ENDPOINTS>")]
    public string? Endpoints { get; set; }

    [CommandOption("--dataContext <DATACONTEXT>")]
    public string? DataContext { get; set; }

    [CommandOption("--open")]
    public bool OpenApi { get; set; } = true;

    [CommandOption("--dbProvider <DBPROVIDER>")]
    public string? DatabaseProvider { get; set; }

    [CommandOption("--prerelease")]
    public required bool Prerelease { get; set; }
}
