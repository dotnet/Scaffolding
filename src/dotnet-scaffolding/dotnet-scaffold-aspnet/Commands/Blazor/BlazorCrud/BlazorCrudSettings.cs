// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

internal class BlazorCrudSettings : CommandSettings
{
    [CommandOption("--project <PROJECT>")]
    public required string Project { get; init; }

    [CommandOption("--model <MODEL>")]
    public required string Model { get; init; }

    [CommandOption("--dataContext <DATACONTEXT>")]
    public required string DataContext { get; init; }

    [CommandOption("--dbProvider <DBPROVIDER>")]
    public required string DatabaseProvider { get; set; }

    [CommandOption("--prerelease")]
    public required bool Prerelease { get; init; }

    [CommandOption("--page")]
    public required string Page { get; set; }
}
