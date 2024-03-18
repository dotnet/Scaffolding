// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    public class GetCmdsCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            var commands = new List<CommandInfo>
            {
                new CommandInfo
                {
                    Name = "minimalapi",
                    DisplayName = "Minimal API",
                    Description = "Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.",
                    Parameters = GetCmdsHelper.MinimalApiParameters // Add parameters if any
                },
                new CommandInfo
                {
                    Name = "area",
                    DisplayName = "Area",
                    Description = "Generates an Area folder structure.",
                    Parameters = GetCmdsHelper.AreaParameters // Add parameters if any
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
        internal static Parameter ModelName = new()  { Name = "--model", DisplayName = "Model Name", Description = "Name for the model class to be used for scaffolding", Required = true, Type = BaseTypes.String };
        internal static Parameter EndpointsClass = new()  { Name = "--endpoints", DisplayName = "Endpoints Class Name", Description = "", Required = false, Type = BaseTypes.String };
        internal static Parameter DataContextClass = new() { Name = "--dataContext", DisplayName = "Data Context Class", Description = "", Required = true, Type = BaseTypes.String };
        internal static Parameter RelativeFolderPath = new() { Name = "--relativeFolderPath", DisplayName = "Relative Folder Path", Description = "The relative folder path where scaffolded files will be added", Required = false, Type = BaseTypes.String };
        internal static Parameter OpenApi = new() { Name = "--open", DisplayName = "Open API Enabled", Description = "", Required = false, Type = BaseTypes.Bool };
        internal static Parameter DatabaseProvider = new() { Name = "--dbProvider", DisplayName = "Database Provider", Description = "", Required = true, Type = BaseTypes.String };
        internal static Parameter AreaName = new() { Name = "--name", DisplayName = "Area Name", Description = "Name for the area being created", Required = true, Type = BaseTypes.String };
        internal static Parameter[] AreaParameters = [AreaName];
        internal static Parameter[] MinimalApiParameters = [ModelName, EndpointsClass, DataContextClass, OpenApi, DatabaseProvider, AreaName];
    }
}
