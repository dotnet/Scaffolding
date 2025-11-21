// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// IFlowStep that deals with the selection of the component (DotNetToolInfo) and the associated command (CommandInfo).
    /// If provided by the user, verifies if the component is installed and the command is supported.
    /// </summary>
    internal class CategoryPickerFlowStep : IFlowStep
    {
        private readonly ILogger _logger;
        private readonly IDotNetToolService _dotnetToolService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryPickerFlowStep"/> class.
        /// </summary>
        public CategoryPickerFlowStep(ILogger logger, IDotNetToolService dotnetToolService)
        {
            _logger = logger;
            _dotnetToolService = dotnetToolService;
        }

        /// <inheritdoc/>
        public string Id => nameof(CategoryPickerFlowStep);
        /// <inheritdoc/>
        public string DisplayName => "Scaffolding Category";

        /// <inheritdoc/>
        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.ScaffoldingCategories);
            context.Unset(FlowContextProperties.ChosenCategory);
            context.Unset(FlowContextProperties.ComponentName);
            context.Unset(FlowContextProperties.ComponentObj);
            context.Unset(FlowContextProperties.CommandName);
            context.Unset(FlowContextProperties.CommandObj); 
            return new ValueTask();
        }

        /// <inheritdoc/>
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
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Unable to find any component categories."));
            }
            else
            {
                SelectChosenCategory(context, displayCategory);
            }

            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        /// <inheritdoc/>
        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var settings = context.GetCommandSettings();
            var envVars = context.GetTelemetryEnvironmentVariables();
            var componentName = settings?.ComponentName;
            var commandName = settings?.CommandName;
            CommandInfo? commandInfo = null;

            // Check if user input included a component name.
            // If included, check for a command name, and get the CommandInfo object.
            var dotnetTools = _dotnetToolService.GetDotNetTools();
            var dotnetToolComponent = dotnetTools.FirstOrDefault(x => x.Command.Equals(componentName, StringComparison.OrdinalIgnoreCase));
            if (dotnetToolComponent != null)
            {
                var allCommands = _dotnetToolService.GetCommands(dotnetToolComponent, envVars);
                commandInfo = allCommands.FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("No component (dotnet tool) provided."));
            }

            if (commandInfo is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Invalid or empty command provided for component '{componentName}'"));
            }

            SelectComponent(context, dotnetToolComponent);
            SelectCommand(context, commandInfo);
            SelectCategories(context, commandInfo.DisplayCategories);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        /// <summary>
        /// Sets the available categories in the flow context.
        /// </summary>
        private void SelectCategories(IFlowContext context, List<string> categories)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.ScaffoldingCategories,
                categories,
                "Scaffolding Categories",
                isVisible: false));
        }

        /// <summary>
        /// Sets the chosen category in the flow context.
        /// </summary>
        private void SelectChosenCategory(IFlowContext context, string category)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.ChosenCategory,
                category,
                "Scaffolding Category",
                isVisible: true));
        }

        /// <summary>
        /// Sets the selected command in the flow context.
        /// </summary>
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

        /// <summary>
        /// Sets the selected component in the flow context.
        /// </summary>
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
