// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// IFlowStep that deals with the selection of a built-in scaffolding command.
    /// </summary>
    internal class CommandPickerFlowStep : IFlowStep
    {
        private readonly ILogger _logger;
        private readonly ScaffolderCatalog _scaffolderCatalog;
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPickerFlowStep"/> class.
        /// </summary>
        public CommandPickerFlowStep(
            ILogger logger,
            ScaffolderCatalog scaffolderCatalog,
            IEnvironmentService environmentService,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _scaffolderCatalog = scaffolderCatalog;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public string Id => nameof(CommandPickerFlowStep);
        /// <inheritdoc/>
        public string DisplayName => "Command Name";

        /// <inheritdoc/>
        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.ComponentName);
            context.Unset(FlowContextProperties.ComponentObj);
            context.Unset(FlowContextProperties.CommandName);
            context.Unset(FlowContextProperties.CommandObj);
            return new ValueTask();
        }

        /// <inheritdoc/>
        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            // KeyValuePair with key being name of the DotNetToolInfo (component) and value being the CommandInfo supported by that component.
            KeyValuePair<string, CommandInfo>? commandInfoKvp = null;
            CommandInfo? commandInfo = null;
            CommandDiscovery commandDiscovery = new(_scaffolderCatalog.GetCommands(), componentPicked: null);
            commandInfoKvp = commandDiscovery.Discover(context);
            if (commandDiscovery.State.IsNavigation())
            {
                return new ValueTask<FlowStepResult>(new FlowStepResult { State = commandDiscovery.State });
            }

            if (commandInfoKvp is null || !commandInfoKvp.HasValue || commandInfoKvp.Value.Value is null || string.IsNullOrEmpty(commandInfoKvp.Value.Key))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Unable to find any commands."));
            }
            else
            {
                commandInfo = commandInfoKvp.Value.Value;
                var scaffolderComponent = _scaffolderCatalog.FindComponent(commandInfoKvp.Value.Key);
                if (scaffolderComponent is null)
                {
                    return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Unable to find component '{commandInfoKvp.Value.Key}'."));
                }

                SelectComponent(context, scaffolderComponent.Component);
                SelectCommand(context, commandInfo);
            }

            var commandFirstStep = GetFirstParameterBasedStep(commandInfo);
            if (commandFirstStep is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Failed to get/parse parameters for command '{commandInfo.Name}'"));
            }

            return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { commandFirstStep } });
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

            var commandFirstStep = GetFirstParameterBasedStep(commandInfo);
            if (commandFirstStep is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Failed to get/parse parameters for command '{commandInfo.Name}'"));
            }

            SelectComponent(context, scaffolderComponent.Component);
            SelectCommand(context, commandInfo);
            return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { commandFirstStep } });
        }

        /// <summary>
        /// Wrapper to get the first ParameterBasedFlowStep. Uses 'BuildParameterFlowSteps'.
        /// </summary>
        internal ParameterBasedFlowStep? GetFirstParameterBasedStep(CommandInfo commandInfo)
        {
            ParameterBasedFlowStep? firstParameterStep = null;
            if (commandInfo.Parameters != null && commandInfo.Parameters.Length != 0)
            {
                firstParameterStep = BuildParameterFlowSteps([.. commandInfo.Parameters]);
            }

            return firstParameterStep;
        }

        /// <summary>
        /// Takes all the 'Parameter's, creates ParameterBasedFlowSteps, and connects them using 'NextStep'.
        /// </summary>
        /// <returns>The first step from the connected ParameterBasedFlowSteps.</returns>
        internal ParameterBasedFlowStep? BuildParameterFlowSteps(List<Parameter> parameters)
        {
            ParameterBasedFlowStep? firstStep = null;
            ParameterBasedFlowStep? previousStep = null;

            foreach (var parameter in parameters)
            {
                var step = new ParameterBasedFlowStep(
                    parameter,
                    null,
                    _environmentService,
                    _fileSystem,
                    _logger);
                if (firstStep == null)
                {
                    // This is the first step
                    firstStep = step;
                }
                else
                {
                    if (previousStep != null)
                    {
                        // Connect the previous step to this step
                        previousStep.NextStep = step;
                    }
                }

                previousStep = step;
            }

            return firstStep;
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
