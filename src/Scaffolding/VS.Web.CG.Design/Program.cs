// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class Program
    {
        public const string TOOL_NAME = "dotnet-aspnet-codegenerator-design";
        private const string APPNAME = "Code Generation";

        private static ConsoleLogger _logger;

        public static void Main(string[] args)
        {
            _logger = new ConsoleLogger();
            _logger.LogMessage($"Command Line: {string.Join(" ", args)}", LogMessageLevel.Trace);

            //DotnetToolDispatcher.EnsureValidDispatchRecipient(ref args);
            Execute(args, _logger);
        }

        private static void Execute(string[] args, ConsoleLogger logger)
        {
            var app = new CommandLineApplication(false)
            {
                Name = APPNAME,
                Description = Resources.AppDesc
            };

            // Define app Options;
            app.HelpOption("-h|--help");
            var projectPath = app.Option("-p|--project", Resources.ProjectPathOptionDesc, CommandOptionType.SingleValue);
            var appConfiguration = app.Option("-c|--configuration", Resources.ConfigurationOptionDesc, CommandOptionType.SingleValue);
            var framework = app.Option("-tfm|--target-framework", Resources.TargetFrameworkOptionDesc, CommandOptionType.SingleValue);
            var buildBasePath = app.Option("-b|--build-base-path", "", CommandOptionType.SingleValue);
            var dependencyCommand = app.Option("--no-dispatch", "", CommandOptionType.NoValue);
            var port = app.Option("--port-number", "", CommandOptionType.SingleValue);
            var noBuild = app.Option("--no-build", "", CommandOptionType.NoValue);
            var simMode = app.Option("--simulation-mode", Resources.SimulationModeOptionDesc, CommandOptionType.NoValue);

#if DEBUG
            if (args.Contains("--debug", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Attach a debugger to processID: {System.Diagnostics.Process.GetCurrentProcess().Id} and hit enter.");
                Console.ReadKey();
            }
#endif

            app.OnExecute(async () =>
            {
                var exitCode = 1;
                try
                {
                    CodeGenerationEnvironmentHelper.SetupEnvironment();
                    string project = projectPath.Value();
                    if (string.IsNullOrEmpty(project))
                    {
                        project = Directory.GetCurrentDirectory();
                    }
                    project = Path.GetFullPath(project);
                    var configuration = appConfiguration.Value();

                    var portNumber = port.Value();
                    using (var client = ScaffoldingClient.Connect(portNumber, logger))
                    {
                        var messageOrchestrator = new MessageOrchestrator(client, logger);
                        var projectInformation = messageOrchestrator.GetProjectInformation();
                        string projectAssetsFile = ProjectModelHelper.GetProjectAssetsFile(projectInformation);
                        //fix package dependencies sent from VS
                        projectInformation = projectInformation.AddPackageDependencies(projectAssetsFile);
                        var codeGenArgs = ToolCommandLineHelper.FilterExecutorArguments(args);
                        var isSimulationMode = ToolCommandLineHelper.IsSimulationMode(args);
                        CodeGenCommandExecutor executor = new CodeGenCommandExecutor(projectInformation,
                            codeGenArgs,
                            configuration,
                            logger,
                            isSimulationMode);

                        exitCode = executor.Execute((changes) => messageOrchestrator.SendFileSystemChangeInformation(changes));

                        messageOrchestrator.SendScaffoldingCompletedMessage();
                    }
                }
                catch(Exception ex)
                {
                    do
                    {
                        logger.LogMessage(Resources.GenericErrorMessage, LogMessageLevel.Error);
                        logger.LogMessage(ex.Message, LogMessageLevel.Error);
                        logger.LogMessage(ex.StackTrace, LogMessageLevel.Trace);
                        if (!logger.IsTracing)
                        {
                            logger.LogMessage(Resources.EnableTracingMessage);
                        }
                        ex = ex .InnerException;
                    }
                    while (!(ex is null));
                }
                return exitCode;
                
            });

            app.Execute(args);
        }
    }
}
