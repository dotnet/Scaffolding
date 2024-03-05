// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ExtensibilityModel;
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
        public Parameter[] Parameters { get; set; }

        public string Id => nameof(ComponentFlowStep);

        public string DisplayName => "Scaffolding Component";

        public ComponentFlowStep()
        {
        }

       /* public string GetJsonString()
        {
            string jsonText = string.Empty;
            try
            {
                jsonText = JsonSerializer.Serialize(Parameters);
            }
            catch (JsonException ex)
            {
                AnsiConsole.WriteLine(ex.ToString());
            }

            if (string.IsNullOrEmpty(jsonText))
            {
                throw new Exception("json serialization error, check the parameters used to initalize.");
            }
            return jsonText;
        }

        public Parameter[] GetParameters(string jsonText)
        {
            Parameter[]? parameters = null;
            try
            {
                parameters = JsonSerializer.Deserialize<Parameter[]>(jsonText);
            }
            catch (JsonException ex)
            {
                AnsiConsole.WriteLine(ex.ToString());
            }

            if (parameters is null || !parameters.Any())
            {
                throw new Exception("parameter json parsing error, check the json string being passed.");
            }
            else
            {
                Parameters = parameters.ToArray();
            }

            return parameters;
        }*/

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

            SelectSourceProject(context, projectPath);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public static Dictionary<BaseTypes, Type> TypeDict = new()
        {
            { BaseTypes.Bool, typeof(bool) },
            { BaseTypes.Int, typeof(int) },
            { BaseTypes.Long, typeof(long) },
            { BaseTypes.Double, typeof(double) },
            { BaseTypes.Decimal, typeof(decimal) },
            { BaseTypes.Char, typeof(char) },
            { BaseTypes.String, typeof(string) },
            { BaseTypes.ListBool, typeof(List<bool>) },
            { BaseTypes.ListLong, typeof(List<long>) },
            { BaseTypes.ListInt, typeof(List<int>) },
            { BaseTypes.ListDouble, typeof(List<double>) },
            { BaseTypes.ListDecimal, typeof(List<decimal>) },
            { BaseTypes.ListChar, typeof(List<char>) },
            { BaseTypes.ListString, typeof(List<string>) }
        };
    }
}
