// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps.Project;
using Spectre.Console;
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
        private readonly IDotNetToolService _dotnetToolService;

        public string Id => nameof(ComponentFlowStep);

        public string DisplayName => "Scaffolding Component";

        public ComponentFlowStep(ILogger logger, IDotNetToolService dotnetToolService)
        {
            _logger = logger;
            _dotnetToolService = dotnetToolService;
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

        public CommandInfo[] GetCommandInfo(string componentName)
        {
            CommandInfo[]? commands = null;
            try
            {
                //commands = JsonSerializer.Deserialize<CommandInfo[]>("{bleh}");
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

            var componentPicked = _dotnetToolService.GetDotNetTool(componentName);
            if (componentPicked is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Scaffolding component (dotnet tool) {componentName} not found!"));
            }

            SelectComponent(context, componentPicked);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            ComponentDiscovery componentDiscovery = new ComponentDiscovery(_dotnetToolService);
            var componentPicked = componentDiscovery.Discover(context);

            if (componentDiscovery.State.IsNavigation())
            {
                return new ValueTask<FlowStepResult>(new FlowStepResult { State = componentDiscovery.State });
            }

            if (componentPicked is not null)
            {
                SelectComponent(context, componentPicked);
                return new ValueTask<FlowStepResult>(FlowStepResult.Success);
            }

            AnsiConsole.WriteLine("No projects found in current directory");
            return new ValueTask<FlowStepResult>(FlowStepResult.Failure());
        }

        public void ExecuteComponent(DotNetToolInfo component)
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

        private void SelectComponent(IFlowContext context, DotNetToolInfo component)
        {
            if (component != null)
            {
                context.Set(new FlowProperty(
                    name : FlowContextProperties.ComponentName,
                    value: component,
                    displayName: component.ToDisplayString(),
                    isVisible: true));
            }
        }
    }
}
