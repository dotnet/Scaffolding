/*// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps.Project;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.UpgradeAssistant.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console.Flow;
using Microsoft.DotNet.Tools.Scaffold.Flow;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.Command
{
    public class RegisterComponent : BaseCommand<RegisterComponent.Settings>
    {
        private readonly ILogger _logger;
        private readonly IDotNetToolService _dotnetToolService;
        public RegisterComponent(
            ILogger logger,
            IDotNetToolService dotnetToolService,
            IFlowProvider flowProvider) : base(flowProvider)
        {
            _logger = logger;
            _dotnetToolService = dotnetToolService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            IEnumerable<IFlowStep> flowSteps =
            [
                new RegisterComponentFlowStep(_logger, _dotnetToolService, settings.ToolName)
            ];

            return await RunFlowAsync(flowSteps, settings, context.Remaining, settings.NonInteractive);
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[TOOL-NAME]")]
            public string? ToolName { get; init; }

            [CommandOption("--non-interactive")]
            public bool NonInteractive { get; init; }
        }
    }
}
*/
