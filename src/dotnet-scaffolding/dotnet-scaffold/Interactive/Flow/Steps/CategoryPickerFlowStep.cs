// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// IFlowStep that deals with the selection of a scaffolding category.
    /// </summary>
    internal class CategoryPickerFlowStep : IFlowStep
    {
        private readonly ScaffolderCatalog _scaffolderCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryPickerFlowStep"/> class.
        /// </summary>
        public CategoryPickerFlowStep(ScaffolderCatalog scaffolderCatalog)
        {
            _scaffolderCatalog = scaffolderCatalog;
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
            CategoryDiscovery categoryDiscovery = new(_scaffolderCatalog.GetCommands(), componentPicked: null);
            string? displayCategory = categoryDiscovery.Discover(context);
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
            var componentName = settings?.ComponentName;
            var commandName = settings?.CommandName;
            CommandInfo? commandInfo = null;

            var scaffolderComponent = _scaffolderCatalog.FindComponent(componentName);
            if (scaffolderComponent is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("No component (dotnet tool) provided."));
            }

            commandInfo = scaffolderComponent.Commands.FirstOrDefault(
                command => command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (commandInfo is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Invalid or empty command provided for component '{componentName}'"));
            }

            SelectComponent(context, scaffolderComponent.Component);
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
