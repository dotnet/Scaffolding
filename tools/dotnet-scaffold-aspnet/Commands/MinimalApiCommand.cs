// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    public class MinimalApiCommand : Command<MinimalApiCommand.MinimalApiSettings>
    {
        public override int Execute(CommandContext context, MinimalApiSettings settings)
        {
            AnsiConsole.MarkupLine("[green]Executing minimalapi command...[/]");
            // Your logic for minimalapi command here
            return 0;
        }

        public class MinimalApiSettings : CommandSettings
        {
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

            [CommandOption("--namespace <NAMESPACE>")]
            public string EndpointsNamespace { get; set; } = default!;

            [CommandOption("--dbProvider <DBPROVIDER>")]
            public string DatabaseProvider { get; set; } = default!;
        }
    }
}
