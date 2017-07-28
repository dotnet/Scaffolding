// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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