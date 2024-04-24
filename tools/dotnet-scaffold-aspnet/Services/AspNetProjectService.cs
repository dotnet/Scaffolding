// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Services
{
    internal class AspNetProjectService : IService
    {
        public IProjectService? ProjectService { get; set; }
        public ICodeService CodeService { get; set; }
        private readonly IAppSettings _appSettings;
        private readonly ILogger _logger;
        private readonly IHostService _hostService;
        private readonly IEnvironmentService _environmentService;
        public AspNetProjectService(
            IAppSettings appSettings,
            ILogger logger,
            IHostService hostService,
            IEnvironmentService environmentService,
            ICodeService codeService)
        {
            _logger = logger;
            _appSettings = appSettings;
            _environmentService = environmentService;
            _hostService = hostService;
            CodeService = codeService;
        }

        public async Task RunAsync()
        {
            await AnsiConsole.Status()
            .WithSpinner()
            .Start("Initializing project services!", async statusContext =>
            {
                statusContext.Refresh();
                statusContext.Status = "Initializing MSBuild project and roslyn workspace!";
                var projectPath = _appSettings.Workspace().InputPath;
                if (!string.IsNullOrEmpty(projectPath))
                {
                    ProjectService =  new ProjectService(projectPath, _logger);
                    await CodeService.GetWorkspaceAsync();
                    statusContext.Status = "DONE\n\n";
                }
            });
        }
    }
}
