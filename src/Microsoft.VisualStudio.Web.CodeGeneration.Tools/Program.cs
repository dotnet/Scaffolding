// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using NuGet.Frameworks;
using Microsoft.VisualStudio.Web.CodeGeneration.MsBuild;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    public class Program
    {
        private static ConsoleLogger _logger;
        private static bool _isNoBuild;

        private const string APPNAME = "Code Generation";
        private const string APP_DESC = "Code generation for Asp.net Core";
        private const string TOOL_NAME = "dotnet-aspnet-codegenerator";
        private const string DESIGN_TOOL_NAME = "dotnet-aspnet-codegenerator-design";
        private const string PROJECT_JSON_SUPPORT_VERSION = "1.0.0-preview2-update1";
        private static ScaffoldingServer server;

        public static void Main(string[] args)
        {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            _logger = new ConsoleLogger();
            _logger.LogMessage($"Command Line: {string.Join(" ", args)}", LogMessageLevel.Trace);

            _isNoBuild = ToolCommandLineHelper.IsNoBuild(args);
            try
            {
                DotnetToolDispatcher.EnsureValidDispatchRecipient(ref args);
                Execute(args, _isNoBuild, _logger);
            }
            finally
            {
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);

                _logger.LogMessage("RunTime " + elapsedTime, LogMessageLevel.Information);
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
        private static void Execute(string[] args, bool isNoBuild, ILogger logger)
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
            var noBuild = app.Option("--no-build","", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                string project = projectPath.Value();
                if (string.IsNullOrEmpty(project))
                {
                    project = Directory.GetCurrentDirectory();
                }
                project = Path.GetFullPath(project);
                var configuration = appConfiguration.Value() ?? Constants.DefaultConfiguration;

                ThrowIfProjectJson(project);

                // Invoke the tool from the project's build directory.
                return BuildAndDispatchDependencyCommand(
                    args,
                    project,
                    buildBasePath.Value(),
                    configuration,
                    isNoBuild,
                    logger);

            });

            app.Execute(args);
        }

        private static void ThrowIfProjectJson(string projectPath)
        {
            var attr = File.GetAttributes(projectPath);
            if (projectPath.EndsWith("project.json")
                && !attr.HasFlag(FileAttributes.Directory))
            {
                var msg = $"This version of {TOOL_NAME} does not support project.json based projects.{Environment.NewLine}"
                    +$"Please migrate your project to newer version, or downgrade the version of {TOOL_NAME} to {PROJECT_JSON_SUPPORT_VERSION}";
                throw new Exception(msg);
            }
        }

        private static int BuildAndDispatchDependencyCommand(
            string[] args,
            string projectPath,
            string buildBasePath,
            string configuration,
            bool noBuild,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (!noBuild)
            {
                logger.LogMessage("Building project ...");
                var buildResult = DotNetBuildCommandHelper.Build(
                    projectPath,
                    configuration,
                    //frameworkToUse,
                    buildBasePath);

                if (buildResult.Result.ExitCode != 0)
                {
                    //Build failed. 
                    // Stop the process here. 
                    logger.LogMessage("Build Failed");
                    logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdOut), LogMessageLevel.Error);
                    logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdErr), LogMessageLevel.Error);
                    return buildResult.Result.ExitCode;
                }
            }

            var projectFilePath = projectPath.EndsWith(".csproj")
                ? projectPath
                : FindCsProjInDirectory(projectPath);

            var projectFile = MsBuildProjectFileReader.ReadProjectFile(projectFilePath);
            var frameworksInProject = projectFile.TargetFrameworks;

            var nearestFramework = NuGetFrameworkUtility.GetNearest(frameworksInProject,
                FrameworkConstants.CommonFrameworks.NetCoreApp10,
                f => new NuGetFramework(f));

            if (nearestFramework == null)
            {
                nearestFramework = NuGetFrameworkUtility.GetNearest(frameworksInProject,
                FrameworkConstants.CommonFrameworks.Net451,
                f => new NuGetFramework(f));
            }

            if (nearestFramework == null)
            {
                throw new Exception("Could not find a suitable target framework to use. Please make sure your project targets a framework compatible with netcoreapp1.0 or net451");
            }

            // Build DependencyInformation.
            var msBuildRunner = new MsBuilder<ScaffoldingBuildProcessor>(projectFilePath, new ScaffoldingBuildProcessor());
            msBuildRunner.RunMsBuild(NuGetFramework.Parse(nearestFramework));
            var dependencyProvider = msBuildRunner.BuildProcessor.CreateDependencyProvider();
            var projectContext = msBuildRunner.BuildProcessor.CreateMsBuildProjectContext();

            // Start server
            var server = StartServer(logger);

            // Invoke the dependency command

            var frameworkToUse = projectContext.TargetFramework;
            var projectDirectory = Directory.GetParent(projectFilePath).FullName;
            var dependencyArgs = ToolCommandLineHelper.GetProjectDependencyCommandArgs(
                     args,
                     frameworkToUse.GetShortFolderName(),
                     server.Port.ToString());

            var exitCode = DotnetToolDispatcher.CreateDispatchCommand(
                    dependencyArgs,
                    frameworkToUse,
                    configuration,
                    null,
                    buildBasePath,
                    projectDirectory,
                    DESIGN_TOOL_NAME)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute()
                .ExitCode;

            return exitCode;
        }

        private static ScaffoldingServer StartServer(ILogger logger)
        {
            server = ScaffoldingServer.Listen(logger);
            var messageHandler = new ScaffoldingMessageHandler(logger, "Scaffolding_server");
            server.MessageReceived += messageHandler.HandleMessages;
            server.Accept();
            return server;
        }

        private static string FindCsProjInDirectory(string projectFilePath)
        {
            if(string.IsNullOrEmpty(projectFilePath))
            {
                projectFilePath = Directory.GetCurrentDirectory();
            }

            var dir = Path.GetDirectoryName(projectFilePath);
            var projects = Directory.EnumerateFiles(dir, "*.csproj");

            if (projects.Count() == 1)
            {
                return projects.First();
            }

            throw new ArgumentException("Please provide path to the csproj file.");
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