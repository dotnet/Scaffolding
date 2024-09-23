// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    /// <summary>
    /// IFlowStep that deals with the selection of the component(DotnetToolInfo) and the associated command(CommandInfo).
    /// if provided by the user, verify if the component is installed and the command is supported.
    /// </summary>
    internal class CategoryPickerFlowStep : IFlowStep
    {
        private readonly ILogger _logger;
        private readonly IDotNetToolService _dotnetToolService;
        public CategoryPickerFlowStep(ILogger logger, IDotNetToolService dotnetToolService)
        {
            _logger = logger;
            _dotnetToolService = dotnetToolService;
        }

        public string Id => nameof(CategoryPickerFlowStep);

        public string DisplayName => "Scaffolding Category";

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.ScaffoldingCategory);
            context.Unset(FlowContextProperties.ComponentName);
            context.Unset(FlowContextProperties.ComponentObj);
            context.Unset(FlowContextProperties.CommandName);
            context.Unset(FlowContextProperties.CommandObj); 
            return new ValueTask();
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var settings = context.GetCommandSettings();
            var componentName = settings?.ComponentName;
            var commandName = settings?.CommandName;
            string? displayCategory = null;
            var dotnetTools = _dotnetToolService.GetDotNetTools();
            var dotnetToolComponent = dotnetTools.FirstOrDefault(x => x.Command.Equals(componentName, StringComparison.OrdinalIgnoreCase));

            CategoryDiscovery categoryDiscovery = new(_dotnetToolService, dotnetToolComponent);
            displayCategory = categoryDiscovery.Discover(context);
            if (categoryDiscovery.State.IsNavigation())
            {
                return new ValueTask<FlowStepResult>(new FlowStepResult { State = categoryDiscovery.State });
            }

            if (string.IsNullOrEmpty(displayCategory))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Unable to find any component categories!"));
            }
            else
            {
                SelectCategory(context, displayCategory);
            }

            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var settings = context.GetCommandSettings();
            var componentName = settings?.ComponentName;
            var commandName = settings?.CommandName;
            CommandInfo? commandInfo = null;

            //check if user input included a component name.
            //if included, check for a command name, and get the CommandInfo object.
            var dotnetTools = _dotnetToolService.GetDotNetTools();
            var dotnetToolComponent = dotnetTools.FirstOrDefault(x => x.Command.Equals(componentName, StringComparison.OrdinalIgnoreCase));
            if (dotnetToolComponent != null)
            {
                var allCommands = _dotnetToolService.GetCommands(dotnetToolComponent);
                commandInfo = allCommands.FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("No component (dotnet tool) provided!"));
            }

            if (commandInfo is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Invalid or empty command provided for component '{componentName}'"));
            }

            SelectComponent(context, dotnetToolComponent);
            SelectCommand(context, commandInfo);
            SelectCategory(context, commandInfo.DisplayCategory);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        private void SelectCategory(IFlowContext context, string categoryName)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.ScaffoldingCategory,
                categoryName,
                "Scaffolding Category",
                isVisible: true));
        }

        private void SelectCommand(IFlowContext context, CommandInfo command)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.CommandName,
                command.Name,
                "Command Name",
                isVisible: true));

            context.Set(new FlowProperty(
                FlowContextProperties.CommandObj,
                command,
                isVisible: false));
        }

        private void SelectComponent(IFlowContext context, DotNetToolInfo dotnetToolInfo)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.ComponentName,
                dotnetToolInfo.Command,
                "Component Name",
                isVisible: true));

            context.Set(new FlowProperty(
                FlowContextProperties.ComponentObj,
                dotnetToolInfo,
                isVisible: false));
        }
    }
}
