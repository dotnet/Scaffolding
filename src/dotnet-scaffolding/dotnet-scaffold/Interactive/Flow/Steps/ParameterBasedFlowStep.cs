// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.Extensions.Logging;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// Represents a flow step that handles user input for a specific parameter, including validation and navigation.
    /// Supports chaining to the next parameter step and conditional skipping.
    /// </summary>
    internal class ParameterBasedFlowStep : IFlowStep
    {
        /// <summary>
        /// The parameter associated with this step.
        /// </summary>
        public Parameter Parameter { get; set; }
        /// <summary>
        /// The next step in the parameter flow, if any.
        /// </summary>
        public ParameterBasedFlowStep? NextStep { get; set; }
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterBasedFlowStep"/> class.
        /// </summary>
        public ParameterBasedFlowStep(
            Parameter parameter,
            ParameterBasedFlowStep? nextStep,
            IEnvironmentService environmentService,
            IFileSystem fileSystem,
            ILogger logger)
        {
            Parameter = parameter;
            NextStep = nextStep;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Id => nameof(ParameterBasedFlowStep);
        /// <inheritdoc/>
        public string DisplayName => Parameter.DisplayName;

        /// <inheritdoc/>
        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(Parameter.Name);
            return new ValueTask();
        }

        /// <inheritdoc/>
        public async ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            // Discover the parameter value using ParameterDiscovery
            ParameterDiscovery paraDiscovery = new ParameterDiscovery(Parameter, _fileSystem, _environmentService);
            var parameterValue = await paraDiscovery.DiscoverAsync(context);
            if (string.Equals(parameterValue, FlowNavigation.BackInputToken, StringComparison.OrdinalIgnoreCase))
            {
                return FlowStepResult.Back;
            }
            else if (paraDiscovery.State.IsNavigation())
            {
                return new FlowStepResult { State = paraDiscovery.State };
            }
            else
            {
                SelectParameter(context, parameterValue ?? string.Empty);
            }

            // Skip the next step if this is a conditional picker and the value is 'false'
            if (Parameter.PickerType is InteractivePickerType.ConditionalPicker && string.Equals(parameterValue, "false"))
            {
                NextStep = NextStep?.NextStep;
            }

            if (NextStep is not null && NextStep.Parameter.DisplayName.Equals(AspnetStrings.Options.Prerelease.DisplayName, StringComparison.Ordinal) && ShouldSkipPrereleaseOption(context))
            {
                // Skip the prerelease step if the target framework is not net11, prerelease only applies to net11
                //TODO update for the next major release of .NET
                NextStep = NextStep?.NextStep;
            }

            if (NextStep != null)
            {
                return new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } };
            }
            else
            {
                return FlowStepResult.Success;
            }
        }

        /// <inheritdoc/>
        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            // Check for parameter in the context using command-line args
            var cmdlineArgs = context.GetArgsDict();
            var commandSettings = context.GetCommandSettings();
            if (cmdlineArgs != null && cmdlineArgs.Count != 0)
            {
                var strippedParameterName = Parameter.Name.Replace("--", string.Empty);
                cmdlineArgs.TryGetValue(strippedParameterName, out var cmdlineValues);
                if (cmdlineValues != null && cmdlineValues.Count != 0 && ParameterHelpers.CheckType(Parameter.Type, cmdlineValues.First()))
                {
                    SelectParameter(context, cmdlineValues.First());
                    if (NextStep != null)
                    {
                        return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } });
                    }
                    else
                    {
                        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                    }
                }
            }

            // If required or in interactive mode, fail if not found
            if (Parameter.Required || commandSettings != null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"No value found for option '{Parameter.Name}'"));
            }
            else if (NextStep != null)
            {
                return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } });
            }
            else
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Success);
            }
        }

        /// <summary>
        /// Sets the parameter value in the context and initializes code service if needed.
        /// </summary>
        private void SelectParameter(IFlowContext context, string parameterValue)
        {
            if (!string.IsNullOrEmpty(parameterValue))
            {
                context.Set(new FlowProperty(
                    Parameter.Name,
                    parameterValue,
                    Parameter.DisplayName,
                    isVisible: true));
                SelectCodeService(context, parameterValue);
            }
        }

        /// <summary>
        /// Initializes the code service in the context if the parameter is a project picker.
        /// </summary>
        private void SelectCodeService(IFlowContext context, string projectPath)
        {
            var codeService = context.GetCodeService();
            if (Parameter.PickerType is InteractivePickerType.ProjectPicker && codeService is null && !string.IsNullOrEmpty(projectPath))
            {
                codeService = new CodeService(_logger, projectPath);
                context.Set(new FlowProperty(
                    FlowContextProperties.CodeService,
                    codeService));
            }
        }

        /// <summary>
        /// Determines whether the prerelease option should be skipped based on the target framework of the current
        /// project.
        /// </summary>
        /// <remarks>The prerelease option is skipped for projects targeting frameworks other than .NET
        /// 11. If the project file or target framework cannot be determined, the prerelease option is not
        /// skipped.</remarks>
        /// <param name="context">The flow context containing project information and properties. Must not be null.</param>
        /// <returns>true if the prerelease option should be skipped for the current project; otherwise, false.</returns>
        private static bool ShouldSkipPrereleaseOption(IFlowContext context)
        {
            //TODO update with each major release of .NET

            string projectParameterKey = Parameter.GetParameterName(Constants.CliOptions.ProjectCliOption, AspnetStrings.Options.Project.DisplayName);

            if (context.Properties.Get(projectParameterKey) is FlowProperty projectFileProperty &&
                projectFileProperty.Value is string projectFilePath && !string.IsNullOrEmpty(projectFilePath))
            {
                string? targetFramework = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectFilePath);
                return targetFramework is null || !targetFramework.Equals(TargetFrameworkConstants.Net11, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
