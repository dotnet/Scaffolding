// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ComponentDiscovery
    {
        private readonly IDotNetToolService _dotnetToolService;

        public ComponentDiscovery(IDotNetToolService dotNetToolService)
        {
            _dotnetToolService =  dotNetToolService;
        }
        public FlowStepState State { get; private set; }

        public DotNetToolInfo? Discover(IFlowContext context)
        {
            return Prompt(context, "Pick a scaffolding component ('dotnet tool')", _dotnetToolService.GlobalDotNetTools);
        }

        private DotNetToolInfo? Prompt(IFlowContext context, string title, IList<DotNetToolInfo> components)
        {
            if (components.Count == 0)
            {
                return null;
            }

            if (components.Count == 1)
            {
                return components[0];
            }

            var prompt = new FlowSelectionPrompt<DotNetToolInfo>()
                .Title(title)
                .Converter(GetDotNetToolInfoDisplayString)
                .AddChoices(components, navigation: context.Navigation);

            var result = prompt.Show();
            State = result.State;
            return result.Value;
        }

        internal string GetDotNetToolInfoDisplayString(DotNetToolInfo dotnetToolInfo)
        {
            return dotnetToolInfo.ToDisplayString();
        }
    }
}
