// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ParameterDiscovery
    {
        private readonly Parameter _parameter;
        public ParameterDiscovery(Parameter parameter)
        {
            _parameter = parameter;
        }
        public FlowStepState State { get; private set; }

        public string Discover(IFlowContext context)
        {
            return Prompt(context, $"Enter new value for '{_parameter.DisplayName}' (or [lightseagreen]<[/] to go back : ");
        }

        private string Prompt(IFlowContext context, string title)
        {
            //check if Parameter has a InteractivePickerType
/*            if (_parameter.PickerType is null)
            {*/
                var prompt = new TextPrompt<string>(title)
                .ValidationErrorMessage("bad value fix it please")
                .Validate(x =>
                {
                    if (x.Trim() == FlowNavigation.BackInputToken)
                    {
                        return ValidationResult.Success();
                    }

                    return Validate(context, x);
                });

                return AnsiConsole.Prompt(prompt).Trim();
/*            }
            else
            {
                var msBuildProject = context.GetSourceProject();
                var compileItems = msBuildProject?.GetItems("Compile");
                var sourceFiles = msBuildProject?.GetItems("Compile").Select(i => i.EvaluatedInclude);
                return "";
            }*/
            
        }

        private ValidationResult Validate(IFlowContext context, string promptVal)
        {
            if (string.IsNullOrEmpty(promptVal) || !ParameterHelpers.CheckType(_parameter.Type, new List<string> { promptVal }))
            {
                return ValidationResult.Error("Invalid input, please try again!");
            }

            return ValidationResult.Success();
        }
    }
}
