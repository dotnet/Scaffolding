// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    internal class ToolCommandLineHelper
    {
        private const string NO_DISPATCH_FLAG = "--no-dispatch";
        private const string CONFIGURATION = "--configuration";
        private const string TARGET_FRAMEWORK = "--target-framework";
        private const string PROJECT = "--project";
        private const string NUGET_PACKAGE_DIR = "--nuget-package-dir";
        private const string BUILD_BASE_PATH = "--build-base-path";
        private const string DISPATCHER_VERSION = "--dispatcher-version";
        private const string NO_BUILD = "--no-build";
        private const string SIM_MODE = "--simulation-mode";

        private const string NO_DISPATCH_FLAG_SHORT = "-nd";
        private const string CONFIGURATION_SHORT = "-c";
        private const string TARGET_FRAMEWORK_SHORT = "-tfm";
        private const string PROJECT_SHORT = "-p";
        private const string NUGET_PACKAGE_DIR_SHORT = "-n";
        private const string BUILD_BASE_PATH_SHORT = "-b";
        private const string PORT_NUMBER = "--port-number";

        internal static string[] FilterExecutorArguments(string[] args)
        {
            if (args == null)
            {
                return args;
            }
            List<string> filteredArgs = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case NO_DISPATCH_FLAG:
                    case NO_DISPATCH_FLAG_SHORT:
                    case NO_BUILD:
                    case SIM_MODE:
                        break;
                    case CONFIGURATION:
                    case CONFIGURATION_SHORT:
                    case TARGET_FRAMEWORK:
                    case TARGET_FRAMEWORK_SHORT:
                    case PROJECT:
                    case PROJECT_SHORT:
                    case NUGET_PACKAGE_DIR:
                    case NUGET_PACKAGE_DIR_SHORT:
                    case BUILD_BASE_PATH:
                    case BUILD_BASE_PATH_SHORT:
                    case DISPATCHER_VERSION:
                    case PORT_NUMBER:
                        i++;
                        break;

                    default:
                        filteredArgs.Add(args[i]);
                        break;
                }
            }

            return filteredArgs.ToArray();
        }

        internal static bool IsSimulationMode(string[] args) => args.Contains(SIM_MODE, StringComparer.OrdinalIgnoreCase);
    }
}
