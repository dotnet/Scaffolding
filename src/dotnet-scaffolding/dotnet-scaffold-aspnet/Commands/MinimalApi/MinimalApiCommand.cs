// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;

internal class MinimalApiCommand : Command<MinimalApiCommand.MinimalApiSettings>
{
    private readonly IAppSettings _appSettings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostService _hostService;
    private readonly ICodeService _codeService;
    private List<string> _excludeList;

    public MinimalApiCommand(
        IAppSettings appSettings,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IHostService hostService,
        ICodeService codeService,
        ILogger logger)
    {
        _appSettings = appSettings;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _hostService = hostService;
        _logger = logger;
        _codeService = codeService;
        _excludeList = [];
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] MinimalApiSettings settings)
    {
        AnsiConsole.MarkupLine("[green]Executing minimalapi command...[/]");
        //setup project settings
        _appSettings.AddSettings("workspace", new WorkspaceSettings());
        _appSettings.Workspace().InputPath = settings.Project;
        AnsiConsole.MarkupLine("\n[green]DONE![/]");
        return 0;
    }

    public class MinimalApiSettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string Project { get; set; } = default!;

        [CommandOption("--model <MODEL>")]
        public string Model { get; set; } = default!;

        [CommandOption("--endpoints <ENDPOINTS>")]
        public string Endpoints { get; set; } = default!;

        [CommandOption("--dataContext <DATACONTEXT>")]
        public string DataContext { get; set; } = default!;

        [CommandOption("--relativeFolderPath <FOLDERPATH>")]
        public string RelativeFolderPath { get; set; } = default!;

        [CommandOption("--open")]
        public bool Open { get; set; }

        [CommandOption("--dbProvider <DBPROVIDER>")]
        public string DatabaseProvider { get; set; } = default!;
    }
}
