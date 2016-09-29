// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class DotNetBuildCommandHelper
    {
        internal static BuildResult Build(
            string project,
            string configuration,
            //NuGetFramework framework,
            string buildBasePath)
        {
            // TODO: Specify --runtime?
            var args = new List<string>()
            {
                project,
                "--configuration", configuration,
                //"--framework", framework.GetShortFolderName(),
            };

            if (buildBasePath != null)
            {
                // ProjectDependenciesCommandFactory cannot handle relative build base paths.
                buildBasePath = (!Path.IsPathRooted(buildBasePath)) 
                    ? Path.Combine(Directory.GetCurrentDirectory(), buildBasePath)
                    : buildBasePath;

                args.Add("--build-base-path");
                args.Add(buildBasePath);
            }

            var stdOutMsgs = new List<string>();
            var stdErrorMsgs = new List<string>();

            var command = Command.CreateDotNet(
                    "build",
                    args,
                    //framework,
                    configuration:configuration)
                    .OnErrorLine((str) => stdOutMsgs.Add(str))
                    .OnOutputLine((str) => stdErrorMsgs.Add(str));

            var result = command.Execute();
            return new BuildResult()
            {
                Result = result,
                StdErr = stdErrorMsgs,
                StdOut = stdOutMsgs
            };
        }
    }

    internal class BuildResult
    {
        public CommandResult Result { get; set; }
        public List<string> StdErr { get; set; }
        public List<string> StdOut { get; set; }
    }
}
