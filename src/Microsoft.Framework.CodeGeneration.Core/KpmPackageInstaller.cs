// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Framework.PackageManager;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    public class KpmPackageInstaller : IPackageInstaller
    {
        private readonly IApplicationEnvironment _environment;
        private readonly ILogger _logger;

        public KpmPackageInstaller(
            [NotNull]ILogger logger,
            [NotNull]IApplicationEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task InstallPackages(IEnumerable<PackageMetadata> packages)
        {
            var report = new NullReport();

            foreach (var package in packages)
            {
                _logger.LogMessage("Adding dependency " + package.Name + 
                    " of version " + package.Version + " to the application.");

                AddCommand addComand = new AddCommand()
                {
                    Name = package.Name,
                    Version = package.Version,
                    ProjectDir = _environment.ApplicationBasePath,
                    Report = report
                };

                addComand.ExecuteCommand();
            }

            _logger.LogMessage("Started Restoring dependencies...");

            try
            {
                RestoreCommand restore = new RestoreCommand(_environment);
                restore.RestoreDirectory = _environment.ApplicationBasePath;
                restore.Reports = new Reports()
                {
                    Information = report,
                    Verbose = report,
                    Quiet = report
                };

                await restore.ExecuteCommand();
            }
            catch (Exception ex)
            {
                _logger.LogMessage("Error from Restore");
                _logger.LogMessage(ex.ToString());
            }

            _logger.LogMessage("Restoring complete");
        }
    }
}