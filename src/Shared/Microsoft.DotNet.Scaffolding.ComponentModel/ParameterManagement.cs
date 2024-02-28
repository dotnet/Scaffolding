// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;

namespace Microsoft.DotNet.Scaffolding.ExtensibilityModel
{
    public class ParameterManagement
    {
        public Parameter[] Parameters { get; set; }
        public ParameterManagement(Parameter[] parameters)
        {
            Parameters = parameters;
        }

        public string GetJsonString()
        {
            string jsonText = string.Empty;
            try
            {
                jsonText = JsonSerializer.Serialize(Parameters);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex.ToString());
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
                Console.WriteLine(ex.ToString());
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
        }
    }
}
