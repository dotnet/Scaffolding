// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;
using Spectre.Console;
using System.IO;
using System;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    public class AreaCommand : Command<AreaCommand.AreaSettings>
    {
        public override int Execute(CommandContext context, AreaSettings settings)
        {
            AnsiConsole.MarkupLine("[green]Executing area command...[/]");
            if (string.IsNullOrEmpty(settings.Name))
            {
                AnsiConsole.WriteException(new ArgumentNullException("--name parameter required for area"));
            }
            else
            {
                EnsureFolderLayout(settings.Name);
                return 0;
            }

            return -1;
        }

        private void EnsureFolderLayout(string areaName)
        {
            var areaBasePath = Path.Combine("blehTODO", "Areas");
            if (!Directory.Exists(areaBasePath))
            {
                Directory.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, areaName);
            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            foreach (var areaFolder in AreaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private static readonly string[] AreaFolders =
        [
            "Controllers",
            "Models",
            "Data",
            "Views"
        ];

        public class AreaSettings : ProjectScaffolderSettings
        {
            [CommandOption("--name <NAME>")]
            public string Name { get; set; } = default!;
        }
    }
}
