// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using NuGet.Frameworks;
using Microsoft.Extensions.ProjectModel;

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
        /// Steps of execution
        ///    1. Try getting the projectContext for the project
        ///    2. Invoke project dependency command with the first compatible tfm found in the project
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
            var noBuild = app.Option("--no-build", "", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                string project = projectPath.Value();
                if (string.IsNullOrEmpty(project))
                {
                    project = Directory.GetCurrentDirectory();
                }

                project = Path.GetFullPath(project);
                var configuration = appConfiguration.Value() ?? Constants.DefaultConfiguration;

                var projectFileFinder = new ProjectFileFinder(project);

                // Invoke the tool from the project's build directory.
                return BuildAndDispatchDependencyCommand(
                    args,
                    projectFileFinder.ProjectFilePath,
                    buildBasePath.Value(),
                    configuration,
                    isNoBuild,
                    logger);

            });

            app.Execute(args);
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

            var context = GetProjectInformation(projectPath);
            var frameworkToUse = NuGetFramework.Parse(context.TargetFramework);
            if (!_isNoBuild)
            {
                var result = Build(logger, projectPath, configuration, frameworkToUse, buildBasePath);
                if (result != 0)
                {
                    return result;
                }
            }

            // Start server
            var server = StartServer(logger, context);
            
            var projectDirectory = Directory.GetParent(context.ProjectFullPath).FullName;
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

        private static IProjectContext GetProjectInformation(string projectPath)
        {
            var projectFileFinder = new ProjectFileFinder(projectPath);
            if (projectFileFinder.IsMsBuildProject)
            {
                return new MsBuildProjectContextBuilder(projectFileFinder.ProjectFilePath)
                    .Build();
            }
            else
            {
                return new DotNetProjectContextBuilder(
                    projectFileFinder.ProjectFilePath,
                    new NuGetFramework[] 
                    {
                        FrameworkConstants.CommonFrameworks.NetCoreApp10,
                        FrameworkConstants.CommonFrameworks.Net451
                    })
                    .Build();
            }
        }

        private static int Build(ILogger logger, string projectPath, string configuration, NuGetFramework frameworkToUse, string buildBasePath)
        {

            logger.LogMessage("Building project ...");
            var buildResult = DotNetBuildCommandHelper.Build(
                projectPath,
                configuration,
                frameworkToUse,
                buildBasePath);

            if (buildResult.Result.ExitCode != 0)
            {
                //Build failed. 
                // Stop the process here. 
                logger.LogMessage("Build Failed");
                logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdOut), LogMessageLevel.Error);
                logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdErr), LogMessageLevel.Error);
            }

            return buildResult.Result.ExitCode;
        }

        private static ScaffoldingServer StartServer(ILogger logger, IProjectContext projectInformation)
        {
            server = ScaffoldingServer.Listen(logger);
            var messageHandler = new ScaffoldingMessageHandler(logger, "Scaffolding_server");
            messageHandler.ProjectInfo = projectInformation;
            server.MessageReceived += messageHandler.HandleMessages;
            server.Accept();
            return server;
        }
    }
}