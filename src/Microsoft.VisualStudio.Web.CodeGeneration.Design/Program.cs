// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class Program
    {
        public const string TOOL_NAME = "dotnet-aspnet-codegenerator-design";

        private static ConsoleLogger _logger;

        private const string APPNAME = "Code Generation";
        private const string APP_DESC = "Code generation for Asp.net Core";

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
                Description = APP_DESC
            };

            // Define app Options;
            app.HelpOption("-h|--help");
            var projectPath = app.Option("-p|--project", "Path to project.json", CommandOptionType.SingleValue);
            var appConfiguration = app.Option("-c|--configuration", "Configuration for the project (Possible values: Debug/ Release)", CommandOptionType.SingleValue);
            var framework = app.Option("-tfm|--target-framework", "Target Framework to use. (Short folder name of the tfm. eg. net451)", CommandOptionType.SingleValue);
            var buildBasePath = app.Option("-b|--build-base-path", "", CommandOptionType.SingleValue);
            var dependencyCommand = app.Option("--no-dispatch", "", CommandOptionType.NoValue);
            var port = app.Option("--port-number", "", CommandOptionType.SingleValue);
            var noBuild = app.Option("--no-build", "", CommandOptionType.NoValue);
            var simMode = app.Option("--simulation-mode", "Specifies whether to persist any file changes.", CommandOptionType.NoValue);

            app.OnExecute(async () =>
            {

                string project = projectPath.Value();
                if (string.IsNullOrEmpty(project))
                {
                    project = Directory.GetCurrentDirectory();
                }
                project = Path.GetFullPath(project);
                var configuration = appConfiguration.Value();

                var portNumber = int.Parse(port.Value());
                var projectInformation = await GetProjectInformationFromServer(logger, portNumber);

                var codeGenArgs = ToolCommandLineHelper.FilterExecutorArguments(args);
                var isSimulationMode = ToolCommandLineHelper.IsSimulationMode(args);
                CodeGenCommandExecutor executor = new CodeGenCommandExecutor(projectInformation,
                    codeGenArgs,
                    configuration,
                    logger,
                    isSimulationMode);

                return executor.Execute();
            });

            app.Execute(args);
        }

        private static async Task<IProjectContext> GetProjectInformationFromServer(ILogger logger, int portNumber)
        {
            using (var client = await ScaffoldingClient.Connect(portNumber, logger))
            {
                var messageHandler = new ScaffoldingMessageHandler(logger, "ScaffoldingClient");
                client.MessageReceived += messageHandler.HandleMessages;

                var message = new Message()
                {
                    MessageType = MessageTypes.ProjectInfoRequest
                };

                client.Send(message);
                // Read the project Information
                client.ReadMessage();

                var projectInfo = messageHandler.ProjectInfo;

                message = new Message()
                {
                    MessageType = MessageTypes.Scaffolding_Completed
                };

                client.Send(message);

                if (projectInfo == null)
                {
                    throw new InvalidOperationException($"Could not get ProjectInformation.");
                }

                return projectInfo;
            }
        }
    }
}
