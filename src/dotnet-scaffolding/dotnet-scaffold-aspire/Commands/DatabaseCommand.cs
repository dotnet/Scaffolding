// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
            _logger.LogInformation("Updating App host project...");
            var appHostResult = await UpdateAppHostAsync(settings);

            var dbContextProperties = GetDbContextProperties(settings);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(settings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add("BaseProjectPath", projectBasePath);
            }

            _logger.LogInformation("Updating web/worker project...");
            var workerResult = await UpdateWebAppAsync(settings);

            if (appHostResult && workerResult)
            {
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
            if (AspireCommandHelpers.DbContextTypeDefaults.TryGetValue(settings.Type, out var dbContextProperties) &&
                dbContextProperties is not null)
            {
                dbContextProperties.DbContextPath = newDbContextPath;
                return dbContextProperties;
            }

            return null;
        }

        private string CreateNewDbContextPath(CommandSettings commandSettings)
        {
            if (!AspireCommandHelpers.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) || dbContextProperties is null)
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

        private async Task<bool> UpdateAppHostAsync(CommandSettings commandSettings)
        {
            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("db-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config is null)
            {
                _logger.LogInformation("Unable to parse 'db-apphost.json' CodeModifierConfig.");
                return false;
            }

            var codeService = new CodeService(_logger, commandSettings.AppHostProject);
            var codeModifierProperties = await GetAppHostPropertiesAsync(commandSettings);
            CodeChangeStep codeChangeStep = new()
            {
                CodeModifierConfig = config,
                CodeModifierProperties = codeModifierProperties,
                Logger = _logger,
                ProjectPath = commandSettings.AppHostProject,
                CodeChangeOptions = new CodeChangeOptions()
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
                CodeChangeOptions = new CodeChangeOptions()
            };

            return await codeChangeStep.ExecuteAsync();
        }

        internal async Task<Dictionary<string, string>> GetAppHostPropertiesAsync(CommandSettings commandSettings)
        {
            var codeModifierProperties = new Dictionary<string, string>();
            var autoGenProjectNames = await AspireHelpers.GetAutoGeneratedProjectNamesAsync(commandSettings.AppHostProject, _logger);
            //add the web worker project name
            if (autoGenProjectNames.TryGetValue(commandSettings.Project, out var autoGenProjectName))
            {
                codeModifierProperties.Add("$(AutoGenProjectName)", autoGenProjectName);
            }

            if (AspireCommandHelpers.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) && dbProperties is not null)
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
            if (AspireCommandHelpers.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) &&
                AspireCommandHelpers.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) &&
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
