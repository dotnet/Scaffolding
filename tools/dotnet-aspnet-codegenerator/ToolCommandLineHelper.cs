﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
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
                        i++;
                        break;

                    default:
                        filteredArgs.Add(args[i]);
                        break;
                }
            }

            return filteredArgs.ToArray();
        }

        /// <summary>
        /// Adds the --dependencyCommand flag, --port-number option and --targetFramework option
        /// </summary>
        internal static string[] GetProjectDependencyCommandArgs(string[] args, string frameworkName, string portNumber)
        {
            List<string> cmdArgs = new List<string>();
            cmdArgs.Add(NO_DISPATCH_FLAG.ToLowerInvariant());
            cmdArgs.Add(PORT_NUMBER.ToLowerInvariant());
            cmdArgs.Add(portNumber);

            cmdArgs.AddRange(args);
            return cmdArgs.ToArray();
        }

        internal static bool IsNoBuild(string[] args) => args.Contains(NO_BUILD, StringComparer.OrdinalIgnoreCase);

        internal static bool IsHelpArgument(string[] args) => args.Any(a => a.Equals("-h", StringComparison.OrdinalIgnoreCase)
                                                                         || a.Equals("--help", StringComparison.OrdinalIgnoreCase));
    }
}
