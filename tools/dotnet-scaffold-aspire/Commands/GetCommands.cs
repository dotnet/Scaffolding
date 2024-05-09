// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    public class GetCmdsCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            var commands = new List<CommandInfo>
            {
                new CommandInfo
                {
                    Name = "redis",
                    DisplayName = "Redis",
                    DisplayCategory = "Aspire",
                    Description = "Modifies Aspire project to make it redis ready",
                    Parameters = GetCmdsHelper.RedisParameters
                }
                // Add other commands here
            };

            var json = System.Text.Json.JsonSerializer.Serialize(commands);
            AnsiConsole.WriteLine(json);
            return 0;
        }
    }

    public static class GetCmdsHelper
    {
        internal static Parameter ProjectParameter = new() { Name = "--project", DisplayName = "Project Name", Description = "Project of choice for the scaffolding", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.ProjectPicker };
        internal static Parameter[] RedisParameters = [ProjectParameter];
    }
}
