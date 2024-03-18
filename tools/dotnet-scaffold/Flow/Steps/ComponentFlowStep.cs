// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    /// <summary>
    /// Primary flow step for the rest of scaffold
    /// 2. Get project capabilities
    /// 3. Detect all installed scaffolders (check dotnet tools)
    /// 4. 
    /// </summary>
    public class ComponentFlowStep : IFlowStep
    {
        private readonly ILogger _logger;

        public string Id => nameof(ComponentFlowStep);

        public string DisplayName => "Scaffolding Component";

        public ComponentFlowStep(ILogger logger)
        {
            _logger = logger;
        }

        public string GetJsonString(string jsonString)
        {
            string jsonText = string.Empty;
            try
            {
                jsonText = JsonSerializer.Serialize(jsonString);
            }
            catch (JsonException ex)
            {
                _logger.LogFailureAndExit(ex.ToString());
            }

            if (string.IsNullOrEmpty(jsonText))
            {
                throw new Exception("json serialization error, check the parameters used to initalize.");
            }
            return jsonText;
        }

        public CommandInfo[] GetParameters(string jsonText)
        {
            CommandInfo[]? commands = null;
            try
            {
                commands = JsonSerializer.Deserialize<CommandInfo[]>(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogFailureAndExit(ex.ToString());
            }

            if (commands is null || commands.Length == 0)
            {
                throw new Exception("parameter json parsing error, check the json string being passed.");
            }

            return commands;
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var componentName = context.GetComponentName();
            if (string.IsNullOrEmpty(componentName))
            {
                var settings = context.GetCommandSettings();
                componentName = settings?.ComponentName;
            }

            if (string.IsNullOrEmpty(componentName))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Scaffolding component name is needed!"));
            }

            SelectComponent(context, componentName);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            SelectComponent(context, "thang");
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public void ExecuteComponent()
        {
            //get all parameters from area scaffolder
            //
            //currently executing area scaffolder 1st party component for testing
        }

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.ComponentName);
            return new ValueTask();
        }

        private void SelectComponent(IFlowContext context, string component)
        {
            if (!string.IsNullOrEmpty(component))
            {
                context.Set(new FlowProperty(
                    FlowContextProperties.ComponentName,
                    component,
                    FlowContextProperties.ComponentNameDisplay,
                    isVisible: true));
            }
        }
    }
}
