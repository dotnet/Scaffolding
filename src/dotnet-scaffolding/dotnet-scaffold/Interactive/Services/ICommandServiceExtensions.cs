// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Services;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Services
{
    internal static class ICommandServiceExtensions
    {
        public static IList<KeyValuePair<string, CommandInfo>> GetAllCommandsWithId(List<ICommandService> commandServices, IDotNetToolService dotNetToolService, IDictionary<string, string>? envVars)
        {
            IList<KeyValuePair<string, CommandInfo>> commandsFromServices = [];
            if (commandServices is not null)
            {
                foreach (ICommandService commandService in commandServices)
                {
                    string key = commandService.CommandId;
                    List<CommandInfo> commandInfos = commandService.CommandInfos;
                    commandsFromServices = [.. commandsFromServices, .. commandInfos.Select(ci => new KeyValuePair<string, CommandInfo>(key, ci))];
                }
            }

            IList<KeyValuePair<string, CommandInfo>> commandsFromTools = dotNetToolService.GetAllCommandsParallel(envVars: envVars);

            return [.. commandsFromServices, .. commandsFromTools];
        }

        public static IList<CommandInfo> GetAllCommands(List<ICommandService> commandServices, IDotNetToolService dotNetToolService, IDictionary<string, string>? envVars)
        {
            IList<KeyValuePair<string, CommandInfo>> commandsWithIds = GetAllCommandsWithId(commandServices, dotNetToolService, envVars);
            IList<CommandInfo> commandsWithoutId = commandsWithIds.Select(kvp => kvp.Value).ToList();
            return commandsWithoutId;
        }
    }
}
