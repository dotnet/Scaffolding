// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    public class RedisCommand : Command<RedisCommand.RedisCommandSettings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] RedisCommandSettings settings)
        {
            AnsiConsole.MarkupLine("[green]Executing redis command...[/]");
            AnsiConsole.MarkupLine("\nDONE!");
            return 0;;
        }

        public class RedisCommandSettings : CommandSettings
        {
            [CommandOption("--project <PROJECT>")]
            public string Project { get; set; } = default!;
        }
    }
}
