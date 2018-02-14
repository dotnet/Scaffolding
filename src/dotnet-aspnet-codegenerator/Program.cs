// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    public class Program
    {
        private static bool _isNoBuild;
        private const string TOOL_NAME = "dotnet-aspnet-codegenerator";
        private const string DESIGN_TOOL_NAME = "dotnet-aspnet-codegenerator-design";
        private const string PROJECT_JSON_SUPPORT_VERSION = "1.0.0-preview4-final";
        private static ScaffoldingServer server;

        // Wait time after the 'inside_man' process has exited to process any messages.
        private static readonly int ServerWaitTimeForExit = 3;

        internal bool SkipImportTarget { get; set; }

        private string _codeGenerationTargetsLocation;
        internal string CodeGenerationTargetsLocation 
        { 
            get
            {
                if (string.IsNullOrEmpty(_codeGenerationTargetsLocation))
                {
                    _codeGenerationTargetsLocation = GetTargetsLocation();
                }

                return _codeGenerationTargetsLocation;
            }
            set
            {
                _codeGenerationTargetsLocation = value;
            }
        }

        private ILogger _logger;
        internal ILogger Logger 
        { 
            get
            {
                if (_logger == null)
                {
                    _logger = new ConsoleLogger();
                }
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        public static int Main(string[] args)
        {
            int exitCode = -1;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var instance = new Program();
            instance.Logger.LogMessage($"Command Line: {string.Join(" ", args)}", LogMessageLevel.Trace);

            _isNoBuild = ToolCommandLineHelper.IsNoBuild(args);
            try
            {
                DotnetToolDispatcher.EnsureValidDispatchRecipient(ref args);
                instance.SkipImportTarget = false;
                exitCode = instance.Execute(args, _isNoBuild);
            }
            finally
            {
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);

                instance.Logger.LogMessage("RunTime " + elapsedTime, LogMessageLevel.Information);
            }

            return exitCode;
        }

        /// <summary>
        /// Steps of execution
        ///    1. Try getting the projectContext for the project
        ///    2. Invoke project dependency command with the first compatible tfm found in the project
        /// </summary>
        internal int Execute(string[] args, bool isNoBuild)
        {
            var app = new ScaffoldingApp(false);
            bool isShowHelp = false;
            app.OnExecute(() =>
            {
                try
                {
                    string project = app.ProjectPath.Value();
                    if (string.IsNullOrEmpty(project))
                    {
                        project = Directory.GetCurrentDirectory();
                    }

                    project = Path.GetFullPath(project);
                    var configuration = app.AppConfiguration.Value() ?? "Debug";

                    isShowHelp = ToolCommandLineHelper.IsHelpArgument(args)
                                                || app.GeneratorArgument == null
                                                || string.IsNullOrEmpty(app.GeneratorArgument.Value);
                    
                    ProjectFileFinder projectFileFinder = new ProjectFileFinder(project);

                    if (isShowHelp)
                    {
                        app.ProjectContext = GetProjectInformation(projectFileFinder.ProjectFilePath, configuration);
                        app.ShowHelp();
                        return 0;
                    }
                    // Invoke the tool from the project's build directory.
                    return BuildAndDispatchDependencyCommand(
                        args,
                        projectFileFinder.ProjectFilePath,
                        app.BuildBasePath.Value(),
                        configuration,
                        isNoBuild,
                        Logger);
                }
                catch (Exception ex)
                {
                    Logger.LogMessage(Resources.GenericErrorMessage, LogMessageLevel.Error);
                    Logger.LogMessage(ex.Message, LogMessageLevel.Error);

                    if (isShowHelp)
                    {
                        app.ShowHelp();
                    }

                    Logger.LogMessage(ex.StackTrace, LogMessageLevel.Trace);
                    if (Logger is ConsoleLogger consoleLogger && !consoleLogger.IsTracing)
                    {
                        Logger.LogMessage(Resources.EnableTracingMessage);
                    }
                    return -1;
                }

            });

            return app.Execute(args);
        }

        private int BuildAndDispatchDependencyCommand(
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

            var context = GetProjectInformation(projectPath, configuration);
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
            try
            {
                var command = CreateDipatchCommand(
                    context,
                    args,
                    buildBasePath,
                    configuration,
                    frameworkToUse,
                    server);

                var exitCode = command
                    .OnErrorLine(e => logger.LogMessage(e, LogMessageLevel.Error))
                    .OnOutputLine(e => logger.LogMessage(e, LogMessageLevel.Information))
                    .Execute()
                    .ExitCode;

                return exitCode;
            }
            finally
            {
                server.WaitForExit(TimeSpan.FromSeconds(ServerWaitTimeForExit));
            }
        }

        // Creates a command to execute dotnet-aspnet-codegenerator-design
        private Command CreateDipatchCommand(
            IProjectContext context,
            string[] args,
            string buildBasePath,
            string configuration,
            NuGetFramework frameworkToUse,
            ScaffoldingServer server)
        {
            var projectDirectory = Directory.GetParent(context.ProjectFullPath).FullName;

            // Command Resolution Args
            // To invoke dotnet-aspnet-codegenerator-design with the user project's dependency graph,
            // we need to pass in the runtime config and deps file to dotnet for netcore app projects.
            // For projects that target net4x, since the dotnet-aspnet-codegenerator-design.exe is in the project's bin folder
            // and `dotnet build` generates a binding redirect file for it, we can directly invoke the exe from output location.

            var targetDir = Path.GetDirectoryName(context.AssemblyFullPath);
            var runtimeConfigPath = Path.Combine(targetDir, context.RuntimeConfig);
            var depsFile = Path.Combine(targetDir, context.DepsFile);

            string dotnetCodeGenInsideManPath = string.Empty;
            
            if (IsNetCoreAppFramework(frameworkToUse))
            {
                dotnetCodeGenInsideManPath = context.CompilationAssemblies
                .Where(c => Path.GetFileNameWithoutExtension(c.Name)
                            .Equals(DESIGN_TOOL_NAME, StringComparison.OrdinalIgnoreCase))
                .Select(reference => reference.ResolvedPath)
                .FirstOrDefault();
                if (string.IsNullOrEmpty(dotnetCodeGenInsideManPath))
                {
                    throw new InvalidOperationException(Resources.AddDesignPackage);
                }
            }
            else
            {
                dotnetCodeGenInsideManPath = Path.Combine(Path.GetDirectoryName(context.AssemblyFullPath), DESIGN_TOOL_NAME + ".exe");
                if (!File.Exists(dotnetCodeGenInsideManPath))
                {
                    throw new InvalidOperationException(Resources.AddDesignPackage);
                }
            }

            var dependencyArgs = ToolCommandLineHelper.GetProjectDependencyCommandArgs(
                     args,
                     frameworkToUse.GetShortFolderName(),
                     server.Port.ToString());

            return DotnetToolDispatcher.CreateDispatchCommand(
                    runtimeConfigPath: runtimeConfigPath,
                    depsFile: depsFile,
                    dependencyToolPath: dotnetCodeGenInsideManPath,
                    dispatchArgs: dependencyArgs,
                    framework: frameworkToUse,
                    configuration: configuration,
                    projectDirectory: projectDirectory,
                    assemblyFullPath: context.AssemblyFullPath);
        }

        private static bool IsNetCoreAppFramework(NuGetFramework framework)
        {
            // Only need to compare the framework name to be netcoreapp. Version doesn't matter.

            return NuGetFramework.FrameworkNameComparer.Equals(
                framework,
                NuGet.Frameworks.FrameworkConstants.CommonFrameworks.NetCoreApp10);
        }

        private IProjectContext GetProjectInformation(string projectPath, string configuration)
        {
            var projectFileFinder = new ProjectFileFinder(projectPath);
            if (projectFileFinder.IsMsBuildProject)
            {
                if (!SkipImportTarget)
                {
                    // First install the target to obj folder so that it imports the one in the tools package.
                    // Here we are taking an assumption that the 'obj' folder is the right place to put the project extension target.
                    // This may not always be true if the user's settings override the default in the csproj.
                    // However, currently restoring the project always creates the obj folder irrespective of the user's settings.

                    new TargetInstaller(_logger)
                        .EnsureTargetImported(
                            Path.GetFileName(projectFileFinder.ProjectFilePath),
                            Path.Combine(Path.GetDirectoryName(projectFileFinder.ProjectFilePath), "obj"));
                }

                return new MsBuildProjectContextBuilder(projectFileFinder.ProjectFilePath, CodeGenerationTargetsLocation, configuration)
                    .Build();
            }

            throw new InvalidOperationException(string.Format(Resources.InvalidMsBuildProjectFile, projectFileFinder.ProjectFilePath));
        }

        private string GetTargetsLocation()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var path = Path.GetDirectoryName(assembly.Location);
            // Crawl up from assembly location till we find 'build' directory.
            do
            {
                if (Directory.EnumerateDirectories(path, "build", SearchOption.TopDirectoryOnly).Any())
                {
                    return path;
                }

                path = Path.GetDirectoryName(path);
            } while (path != null);


            return string.Empty;
        }

        private int Build(ILogger logger, string projectPath, string configuration, NuGetFramework frameworkToUse, string buildBasePath)
        {

            logger.LogMessage(Resources.BuildingProject);
            var buildResult = DotNetBuildCommandHelper.Build(
                projectPath,
                configuration,
                frameworkToUse,
                buildBasePath);

            if (buildResult.Result.ExitCode != 0)
            {
                //Build failed. 
                // Stop the process here. 
                logger.LogMessage(Resources.BuildFailed);
                logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdOut), LogMessageLevel.Error);
                logger.LogMessage(string.Join(Environment.NewLine, buildResult.StdErr), LogMessageLevel.Error);
            }

            return buildResult.Result.ExitCode;
        }

        private ScaffoldingServer StartServer(ILogger logger, IProjectContext projectInformation)
        {
            server = ScaffoldingServer.Listen(logger);
            server.AddHandler(new ProjectInformationMessageHandler(projectInformation, logger));
            server.AddHandler(new FileSystemChangeMessageHandler(logger));
            server.Accept();
            return server;
        }
    }
}