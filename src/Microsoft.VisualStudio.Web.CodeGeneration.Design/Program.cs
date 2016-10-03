// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
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

            app.OnExecute(() =>
            {
                ScaffoldingClient client = null;

                string project = projectPath.Value();
                if (string.IsNullOrEmpty(project))
                {
                    project = Directory.GetCurrentDirectory();
                }
                project = Path.GetFullPath(project);
                var configuration = appConfiguration.Value();

                var portNumber = int.Parse(port.Value());
                try
                {
                    client = ScaffoldingClient.Connect(portNumber, logger);
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
                    ValidateProjectInfo(projectInfo);

                    logger.LogMessage($"Received Project Info, now need to chew on it.");
                    logger.LogMessage($"ProjectInfo: {projectInfo}");

                    var codeGenArgs = ToolCommandLineHelper.FilterExecutorArguments(args);

                    CodeGenCommandExecutor executor = new CodeGenCommandExecutor(projectInfo,
                        codeGenArgs,
                        configuration,
                        logger);

                    return executor.Execute();
                }
                finally
                {
                    if (client != null)
                    {
                        // Free the connection.
                        client.Dispose();
                    }
                }
            });

            app.Execute(args);
        }

        private static void ValidateProjectInfo(ProjectInfoContainer projectInfo)
        {
            if (projectInfo == null)
            {
                throw new ArgumentNullException(nameof(projectInfo));
            }

            if (projectInfo.ProjectContext == null
                || string.IsNullOrEmpty(projectInfo.ProjectContext.ProjectName)
                || projectInfo.ProjectContext.ProjectFile == null)
            {
                throw new Exception("Project context received from the tool is invalid/ incomplete.");
            }

            if (projectInfo.ProjectDependencyProvider == null
                || projectInfo.ProjectDependencyProvider.GetAllResolvedReferences() == null
                || projectInfo.ProjectDependencyProvider.GetAllPackages() == null)
            {
                throw new Exception("Dependency information received from the tool is invalid/ incomplete.");
            }
        }
    }
}
