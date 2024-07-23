// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    internal class DatabaseCommand : ICommandWithSettings
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        public DatabaseCommand(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(CommandSettings settings, ScaffolderContext context)
        {
            if (!ValidateDatabaseCommandSettings(settings))
            {
                return -1;
            }

            _logger.LogInformation("Installing packages...");
            await InstallPackagesAsync(settings);

            _logger.LogInformation("Updating App host project...");
            var appHostResult = await UpdateAppHostAsync(settings);

            var dbContextProperties = GetDbContextProperties(settings);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            _logger.LogInformation("Updating web/worker project...");
            var workerResult = await UpdateWebAppAsync(settings);

            if (appHostResult && workerResult)
            {
                _logger.LogInformation("Finished");
                return 0;
            }
            else
            {
                _logger.LogInformation("An error occurred.");
                return -1;
            }
        }

        /// <summary>
        /// generate a path for DbContext, then use DbContextHelper.CreateDbContext to invoke 'NewDbContext.tt'
        /// DbContextHelper.CreateDbContext will also write the resulting templated string (class text) to disk
        /// </summary>
        private DbContextProperties? GetDbContextProperties(CommandSettings settings)
        {
            var newDbContextPath = CreateNewDbContextPath(settings);
            var projectBasePath = Path.GetDirectoryName(settings.Project);
            if (GetCmdsHelper.DbContextTypeDefaults.TryGetValue(settings.Type, out var dbContextProperties) &&
                dbContextProperties is not null)
            {
                dbContextProperties.DbContextPath = newDbContextPath;
                return dbContextProperties;
            }

            return null;
        }

        private string CreateNewDbContextPath(CommandSettings commandSettings)
        {
            if (!GetCmdsHelper.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) || dbContextProperties is null)
            {
                return string.Empty;
            }

            var dbContextPath = string.Empty;
            var dbContextFileName = $"{dbContextProperties.DbContextName}.cs";
            var baseProjectPath = Path.GetDirectoryName(commandSettings.Project);
            if (!string.IsNullOrEmpty(baseProjectPath))
            {
                dbContextPath = Path.Combine(baseProjectPath, dbContextFileName);
                dbContextPath = StringUtil.GetUniqueFilePath(dbContextPath);
            }

            return dbContextPath;
        }

        private bool ValidateDatabaseCommandSettings(CommandSettings commandSettings)
        {
            if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.DatabaseTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
            {
                string dbTypeDisplayList = string.Join(", ", GetCmdsHelper.DatabaseTypeCustomValues.GetRange(0, GetCmdsHelper.DatabaseTypeCustomValues.Count - 1)) +
                    (GetCmdsHelper.DatabaseTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.DatabaseTypeCustomValues[GetCmdsHelper.DatabaseTypeCustomValues.Count - 1];
                _logger.LogError("Missing/Invalid --type option.");
                _logger.LogError($"Valid options : {dbTypeDisplayList}");
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.AppHostProject) || !_fileSystem.FileExists(commandSettings.AppHostProject))
            {
                _logger.LogError("Missing/Invalid --apphost-project option.");
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.Project) || !_fileSystem.FileExists(commandSettings.Project))
            {
                _logger.LogError("Missing/Invalid --project option.");
                return false;
            }

            return true;
        }

        private async Task InstallPackagesAsync(CommandSettings commandSettings)
        {
            List<AddPackagesStep> packageSteps = [];
            if (PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? appHostPackageName))
            {
                var appHostPackageStep = new AddPackagesStep
                {
                    PackageNames = [appHostPackageName],
                    ProjectPath = commandSettings.AppHostProject,
                    Prerelease = commandSettings.Prerelease,
                    Logger = _logger
                };

                packageSteps.Add(appHostPackageStep);
            }

            if(PackageConstants.DatabasePackages.DatabasePackagesApiServiceDict.TryGetValue(commandSettings.Type, out string? projectPackageName))
            {
                var workerProjPackageStep = new AddPackagesStep
                {
                    PackageNames = [projectPackageName],
                    ProjectPath = commandSettings.AppHostProject,
                    Prerelease = commandSettings.Prerelease,
                    Logger = _logger
                };

                packageSteps.Add(workerProjPackageStep);
            }

            foreach (var packageStep in packageSteps)
            {
                await packageStep.ExecuteAsync();
            }
        }

        private async Task<bool> UpdateAppHostAsync(CommandSettings commandSettings)
        {
            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("db-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config is null)
            {
                _logger.LogInformation("Unable to parse 'db-apphost.json' CodeModifierConfig.");
                return false;
            }

            var workspaceSettings = new WorkspaceSettings
            {
                InputPath = commandSettings.AppHostProject
            };

            var hostAppSettings = new AppSettings();
            hostAppSettings.AddSettings("workspace", workspaceSettings);
            var codeService = new CodeService(hostAppSettings, _logger);
            var codeModifierProperties = await GetAppHostPropertiesAsync(commandSettings, codeService);
            CodeChangeStep codeChangeStep = new()
            {
                CodeModifierConfig = config,
                CodeModifierProperties = codeModifierProperties,
                Logger = _logger,
                ProjectPath = commandSettings.AppHostProject,
            };

            return await codeChangeStep.ExecuteAsync();
        }

        private async Task<bool> UpdateWebAppAsync(CommandSettings commandSettings)
        {
            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("db-webapi.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config is null)
            {
                return false;
            }

            var codeModifierProperties = GetApiProjectProperties(commandSettings);
            CodeChangeStep codeChangeStep = new()
            {
                CodeModifierConfig = config,
                CodeModifierProperties = codeModifierProperties,
                Logger = _logger,
                ProjectPath = commandSettings.Project,
            };

            return await codeChangeStep.ExecuteAsync();
        }

        internal async Task<Dictionary<string, string>> GetAppHostPropertiesAsync(CommandSettings commandSettings, ICodeService codeService)
        {
            var codeModifierProperties = new Dictionary<string, string>();
            var autoGenProjectNames = await AspireHelpers.GetAutoGeneratedProjectNamesAsync(commandSettings.AppHostProject, codeService);
            //add the web worker project name
            if (autoGenProjectNames.TryGetValue(commandSettings.Project, out var autoGenProjectName))
            {
                codeModifierProperties.Add("$(AutoGenProjectName)", autoGenProjectName);
            }

            if (GetCmdsHelper.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) && dbProperties is not null)
            {
                codeModifierProperties.Add("$(DbName)", dbProperties.AspireDbName);
                codeModifierProperties.Add("$(AddDbMethod)", dbProperties.AspireAddDbMethod);
                codeModifierProperties.Add("$(DbType)", dbProperties.AspireDbType);
            }

            return codeModifierProperties;
        }

        internal Dictionary<string, string> GetApiProjectProperties(CommandSettings commandSettings)
        {
            var codeModifierProperties = new Dictionary<string, string>();
            if (GetCmdsHelper.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) &&
                GetCmdsHelper.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) &&
                dbProperties is not null &&
                dbContextProperties is not null)
            {
                codeModifierProperties.Add("$(DbName)", dbProperties.AspireDbName);
                codeModifierProperties.Add("$(AddDbContextMethod)", dbProperties.AspireAddDbContextMethod);
                codeModifierProperties.Add("$(DbContextName)", dbContextProperties.DbContextName);
            }

            return codeModifierProperties;
        }
    }
}
