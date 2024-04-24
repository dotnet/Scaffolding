// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
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

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var componentName = context.GetComponentObj()?.Command;
            if (string.IsNullOrEmpty(componentName))
            {
                var settings = context.GetCommandSettings();
                componentName = settings?.ComponentName;
            }

            if (string.IsNullOrEmpty(componentName))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Scaffolding component name is needed!"));
            }

            DotNetToolInfo? componentPicked = AnsiConsole
                .Status()
                .WithSpinner()
                .Start("Invoking 'dotnet tool' to get components!", statusContext =>
                {
                    return _dotnetToolService.GetDotNetTool(componentName);
                });

            if (componentPicked is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Scaffolding component (dotnet tool) {componentName} not found!"));
            }

            SelectComponent(context, componentPicked);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.ComponentName);
            context.Unset(FlowContextProperties.ComponentObj);
            context.Unset(FlowContextProperties.CommandInfos);
            return new ValueTask();
        }

        private void SelectComponent(IFlowContext context, DotNetToolInfo component)
        {
            if (component != null)
            {
                var commandInfos = AnsiConsole
                .Status()
                .WithSpinner()
                .Start($"Retrieving commands from '{component.Command}'", statusContext =>
                {
                    return _dotnetToolService.GetCommands(component.Command);
                });

                context.Set(new FlowProperty(
                    name: FlowContextProperties.ComponentName,
                    value: component.Command,
                    displayName: "Component Name",
                    isVisible: true));

                context.Set(new FlowProperty(
                    name : FlowContextProperties.ComponentObj,
                    value: component,
                    displayName: component.ToDisplayString(),
                    isVisible: false));

                context.Set(new FlowProperty(
                    name: FlowContextProperties.CommandInfos,
                    value: commandInfos,
                    isVisible: false));
            }
        }
    }
}
