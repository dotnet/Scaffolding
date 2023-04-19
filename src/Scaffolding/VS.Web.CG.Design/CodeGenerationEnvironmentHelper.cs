// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    internal static class CodeGenerationEnvironmentHelper
    {
        public static Dictionary<string, string> DefaultEnvironmentVariables = new Dictionary<string, string>()
        {
            {"ASPNETCORE_ENVIRONMENT", "Development"}
        };

        public static void SetupEnvironment()
        {
            SetupEnvironment(DefaultEnvironmentVariables);
        }

        public static void SetupEnvironment(Dictionary<string, string> environmentVariables)
        {
            if (environmentVariables == null)
            {
                throw new ArgumentNullException(nameof(environmentVariables));
            }

            foreach (var variable in environmentVariables)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }
    }
}
