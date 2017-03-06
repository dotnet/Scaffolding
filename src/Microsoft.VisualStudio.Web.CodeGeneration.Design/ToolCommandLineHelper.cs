// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    internal class ToolCommandLineHelper
    {
        private const string NO_DISPATCH_FLAG = "--NO-DISPATCH";
        private const string CONFIGURATION = "--CONFIGURATION";
        private const string TARGET_FRAMEWORK = "--TARGET-FRAMEWORK";
        private const string PROJECT = "--PROJECT";
        private const string NUGET_PACKAGE_DIR = "--NUGET-PACKAGE-DIR";
        private const string BUILD_BASE_PATH = "--BUILD-BASE-PATH";
        private const string DISPATCHER_VERSION = "--DISPATCHER-VERSION";
        private const string NO_BUILD = "--NO-BUILD";
        private const string SIM_MODE = "--SIMULATION-MODE";
        private const string PORT_NUMBER = "--PORT-NUMBER";

        private const string NO_DISPATCH_FLAG_SHORT = "-ND";
        private const string CONFIGURATION_SHORT = "-C";
        private const string TARGET_FRAMEWORK_SHORT = "-TFM";
        private const string PROJECT_SHORT = "-P";
        private const string NUGET_PACKAGE_DIR_SHORT = "-N";
        private const string BUILD_BASE_PATH_SHORT = "-B";

        internal static string[] FilterExecutorArguments(string[] args)
        {
            if (args == null)
            {
                return args;
            }
            List<string> filteredArgs = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToUpperInvariant())
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
