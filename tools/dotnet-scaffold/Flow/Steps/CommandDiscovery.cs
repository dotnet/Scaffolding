// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class CommandDiscovery
    {
        private readonly IDotNetToolService _dotnetToolService;

        public CommandDiscovery(IDotNetToolService dotNetToolService)
        {
            _dotnetToolService = dotNetToolService;
        }

        public FlowStepState State { get; private set; }

        public CommandInfo? Discover(IFlowContext context, string componentName)
        {
            return Prompt(context, "Pick a scaffolding command (from chosen component)", componentName);
        }

        private CommandInfo? Prompt(IFlowContext context, string title, string componentName)
        {
            var commands = _dotnetToolService.GetCommands(componentName); 
            if (commands.Count == 0)
            {
                return null;
            }

            if (commands.Count == 1)
            {
                return commands[0];
            }

            var prompt = new FlowSelectionPrompt<CommandInfo>()
                .Title(title)
                .Converter(GetCommandInfoDisplayName)
                .AddChoices(commands, navigation: context.Navigation);

            var result = prompt.Show();
            State = result.State;
            return result.Value;
        }

        internal string GetCommandInfoDisplayName(CommandInfo commandInfo)
        {
            return commandInfo.DisplayName;
        }
    }
}
