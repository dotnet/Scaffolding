// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.Extensions.CodeGeneration
{
    internal class DotNetBuildCommandHelper
    {
        internal static int Build(
            string project,
            string configuration,
            NuGetFramework framework,
            string buildBasePath)
        {
            // TODO: Specify --runtime?
            var args = new List<string>()
            {
                project,
                "--configuration", configuration,
                "--framework", framework.GetShortFolderName(),
            };

            if (buildBasePath != null && !Path.IsPathRooted(buildBasePath))
            {
                // ProjectDependenciesCommandFactory cannot handle relative build base paths.
                buildBasePath = Path.Combine(Directory.GetCurrentDirectory(), buildBasePath);
                args.Add("--build-base-path");
                args.Add(buildBasePath);
            }

            var command = Command.CreateDotNet(
                    "build",
                    args,
                    framework,
                    configuration)
                .ForwardStdErr();

            var result = command.Execute();

            return result.ExitCode;
        }
    }
}
