// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;

namespace Microsoft.Extensions.CodeGeneration
{
    public class Program
    {
        private static ConsoleLogger _logger;

        private const string APPNAME = "Code Generation";
        private const string APP_DESC = "Code generation for Asp.net Core";

        private static bool isProjectDependencyCommand;

        public static void Main(string[] args)
        {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            _logger = new ConsoleLogger();
            // We don't want to expose this flag, as it only needs to be used for identifying self invocation. 
            isProjectDependencyCommand = args.Contains("--no-dispatch");
            _logger.LogMessage($"Is Dependency Command: {isProjectDependencyCommand}", LogMessageLevel.Trace);
            try
            {
                Execute(args);
            }
            finally
            {
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                if (!isProjectDependencyCommand)
                {
                    // Check is needed so we don't show the runtime twice (once for the portable process and once for the dependency process)
                    _logger.LogMessage("RunTime " + elapsedTime, LogMessageLevel.Information);
                }
            }
        }

        /// <summary>
        /// The execution is done in 2 phases. 
        /// Phase 1 ::
        ///    1. Determine if the tool is running as a project dependency or not. 
        ///    2. Try getting the project context for the project (use netcoreapp1.0 as the tfm if not running as dependency command or else use the tfm passed in)
        ///    3. If not running as dependency command and project context cannot be built using netcoreapp1.0, invoke project dependency command with the first tfm found in the project.json
        ///    
        /// Phase 2 ::
        ///     1. After successfully getting the Project context, invoke the CodeGenCommandExecutor.
        /// </summary>
        private static void Execute(string[] args)
        {
            var app = new CommandLineApplication(false)
            {
                Name = APPNAME,
                Description = APP_DESC
            };

            // Define app Options;
            app.HelpOption("-h|--help");
            var projectPath = app.Option("-p|--project", "Path to project.json", CommandOptionType.SingleValue);
            var packagesPath = app.Option("-n|--nuget-package-dir", "Path to check for Nuget packages", CommandOptionType.SingleValue);
            var appConfiguration = app.Option("-c|--configuration", "Configuration for the project (Possible values: Debug/ Release)", CommandOptionType.SingleValue);
            var framework = app.Option("-tfm|--target-framework", "Target Framework to use. (Short folder name of the tfm. eg. net451)", CommandOptionType.SingleValue);
            var buildBasePath = app.Option("-b|--build-base-path", "", CommandOptionType.SingleValue);
            var dependencyCommand = app.Option("--no-dispatch", "", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                string project = projectPath.Value();
                if (string.IsNullOrEmpty(project))
                {
                    project = Directory.GetCurrentDirectory();
                }
                var configuration = appConfiguration.Value() ?? Constants.DefaultConfiguration;
                var projectFile = ProjectReader.GetProject(project);
                var frameworksInProject = projectFile.GetTargetFrameworks().Select(f => f.FrameworkName);

                var nugetFramework = FrameworkConstants.CommonFrameworks.NetCoreApp10;

                if (!isProjectDependencyCommand)
                {
                    // Invoke the tool from the project's build directory. 
                    return BuildAndDispatchDependencyCommand(
                        args, 
                        frameworksInProject.FirstOrDefault(), 
                        project, 
                        buildBasePath.Value(), 
                        configuration);
                }
                else
                {
                    if (!TryGetNugetFramework(framework.Value(), out nugetFramework))
                    {
                        throw new ArgumentException($"Could not understand the NuGetFramework information. Framework short folder name passed in was: {framework.Value()}");
                    }

                    var nearestNugetFramework = NuGetFrameworkUtility.GetNearest(
                        frameworksInProject,
                        nugetFramework,
                        f => new NuGetFramework(f));

                    if(nearestNugetFramework == null)
                    {
                        // This should never happen as long as we dispatch correctly.
                        var msg = "Could not find a compatible framework to execute."
                            + Environment.NewLine
                            +$"Available frameworks in project:{string.Join($"{Environment.NewLine} -", frameworksInProject.Select(f => f.GetShortFolderName()))}";
                        throw new InvalidOperationException(msg);
                    }

                    ProjectContext context = new ProjectContextBuilder()
                        .WithProject(projectFile)
                        .WithTargetFramework(nearestNugetFramework)
                        .Build();

                    Debug.Assert(context != null);

                    var codeGenArgs = ToolCommandLineHelper.FilterExecutorArguments(args);

                    CodeGenCommandExecutor executor = new CodeGenCommandExecutor(
                        context,
                        codeGenArgs,
                        configuration,
                        packagesPath.Value(),
                        _logger);

                    return executor.Execute();
                }
            });

            app.Execute(args);
        }

        private static int BuildAndDispatchDependencyCommand(
            string[] args,
            NuGetFramework frameworkToUse,
            string projectPath, 
            string buildBasePath, 
            string configuration)
        {
            if(frameworkToUse == null)
            {
                throw new ArgumentNullException(nameof(frameworkToUse));
            }
            if(string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            var buildExitCode = DotNetBuildCommandHelper.Build(
                projectPath,
                configuration,
                frameworkToUse,
                buildBasePath);

            if (buildExitCode != 0)
            {
                //Build failed. 
                // Stop the process here. 
                return buildExitCode;
            }

            // Invoke the dependency command
            var projectFilePath = projectPath.EndsWith("project.json") 
                ? projectPath 
                : Path.Combine(projectPath, "project.json");

            var projectDirectory = Directory.GetParent(projectFilePath).FullName;
            
            var dependencyArgs = ToolCommandLineHelper.GetProjectDependencyCommandArgs(
                     args,
                     frameworkToUse.GetShortFolderName());

            var exitCode = new ProjectDependenciesCommandFactory(
                 frameworkToUse,
                 configuration,
                 null,
                 buildBasePath,
                 projectDirectory)
             .Create(
                 PlatformServices.Default.Application.ApplicationName,
                 dependencyArgs,
                 frameworkToUse,
                 configuration)
            .ForwardStdErr()
            .ForwardStdOut()
            .Execute()
            .ExitCode;

            return exitCode;
        }

        private static bool TryGetNugetFramework(string folderName, out NuGetFramework nugetFramework)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                NuGetFramework tfm = NuGetFramework.Parse(folderName);
                if (tfm != null)
                {
                    nugetFramework = tfm;
                    return true;
                }
            }
            nugetFramework = null;
            return false;
        }
    }
}